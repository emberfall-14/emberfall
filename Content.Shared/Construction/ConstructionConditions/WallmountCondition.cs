#nullable enable
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.Construction.ConstructionConditions
{ 
    [UsedImplicitly]
    public class WallmountCondition : IConstructionCondition
    {
        public void ExposeData(ObjectSerializer serializer) { }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();

            // get blueprint and user position
            var userWorldPosition = user.Transform.WorldPosition;
            var objWorldPosition = location.ToMap(entManager).Position;

            // find direction from user to blueprint
            var userToObject = (objWorldPosition - userWorldPosition);

            // dot product will be positive if user direction and blueprint are co-directed
            var dotProd = Vector2.Dot(direction.ToVec(), userToObject);
            if (dotProd > 0)
                return false;

            // now we need to check that user actually tries to build wallmount on a wall 
            var physics = IoCManager.Resolve<IPhysicsManager>();
            var rUserToObj = new CollisionRay(userWorldPosition, userToObject.Normalized, (int) CollisionGroup.Walls);
            var length = userToObject.Length;
            var userToObjRaycastResults = physics.IntersectRay(user.Transform.MapID, rUserToObj, maxLength: length);
            if (!userToObjRaycastResults.Any())
                return false;

            // get this wall entity
            var targetWall = userToObjRaycastResults.First();

            // check that we didn't try to build wallmount that facing another adjacent wall
            var rAdjWall = new CollisionRay(objWorldPosition, direction.ToVec(), (int) CollisionGroup.Walls);
            var adjWallRaycastResults = physics.IntersectRay(user.Transform.MapID, rAdjWall, maxLength: 0.5f,
                ignoredEnt: targetWall.HitEntity);
            return !adjWallRaycastResults.Any();
        }
    }
}
