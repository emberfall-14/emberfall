#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Utility
{
    public static class GridTileLookupHelpers
    {
        /// <summary>
        ///     Helper that returns all entities in a turf very fast.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this TileRef turf, SharedEntityLookupSystem? gridTileLookup = null)
        {
            gridTileLookup ??= EntitySystem.Get<SharedEntityLookupSystem>();

            return gridTileLookup.GetEntitiesIntersecting(turf.GridIndex, turf.GridIndices);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this Vector2i indices, GridId gridId, SharedEntityLookupSystem? gridTileLookup = null)
        {
            gridTileLookup ??= EntitySystem.Get<SharedEntityLookupSystem>();
            return gridTileLookup.GetEntitiesIntersecting(gridId, indices);
        }
    }
}
