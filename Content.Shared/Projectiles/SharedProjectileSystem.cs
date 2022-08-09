using Content.Shared.Vehicle.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles
{
    public abstract class SharedProjectileSystem : EntitySystem
    {
        public const string ProjectileFixture = "projectile";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedProjectileComponent, PreventCollideEvent>(PreventCollision);
        }

        private void PreventCollision(EntityUid uid, SharedProjectileComponent component, PreventCollideEvent args)
        {
            // Relay it to the rider if in vehicle
            // Note that if mechs (for WHATEVER REASON) decide to use their own system again they will also need
            // to consider this.
            if (component.IgnoreShooter && args.BodyB.Owner == component.Shooter ||
                TryComp<VehicleComponent>(args.BodyB.Owner, out var vehicle) && component.Shooter == vehicle.Rider)
            {
                args.Cancel();
                return;
            }
        }

        public void SetShooter(SharedProjectileComponent component, EntityUid uid)
        {
            if (component.Shooter == uid) return;

            component.Shooter = uid;
            Dirty(component);
        }

        [NetSerializable, Serializable]
        protected sealed class ProjectileComponentState : ComponentState
        {
            public ProjectileComponentState(EntityUid shooter, bool ignoreShooter)
            {
                Shooter = shooter;
                IgnoreShooter = ignoreShooter;
            }

            public EntityUid Shooter { get; }
            public bool IgnoreShooter { get; }
        }

        [Serializable, NetSerializable]
        protected sealed class ImpactEffectEvent : EntityEventArgs
        {
            public string Prototype;
            public EntityCoordinates Coordinates;

            public ImpactEffectEvent(string prototype, EntityCoordinates coordinates)
            {
                Prototype = prototype;
                Coordinates = coordinates;
            }
        }
    }
}
