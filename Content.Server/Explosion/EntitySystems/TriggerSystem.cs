using System;
using Content.Server.Administration.Logs;
using Content.Server.Doors.Components;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Shared.Audio;
using Content.Shared.Doors;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Threading;
using Content.Server.Construction.Components;
using Content.Shared.Trigger;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Physics;
using System.Collections.Generic;

namespace Content.Server.Explosion.EntitySystems
{
    /// <summary>
    /// Raised whenever something is Triggered on the entity.
    /// </summary>
    public class TriggerEvent : HandledEntityEventArgs
    {
        public EntityUid Triggered { get; }
        public EntityUid? User { get; }

        public TriggerEvent(EntityUid triggered, EntityUid? user = null)
        {
            Triggered = triggered;
            User = user;
        }
    }

    [UsedImplicitly]
    public sealed class TriggerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly FlashSystem _flashSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        private readonly List<TriggerOnProximityComponent> _proximityComponents = new();
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);
            SubscribeLocalEvent<TriggerOnProximityComponent, StartCollideEvent>(OnProximityStartCollide);
            SubscribeLocalEvent<TriggerOnProximityComponent, EndCollideEvent>(OnProximityEndCollide);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<SoundOnTriggerComponent, TriggerEvent>(HandleSoundTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);
            SubscribeLocalEvent<ToggleDoorOnTriggerComponent, TriggerEvent>(HandleDoorTrigger);

            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentRemove>(OnRemoval);

            SubscribeLocalEvent<TriggerOnProximityComponent, AnchorStateChangedEvent>(OnAnchorChange);
            
        }

        private void OnAnchorChange(EntityUid uid, TriggerOnProximityComponent component, ref AnchorStateChangedEvent args)
        {
            SetProximityFixture(uid, component, args.Anchored);
        }

        private void OnRemoval(EntityUid uid, TriggerOnProximityComponent component, ComponentRemove args)
        {
            _proximityComponents.Remove(component);
        }

        private void OnStartup(EntityUid uid, TriggerOnProximityComponent component, ComponentStartup args)
        {
            component.Enabled = component.Enabled && (!component.RequiresAnchored ||
                                                      EntityManager.GetComponent<TransformComponent>(uid).Anchored);

            SetProximityFixture(uid, component, component.Enabled, true);
            _proximityComponents.Add(component);
        }

        #region Explosions
        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out ExplosiveComponent? explosiveComponent)) return;

            Explode(uid, explosiveComponent, args.User);
        }

        // You really shouldn't call this directly (TODO Change that when ExplosionHelper gets changed).
        public void Explode(EntityUid uid, ExplosiveComponent component, EntityUid? user = null)
        {
            if (component.Exploding)
            {
                return;
            }

            component.Exploding = true;
            _explosions.SpawnExplosion(uid,
                component.DevastationRange,
                component.HeavyImpactRange,
                component.LightImpactRange,
                component.FlashRange,
                user);
            EntityManager.QueueDeleteEntity(uid);
        }
        #endregion

        #region Flash
        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f);
        }
        #endregion

        private void HandleSoundTrigger(EntityUid uid, SoundOnTriggerComponent component, TriggerEvent args)
        {
            if (component.Sound == null) return;
            SoundSystem.Play(Filter.Pvs(component.Owner), component.Sound.GetSound(), AudioHelpers.WithVariation(0.01f));
        }

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
        }

        private void HandleDoorTrigger(EntityUid uid, ToggleDoorOnTriggerComponent component, TriggerEvent args)
        {
            if (EntityManager.TryGetComponent<ServerDoorComponent>(uid, out var door))
            {
                switch (door.State)
                {
                    case SharedDoorComponent.DoorState.Open:
                        door.Close();
                        break;
                    case SharedDoorComponent.DoorState.Closed:
                        door.Open();
                        break;
                    case SharedDoorComponent.DoorState.Closing:
                    case SharedDoorComponent.DoorState.Opening:
                        break;
                }
            }
        }

        private void OnTriggerCollide(EntityUid uid, TriggerOnCollideComponent component, StartCollideEvent args)
        {
            Trigger(component.Owner);
        }

        #region Proximity

        private void OnProximityStartCollide(EntityUid uid, TriggerOnProximityComponent component, StartCollideEvent args)
        {
            if (args.OurFixture.ID != TriggerOnProximityComponent.FixtureID ||
                args.OtherFixture.Body.LinearVelocity.LengthSquared <= Math.Pow(component.TriggerSpeed, 2f)) return;

            var curTime = _gameTiming.CurTime;

            if (!component.Colliding.Add(args.OtherFixture.Body.Owner) ||
                component.NextTrigger > curTime) return;

            component.Colliding.Add(args.OtherFixture.Body.Owner);

            SetProximityAppearance(uid, component);
            Trigger(component.Owner);
            component.NextTrigger = TimeSpan.FromSeconds(curTime.TotalSeconds + component.Cooldown);

        }

        private void SetProximityAppearance(EntityUid uid, TriggerOnProximityComponent component)
        {
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(ProximityTriggerVisualState.State, ProximityTriggerVisuals.Active);
                if (component.AnimationDuration > 0f)
                {
                    Timer.Spawn(TimeSpan.FromSeconds(component.AnimationDuration), () =>
                    {
                        if (appearanceComponent.Deleted) return;
                        appearanceComponent.SetData(ProximityTriggerVisualState.State, ProximityTriggerVisuals.Inactive);
                    });
                }
            }
        }

        private void OnProximityEndCollide(EntityUid uid, TriggerOnProximityComponent component, EndCollideEvent args)
        {
            if (args.OurFixture.ID != TriggerOnProximityComponent.FixtureID) return;

            // We won't stop the timer because we may have a repeating one already running and we don't want to cancel it if
            // something comes back into range.
            component.Colliding.Remove(args.OtherFixture.Body.Owner);
        }


        public void SetProximityFixture(EntityUid uid, TriggerOnProximityComponent component, bool value, bool force = false)
        {
            if (component.Enabled == value && !force ||
                !EntityManager.TryGetComponent(uid, out PhysicsComponent? body)) return;

            component.Enabled = value;
            FixtureSystem fixtureSystem = Get<FixtureSystem>();

            if (value)
            {
                if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
                {
                    appearanceComponent.SetData(ProximityTriggerVisualState.State, ProximityTriggerVisuals.Inactive);
                }

                // Already has it so don't worry about it.
                if (fixtureSystem.GetFixtureOrNull(body, TriggerOnProximityComponent.FixtureID) != null) return;

                fixtureSystem.CreateFixture(body, new Fixture(body, component.Shape)
                {
                    // TODO: Should probably have these settable via datafield but I'm lazy and it's a pain
                    CollisionLayer = (int) (CollisionGroup.MobImpassable | CollisionGroup.SmallImpassable | CollisionGroup.VaultImpassable), Hard = false, ID = TriggerOnProximityComponent.FixtureID
                });
            }
            else
            {
                if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
                {
                    appearanceComponent.SetData(ProximityTriggerVisualState.State, ProximityTriggerVisuals.Off);
                }

                // Don't disable token in case we get enabled and re-enabled multiple times before it's triggered.
                var fixture = fixtureSystem.GetFixtureOrNull(body, TriggerOnProximityComponent.FixtureID);

                if (fixture == null) return;

                fixtureSystem.DestroyFixture(fixture);
            }
        }

        #endregion


        public void Trigger(EntityUid trigger, EntityUid? user = null)
        {
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger, triggerEvent);
        }

        public void HandleTimerTrigger(TimeSpan delay, EntityUid triggered, EntityUid? user = null)
        {
            if (delay.TotalSeconds <= 0)
            {
                Trigger(triggered, user);
                return;
            }

            Timer.Spawn(delay, () =>
            {
                if (Deleted(triggered)) return;
                Trigger(triggered, user);
            });
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in _proximityComponents)
            {
                if (comp.NextTrigger > _gameTiming.CurTime)
                    return;

                foreach (var colliding in comp.Colliding)
                {
                    EntityManager.TryGetComponent(colliding, out FixturesComponent fixtureComp);
                    foreach (var fixture in fixtureComp.Fixtures)
                    {
                        if (fixture.Value.Body.LinearVelocity.LengthSquared < Math.Pow(comp.TriggerSpeed, 2f))
                            break;

                        SetProximityAppearance(comp.Owner, comp);
                        Trigger(comp.Owner);
                        comp.NextTrigger = TimeSpan.FromSeconds(_gameTiming.CurTime.TotalSeconds + comp.Cooldown);
                    }

                }
            }
        }
    }
}
