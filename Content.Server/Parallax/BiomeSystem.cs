using Content.Server.Decals;
using Content.Shared.Decals;
using Content.Shared.Parallax.Biomes;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Parallax;

public sealed class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;
    private readonly HashSet<EntityUid> _handledEntities = new();
    private const float LoadRange = ChunkSize * 1.3f;
    private readonly Box2 _loadArea = new Box2(-LoadRange, -LoadRange, LoadRange, LoadRange);

    private readonly Dictionary<BiomeComponent, HashSet<Vector2i>> _activeChunks = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("biome");
        SubscribeLocalEvent<BiomeComponent, MapInitEvent>(OnBiomeMapInit);
    }

    private void OnBiomeMapInit(EntityUid uid, BiomeComponent component, MapInitEvent args)
    {
        component.Seed = _random.Next();
        Dirty(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var biomeQuery = GetEntityQuery<BiomeComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var biomes = EntityQueryEnumerator<BiomeComponent>();

        while (biomes.MoveNext(out var biome))
        {
            _activeChunks.Add(biome, new HashSet<Vector2i>());
        }

        // Get chunks in range
        foreach (var client in Filter.GetAllPlayers(_playerManager))
        {
            var pSession = (IPlayerSession) client;

            if (xformQuery.TryGetComponent(pSession.AttachedEntity, out var xform) &&
                _handledEntities.Add(pSession.AttachedEntity.Value) &&
                 biomeQuery.TryGetComponent(xform.MapUid, out var biome))
            {
                AddChunksInRange(biome, _transform.GetWorldPosition(xform, xformQuery));
            }

            foreach (var viewer in pSession.ViewSubscriptions)
            {
                if (!_handledEntities.Add(viewer) ||
                    !xformQuery.TryGetComponent(viewer, out xform) ||
                    !biomeQuery.TryGetComponent(xform.MapUid, out biome))
                {
                    continue;
                }

                AddChunksInRange(biome, _transform.GetWorldPosition(xform, xformQuery));
            }
        }

        var loadBiomes = EntityQueryEnumerator<BiomeComponent, MapGridComponent>();

        while (loadBiomes.MoveNext(out var biome, out var grid))
        {
            // Load new chunks
            LoadChunks(biome, grid);
            // Unload old chunks
            UnloadChunks(biome, grid);
        }

        _handledEntities.Clear();
        _activeChunks.Clear();
    }

    private void AddChunksInRange(BiomeComponent biome, Vector2 worldPos)
    {
        var enumerator = new ChunkIndicesEnumerator(_loadArea.Translated(worldPos), ChunkSize);

        while (enumerator.MoveNext(out var chunkOrigin))
        {
            _activeChunks[biome].Add(chunkOrigin.Value);
        }
    }

    private void LoadChunks(BiomeComponent component, MapGridComponent grid)
    {
        var active = _activeChunks[component];
        var noise = new FastNoise(component.Seed);
        var prototype = ProtoManager.Index<BiomePrototype>(component.BiomePrototype);
        List<(Vector2i, Tile)>? tiles = null;

        foreach (var chunk in active)
        {
            if (!component.LoadedChunks.Add(chunk))
                continue;

            tiles ??= new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
            // Load NOW!
            LoadChunk(component, grid, chunk * ChunkSize, noise, prototype, tiles);
        }
    }

    private void LoadChunk(BiomeComponent component, MapGridComponent grid, Vector2i chunk, FastNoise noise, BiomePrototype prototype, List<(Vector2i, Tile)> tiles)
    {
        _sawmill.Debug($"Loading chunk for {ToPrettyString(component.Owner)} at {chunk}");

        // Set tiles first
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (component.ModifiedTiles.Contains(indices))
                    continue;

                // If there's existing data then don't overwrite it.
                if (grid.TryGetTileRef(indices, out var tileRef) && !tileRef.Tile.IsEmpty)
                    continue;

                // Pass in null so we don't try to get the tileref.
                if (!TryGetBiomeTile(indices, prototype, noise, null, out var biomeTile) || biomeTile.Value == tileRef.Tile)
                    continue;

                tiles.Add((indices, biomeTile.Value));
            }
        }

        grid.SetTiles(tiles);
        tiles.Clear();

        // Now do entities
        var loadedEntities = new List<EntityUid>();
        component.LoadedEntities.Add(chunk, loadedEntities);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (component.ModifiedTiles.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _) || !TryGetEntity(indices, prototype, noise, grid, out var entPrototype))
                    continue;

                // TODO: Fix non-anchored ents spawning.
                // Just track loaded chunks for now.
                var ent = Spawn(entPrototype, grid.GridTileToLocal(indices));
                loadedEntities.Add(ent);
            }
        }

        // Decals
        var loadedDecals = new HashSet<uint>();
        component.LoadedDecals.Add(chunk, loadedDecals);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (component.ModifiedTiles.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _) || !TryGetDecals(indices, prototype, noise, grid, out var decals))
                    continue;

                foreach (var decal in decals)
                {
                    // TODO: Track decals probably
                    if (!_decals.TryAddDecal(decal.ID, new EntityCoordinates(grid.Owner, decal.Position), out var dec))
                        continue;

                    loadedDecals.Add(dec);
                }
            }
        }
    }

    private void UnloadChunks(BiomeComponent component, MapGridComponent grid)
    {
        var active = _activeChunks[component];

        foreach (var chunk in component.LoadedChunks)
        {
            if (active.Contains(chunk) || !component.LoadedChunks.Remove(chunk))
                continue;

            // Unload NOW!
            UnloadChunk(component, grid, chunk * ChunkSize);
        }
    }

    private void UnloadChunk(BiomeComponent component, MapGridComponent grid, Vector2i chunk)
    {
        // Reverse order to loading
        _sawmill.Debug($"Unloading chunk for {ToPrettyString(component.Owner)} at {chunk}");
        var noise = new FastNoise(component.Seed);
        var prototype = ProtoManager.Index<BiomePrototype>(component.BiomePrototype);

        // Delete decals
        foreach (var dec in component.LoadedDecals[chunk])
        {
            if (!_decals.RemoveDecal(grid.Owner, dec))
            {
                // TODO: Flag the tile as diff
                // component.ModifiedTiles.Add()
            }
        }

        component.LoadedDecals.Remove(chunk);

        // Delete entities
        // This is a TODO
        // Ideally any entities that aren't modified just get deleted and re-generated later
        // This is because if we want to save the map (e.g. persistent server) it makes the file much smaller
        // and also if the map is enormous will make stuff like physics broadphase much faster
        // For now we'll just leave them.

        component.LoadedEntities.Remove(chunk);

        // Unset tiles (if the data is custom)
        // TODO: Pass this in
        var tiles = new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _))
                    continue;

                // If it's default data unload the tile.
                if (!TryGetBiomeTile(indices, prototype, noise, null, out var biomeTile) || grid.TryGetTileRef(indices, out var tileRef) && tileRef.Tile != biomeTile.Value)
                    continue;

                tiles.Add((indices, Tile.Empty));
            }
        }

        grid.SetTiles(tiles);
        component.LoadedChunks.Remove(chunk);
    }

    // TODO: Round the view range
}
