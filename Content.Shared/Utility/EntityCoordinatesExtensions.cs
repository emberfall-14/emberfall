﻿#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Shared.Utility
{
    public static class EntityCoordinatesExtensions
    {
        public static EntityCoordinates ToCoordinates(this EntityUid id, Vector2 offset)
        {
            return new EntityCoordinates(id, offset);
        }

        public static EntityCoordinates ToCoordinates(this EntityUid id, float x, float y)
        {
            return new EntityCoordinates(id, new Vector2(x, y));
        }

        public static EntityCoordinates ToCoordinates(this EntityUid id)
        {
            return ToCoordinates(id, Vector2.Zero);
        }

        public static EntityCoordinates ToCoordinates(this IEntity entity, Vector2 offset)
        {
            return ToCoordinates(entity.Uid, offset);
        }

        public static EntityCoordinates ToCoordinates(this IEntity entity, float x, float y)
        {
            return new EntityCoordinates(entity.Uid, new Vector2(x, y));
        }

        public static EntityCoordinates ToCoordinates(this IEntity entity)
        {
            return ToCoordinates(entity.Uid, Vector2.Zero);
        }

        public static EntityCoordinates ToCoordinates(this IMapGrid grid, Vector2 offset)
        {
            return ToCoordinates(grid.GridEntityId, offset);
        }

        public static EntityCoordinates ToCoordinates(this IMapGrid grid, float x, float y)
        {
            return ToCoordinates(grid.GridEntityId, new Vector2(x, y));
        }

        public static EntityCoordinates ToCoordinates(this IMapGrid grid)
        {
            return ToCoordinates(grid.GridEntityId, Vector2.Zero);
        }

        // TODO: Remove
        public static IEntity SpawnEntity(this IEntityManager manager, string? protoName, EntityCoordinates coordinates)
        {
            return manager.SpawnEntity(protoName, coordinates.ToMap(manager));
        }

        // TODO: Remove
        public static EntityCoordinates ToCoordinates(this MapIndices indices, IMapManager mapManager, GridId gridIndex)
        {
            return EntityCoordinates.FromGrid(mapManager, indices.ToGridCoordinates(mapManager, gridIndex));
        }
    }
}
