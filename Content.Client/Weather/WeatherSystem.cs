using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new WeatherOverlay(EntityManager.System<SpriteSystem>(), this));
        SubscribeLocalEvent<WeatherComponent, ComponentHandleState>(OnWeatherHandleState);
    }

    protected override void Run(WeatherComponent component, WeatherPrototype weather, WeatherState state)
    {
        base.Run(component, weather, state);

        var ent = _playerManager.LocalPlayer?.ControlledEntity;

        if (ent == null)
            return;

        var mapUid = Transform(component.Owner).MapUid;
        var entXform = Transform(ent.Value);

        // Maybe have the viewports manage this?
        if (mapUid == null || entXform.MapUid != mapUid)
        {
            component.Stream?.Stop();
            component.Stream = null;
            return;
        }

        // TODO: Average alpha across nearby 2x2 tiles.
        // At least, if we can change position

        if (Timing.IsFirstTimePredicted && component.Stream != null)
        {
            var stream = (AudioSystem.PlayingStream) component.Stream;
            var alpha = GetPercent(component, mapUid.Value, weather);
            alpha = MathF.Pow(alpha, 2f);
            // TODO: Lerp this occlusion.
            var occlusion = 0f;
            // TODO: Fade-out needs to be slower
            // TODO: HELPER PLZ

            // Work out tiles nearby to determine volume.
            if (entXform.GridUid != null)
            {
                // Floodfill to the nearest tile and use that for audio.
                var grid = MapManager.GetGrid(entXform.GridUid.Value);
                var seed = grid.GetTileRef(entXform.Coordinates);
                var frontier = new Queue<TileRef>();
                frontier.Enqueue(seed);
                // If we don't have a nearest node don't play any sound.
                EntityCoordinates? nearestNode = null;
                var bodyQuery = GetEntityQuery<PhysicsComponent>();
                var visited = new HashSet<Vector2i>();

                while (frontier.TryDequeue(out var node))
                {
                    if (!visited.Add(node.GridIndices))
                        continue;

                    if (!CanWeatherAffect(grid, node, weather, bodyQuery))
                    {
                        // Add neighbors
                        // TODO: Ideally we pick some deterministically random direction and use that
                        // We can't just do that naively here because it will flicker between nearby tiles.
                        for (var x = -1; x <= 1; x++)
                        {
                            for (var y = -1; y <= 1; y++)
                            {
                                if (Math.Abs(x) == 1 && Math.Abs(y) == 1 ||
                                    x == 0 && y == 0 ||
                                    (new Vector2(x, y) + node.GridIndices - seed.GridIndices).Length > 3)
                                {
                                    continue;
                                }

                                frontier.Enqueue(grid.GetTileRef(new Vector2i(x, y) + node.GridIndices));
                            }
                        }

                        continue;
                    }

                    nearestNode = new EntityCoordinates(entXform.GridUid.Value,
                        (Vector2) node.GridIndices + (grid.TileSize / 2f));
                    break;
                }

                if (nearestNode == null)
                    alpha = 0f;
                else
                {
                    var entPos = entXform.WorldPosition;
                    var sourceRelative = nearestNode.Value.ToMap(EntityManager).Position - entPos;

                    occlusion = _physics.IntersectRayPenetration(entXform.MapID,
                        new CollisionRay(entXform.WorldPosition, sourceRelative.Normalized, _audio.OcclusionCollisionMask),
                        sourceRelative.Length, stream.TrackingEntity);
                }

            }

            // Full volume if not on grid
            stream.Gain = alpha;
            stream.Source.SetOcclusion(occlusion);
        }
    }

    public float GetPercent(WeatherComponent component, EntityUid mapUid, WeatherPrototype weatherProto)
    {
        var pauseTime = _metadata.GetPauseTime(mapUid);
        var elapsed = Timing.CurTime - (component.StartTime + pauseTime);
        var duration = component.Duration;
        var remaining = duration - elapsed;
        float alpha;

        if (elapsed < weatherProto.StartupTime)
        {
            alpha = (float) (elapsed / weatherProto.StartupTime);
        }
        else if (remaining < weatherProto.ShutdownTime)
        {
            alpha = (float) (remaining / weatherProto.ShutdownTime);
        }
        else
        {
            alpha = 1f;
        }

        return alpha;
    }

    protected override bool SetState(WeatherComponent component, WeatherState state, WeatherPrototype prototype)
    {
        if (!base.SetState(component, state, prototype))
            return false;

        if (Timing.IsFirstTimePredicted)
        {
            // TODO: Fades
            component.Stream?.Stop();
            component.Stream = null;
            component.Stream = _audio.PlayGlobal(prototype.Sound, Filter.Local(), true);
        }

        return true;
    }

    private void OnWeatherHandleState(EntityUid uid, WeatherComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WeatherComponentState state)
            return;

        if (component.Weather != state.Weather || !component.EndTime.Equals(state.EndTime) || !component.StartTime.Equals(state.StartTime))
        {
            EndWeather(component);

            if (state.Weather != null)
                StartWeather(component, ProtoMan.Index<WeatherPrototype>(state.Weather));
        }

        component.EndTime = state.EndTime;
        component.StartTime = state.StartTime;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<WeatherOverlay>();
    }
}
