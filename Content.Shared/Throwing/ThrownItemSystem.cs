using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System.Linq;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Handles throwing landing and collisions.
    /// </summary>
    public sealed class ThrownItemSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly EntityManager _entityManager = default!;

        private const string ThrowingFixture = "throw-fixture";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ThrownItemComponent, PhysicsSleepEvent>(OnSleep);
            SubscribeLocalEvent<ThrownItemComponent, StartCollideEvent>(HandleCollision);
            SubscribeLocalEvent<ThrownItemComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<ThrownItemComponent, ThrownEvent>(ThrowItem);
            SubscribeLocalEvent<ThrownItemComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<ThrownItemComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<PullStartedMessage>(HandlePullStarted);
        }

        private void OnGetState(EntityUid uid, ThrownItemComponent component, ref ComponentGetState args)
        {
            args.State = new ThrownItemComponentState(component.Thrower);
        }

        private void OnHandleState(EntityUid uid, ThrownItemComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ThrownItemComponentState {Thrower: not null } state ||
                !state.Thrower.Value.IsValid())
            {
                return;
            }

            component.Thrower = state.Thrower.Value;
        }

        private void ThrowItem(EntityUid uid, ThrownItemComponent component, ThrownEvent args)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out FixturesComponent? fixturesComponent) ||
                fixturesComponent.Fixtures.Count != 1) return;
            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? physicsComponent)) return;

            if (fixturesComponent.Fixtures.ContainsKey(ThrowingFixture))
            {
                Logger.Error($"Found existing throwing fixture on {component.Owner}");
                return;
            }
            var fixture = fixturesComponent.Fixtures.Values.First();
            var shape = fixture.Shape;
            var throwingFixture = new Fixture(physicsComponent, shape) { CollisionMask = (int) CollisionGroup.ThrownItem, Hard = false, ID = ThrowingFixture };
            _fixtures.TryCreateFixture(physicsComponent, throwingFixture, manager: fixturesComponent);
        }

        private void HandleCollision(EntityUid uid, ThrownItemComponent component, ref StartCollideEvent args)
        {
            if (args.OtherFixture.Hard == false)
                return;

            var thrower = component.Thrower;
            var otherBody = args.OtherFixture.Body;

            if (otherBody.Owner == thrower) return;
            ThrowCollideInteraction(thrower, args.OurFixture.Body, otherBody);
        }

        private void PreventCollision(EntityUid uid, ThrownItemComponent component, ref PreventCollideEvent args)
        {
            if (args.BodyB.Owner == component.Thrower)
            {
                args.Cancelled = true;
            }
        }

        private void OnSleep(EntityUid uid, ThrownItemComponent thrownItem, ref PhysicsSleepEvent @event)
        {
            StopThrow(uid, thrownItem);
        }

        private void HandlePullStarted(PullStartedMessage message)
        {
            // TODO: this isn't directed so things have to be done the bad way
            if (EntityManager.TryGetComponent(message.Pulled.Owner, out ThrownItemComponent? thrownItemComponent))
                StopThrow(message.Pulled.Owner, thrownItemComponent);
        }

        /// <summary>
        ///     Finish up a throw and raise StopThrowEvent.
        /// </summary>
        private void StopThrow(EntityUid uid, ThrownItemComponent thrownItemComponent)
        {
            if (EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                var fixture = _fixtures.GetFixtureOrNull(physicsComponent, ThrowingFixture);

                if (fixture != null)
                {
                    _fixtures.DestroyFixture(physicsComponent, fixture);
                }
            }

            EntityManager.EventBus.RaiseLocalEvent(uid, new StopThrowEvent {User = thrownItemComponent.Thrower}, true);
            EntityManager.RemoveComponent<ThrownItemComponent>(uid);
        }

        /// <summary>
        ///     Called when thrownItem should hit the ground and slide, can stop movement entirely if specified.
        /// </summary>
        public void LandComponent(ThrownItemComponent thrownItem, bool stopMoving = false)
        {
            if (thrownItem.Deleted || Deleted(thrownItem.Owner) || _containerSystem.IsEntityInContainer(thrownItem.Owner)) return;

            var landing = thrownItem.Owner;

            if (stopMoving && _entityManager.TryGetComponent(landing, out IPhysBody? physics))
                physics.LinearVelocity = (0, 0);

            // Unfortunately we can't check for hands containers as they have specific names.
            if (landing.TryGetContainerMan(out var containerManager) &&
                EntityManager.HasComponent<SharedHandsComponent>(containerManager.Owner))
            {
                EntityManager.RemoveComponent(landing, thrownItem);
                return;
            }

            // Assume it's uninteresting if it has no thrower. For now anyway.
            if (thrownItem.Thrower is not null)
                _adminLogger.Add(LogType.Landed, LogImpact.Low, $"{ToPrettyString(landing):entity} thrown by {ToPrettyString(thrownItem.Thrower.Value):thrower} landed.");

            var landMsg = new LandEvent {User = thrownItem.Thrower};
            RaiseLocalEvent(landing, landMsg, false);
        }

        /// <summary>
        ///     Raises collision events on the thrown and target entities.
        /// </summary>
        public void ThrowCollideInteraction(EntityUid? user, IPhysBody thrown, IPhysBody target)
        {
            if (user is not null)
                _adminLogger.Add(LogType.ThrowHit, LogImpact.Low,
                    $"{ToPrettyString(thrown.Owner):thrown} thrown by {ToPrettyString(user.Value):thrower} hit {ToPrettyString(target.Owner):target}.");

            var beHit = new ThrowHitByEvent(user, thrown, target);
            var doHit = new ThrowDoHitEvent(user, thrown, target);
            RaiseLocalEvent(target.Owner, ref beHit, true);
            RaiseLocalEvent(thrown.Owner, ref doHit, true);

            // Stop movement and throw state if requested by subscribers
            if (doHit.StopCollisions && TryComp<ThrownItemComponent>(thrown.Owner, out var item))
            {
                LandComponent(item, doHit.StopMoving);
                StopThrow(thrown.Owner, item);
            }                
        }
    }
}
