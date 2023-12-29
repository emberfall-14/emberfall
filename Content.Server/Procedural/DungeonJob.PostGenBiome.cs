using System.Threading.Tasks;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Procedural;

public sealed partial class DungeonJob
{
    /*
     * Handles PostGen code for marker layers + biomes.
     */

    private async Task PostGen(BiomePostGen postGen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        if (_entManager.TryGetComponent(gridUid, out BiomeComponent? biomeComp))
            return;

        biomeComp = _entManager.AddComponent<BiomeComponent>(gridUid);
        var biomeSystem = _entManager.System<BiomeSystem>();
        biomeSystem.SetTemplate(gridUid, biomeComp, _prototype.Index(postGen.BiomeTemplate));
        var seed = random.Next();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var node in dungeon.RoomTiles)
        {
            // Need to set per-tile to override data.
            if (biomeSystem.TryGetTile(node, biomeComp.Layers, seed, grid, out var tile))
            {
                _maps.SetTile(gridUid, grid, node, tile.Value);
            }

            if (biomeSystem.TryGetDecals(node, biomeComp.Layers, seed, grid, out var decals))
            {
                foreach (var decal in decals)
                {
                    _decals.TryAddDecal(decal.ID, new EntityCoordinates(gridUid, decal.Position), out _);
                }
            }

            if (biomeSystem.TryGetEntity(node, biomeComp, grid, out var entityProto))
            {
                var ent = _entManager.SpawnAtPosition(entityProto, new EntityCoordinates(gridUid, node + grid.TileSizeHalfVector));
                var xform = xformQuery.Get(ent);

                if (!xform.Comp.Anchored)
                    _transform.AnchorEntity(ent, xform);
            }

            await SuspendIfOutOfTime();
            ValidateResume();
        }

        biomeComp.Enabled = false;
    }

    private async Task PostGen(BiomeMarkerLayerPostGen postGen, Dungeon dungeon, EntityUid gridUid, MapGridComponent grid, Random random)
    {
        if (!_entManager.TryGetComponent(gridUid, out BiomeComponent? biomeComp))
            return;

        var biomeSystem = _entManager.System<BiomeSystem>();
        var markerTemplate = _prototype.Index(postGen.MarkerTemplate);
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        var bounds = new Box2i();

        foreach (var tile in dungeon.RoomTiles)
        {
            if (bounds.Contains(tile))
                continue;

            bounds = bounds.UnionTile(tile);
        }

        await SuspendIfOutOfTime();
        ValidateResume();

        int count = postGen.Count;

        if (count == 0)
        {
            count = (int) (bounds.Area / (markerTemplate.Radius * markerTemplate.Radius));
            count = Math.Min(count, markerTemplate.MaxCount);
        }

        biomeSystem.GetMarkerNodes(gridUid, biomeComp, grid, markerTemplate, true, bounds, count,
            random, out var spawnSet, out var existing, false);

        await SuspendIfOutOfTime();
        ValidateResume();

        foreach (var ent in existing)
        {
            _entManager.DeleteEntity(ent);
        }

        await SuspendIfOutOfTime();
        ValidateResume();

        foreach (var (node, mask) in spawnSet)
        {
            string? proto;

            if (mask != null && markerTemplate.EntityMask.TryGetValue(mask, out var maskedProto))
            {
                proto = maskedProto;
            }
            else
            {
                proto = markerTemplate.Prototype;
            }

            var ent = _entManager.SpawnAtPosition(proto, new EntityCoordinates(gridUid, node + grid.TileSizeHalfVector));
            var xform = xformQuery.Get(ent);

            if (!xform.Comp.Anchored)
                _transform.AnchorEntity(ent, xform);

            await SuspendIfOutOfTime();
            ValidateResume();
        }
    }
}
