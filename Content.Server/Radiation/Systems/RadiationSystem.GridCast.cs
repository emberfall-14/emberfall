using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    private const bool SimplifiedSameGrid = true;

    private void UpdateGridcast()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var sources = EntityQuery<RadiationSourceComponent, TransformComponent>().ToArray();
        var destinations = EntityQuery<RadiationReceiverComponent, TransformComponent>().ToArray();
        var blockerQuery = GetEntityQuery<RadiationBlockerComponent>();

        var rays = new List<RadiationRay>();
        foreach (var (source, sourceTrs) in sources)
        {
            foreach (var (_, destTrs) in destinations)
            {
                var ray = Irradiate(sourceTrs, destTrs, source.RadsPerSecond, source.Slope, blockerQuery);
                if (ray != null)
                    rays.Add(ray);
            }
        }

        var elapsedTime = stopwatch.Elapsed.TotalMilliseconds;
        var totalSources = sources.Length;
        var totalReceivers = destinations.Length;

        var ev = new OnRadiationViewUpdateEvent(elapsedTime, totalSources, totalReceivers, rays);
        UpdateDebugView(ev);
    }

    private RadiationRay? Irradiate(TransformComponent sourceTrs, TransformComponent destTrs,
        float incomingRads, float slope,
        EntityQuery<RadiationBlockerComponent> blockerQuery)
    {
        // lets first check that source and destination on the same map
        if (sourceTrs.MapID != destTrs.MapID)
            return null;
        var mapId = sourceTrs.MapID;

        // get direction from rad source to destination and its distance
        var sourceWorldPos = sourceTrs.WorldPosition;
        var destWorldPos = destTrs.WorldPosition;
        var dir = destWorldPos - sourceWorldPos;
        var dist = dir.Length;

        // will it even reach destination considering distance penalty
        var slopeDist = slope * dist;
        var rads = slopeDist > 1f ? incomingRads / slopeDist : incomingRads;
        if (rads <= MinRads)
            return null;

        // if source and destination on the same grid it's possible that
        // between them can be another grid (ie. shuttle in center of donut station)
        // however we can do simplification and ignore that case
        if (SimplifiedSameGrid && sourceTrs.GridUid != null && sourceTrs.GridUid == destTrs.GridUid)
        {
            return Gridcast(mapId, sourceTrs.GridUid.Value, sourceWorldPos, destWorldPos, rads);
        }

        // lets check how many grids are between source and destination
        // do a raycast to get list of all grids that this rad is going to visit
        // it should be pretty cheap because we do it only on grid bounds
        var box = Box2.FromTwoPoints(sourceWorldPos, destWorldPos);
        var grids = _mapManager.FindGridsIntersecting(mapId, box, true).ToArray();
        var gridsCount = grids.Length;

        if (gridsCount == 0)
        {
            // no grids found - so no blockers (just distance penalty)
            return new RadiationRay
            {
                Source = sourceWorldPos,
                Destination = destWorldPos,
                Rads = rads
            };
        }
        else if (gridsCount == 1)
        {
            // one grid found - use it for gridcast
            return Gridcast(mapId, grids[0].GridEntityId, sourceWorldPos, destWorldPos, rads);
        }
        else
        {
            // more than one grid - fallback to raycast
            return Raycast(mapId, sourceWorldPos, dir.Normalized, dist, rads, blockerQuery);
        }
    }

    private RadiationRay Gridcast(MapId mapId, EntityUid gridUid,
        Vector2 sourceWorld, Vector2 destWorld, float incomingRads)
    {
        var visitedTiles = new List<(Vector2i, float?)>();
        var radRay = new RadiationRay
        {
            MapId = mapId,
            Source = sourceWorld,
            Destination = destWorld,
            Rads = incomingRads,
            Grid = gridUid,
            VisitedTiles = visitedTiles
        };

        // if grid doesn't have resistance map just apply distance penalty
        if (!_resistancePerTile.TryGetValue(gridUid, out var resistanceMap))
            return radRay;

        // get coordinate of source and destination in grid coordinates
        // todo: entity queries doesn't support interface - use it when IMapGridComponent will be removed
        if (!TryComp(gridUid, out IMapGridComponent? grid))
            return radRay;
        var sourceGrid = grid.Grid.TileIndicesFor(sourceWorld);
        var destGrid = grid.Grid.TileIndicesFor(destWorld);

        // iterate tiles in grid line from source to destination
        var line = Line(sourceGrid.X, sourceGrid.Y, destGrid.X, destGrid.Y);
        foreach (var point in line)
        {
            if (resistanceMap.TryGetValue(point, out var resData))
            {
                radRay.Rads -= resData.Tolerance[0];
                visitedTiles.Add((point, radRay.Rads));
            }
            else
            {
                visitedTiles.Add((point, null));
            }

            if (radRay.Rads <= MinRads)
                return radRay;
        }

        return radRay;
    }

    private RadiationRay Raycast(MapId mapId, Vector2 sourceWorld, Vector2 dir, float distance, float incomingRads,
        EntityQuery<RadiationBlockerComponent> blockerQuery)
    {
        var blockers = new List<(Vector2, float)>();
        var radRay = new RadiationRay
        {
            MapId = mapId,
            Source = sourceWorld,
            Destination = sourceWorld + dir * distance,
            Rads = incomingRads,
            Blockers = blockers
        };

        // do raycast to the physics
        var colRay = new CollisionRay(sourceWorld, dir, (int) CollisionGroup.Impassable);
        var results = _physicsSystem.IntersectRay(mapId, colRay, distance, returnOnFirstHit: false);

        foreach (var obstacle in results)
        {
            if (!blockerQuery.TryGetComponent(obstacle.HitEntity, out var blocker))
                continue;

            radRay.Rads -= blocker.RadResistance;
            blockers.Add((obstacle.HitPos, radRay.Rads));

            if (radRay.Rads <= MinRads)
            {
                radRay.Rads = 0;
                break;
            }
        }

        return radRay;
    }


    // bresenhams line algorithm
    // this is slightly rewritten version of code bellow
    // https://stackoverflow.com/questions/11678693/all-cases-covered-bresenhams-line-algorithm
    private IEnumerable<Vector2i> Line(int x, int y, int x2, int y2)
    {
        var w = x2 - x;
        var h = y2 - y;

        var dx1 = Math.Sign(w);
        var dy1 = Math.Sign(h);
        var dx2 = Math.Sign(w);
        var dy2 = 0;

        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        if (longest <= shortest)
        {
            (longest, shortest) = (shortest, longest);
            dx2 = 0;
            dy2 = Math.Sign(h);
        }

        var numerator = longest / 2;
        for (var i = 0; i <= longest; i++)
        {
            yield return new Vector2i(x, y);
            numerator += shortest;
            if (numerator >= longest)
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }
}
