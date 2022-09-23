using System.Linq;
using System.Threading;
using Content.Server.Doors.Systems;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems
{
    public sealed partial class NPCSteeringSystem : EntitySystem
    {
        // http://www.red3d.com/cwr/papers/1999/gdc99steer.html for a steering overview
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DoorSystem _doors = default!;
        [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;

        // This will likely get moved onto an abstract pathfinding node that specifies the max distance allowed from the coordinate.
        private const float TileTolerance = 0.4f;

        private bool _enabled;

        public override void Initialize()
        {
            base.Initialize();
            InitializeAvoidance();
            _configManager.OnValueChanged(CCVars.NPCEnabled, SetNPCEnabled, true);

            SubscribeLocalEvent<NPCSteeringComponent, ComponentShutdown>(OnSteeringShutdown);
        }

        private void OnSteeringShutdown(EntityUid uid, NPCSteeringComponent component, ComponentShutdown args)
        {
            component.PathfindToken?.Cancel();
        }

        private void SetNPCEnabled(bool obj)
        {
            if (!obj)
            {
                foreach (var (_, mover) in EntityQuery<NPCSteeringComponent, InputMoverComponent>())
                {
                    mover.CurTickSprintMovement = Vector2.Zero;
                }
            }

            _enabled = obj;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownAvoidance();
            _configManager.UnsubValueChanged(CCVars.NPCEnabled, SetNPCEnabled);
        }

        /// <summary>
        /// Adds the AI to the steering system to move towards a specific target
        /// </summary>
        public NPCSteeringComponent Register(EntityUid uid, EntityCoordinates coordinates, NPCSteeringComponent? component = null)
        {
            if (Resolve(uid, ref component, false))
            {
                component.PathfindToken?.Cancel();
                component.PathfindToken = null;
                component.CurrentPath.Clear();
            }
            else
            {
                component = AddComp<NPCSteeringComponent>(uid);
            }

            EnsureComp<NPCRVOComponent>(uid);
            component.Coordinates = coordinates;
            return component;
        }

        /// <summary>
        /// Attempts to register the entity. Does nothing if the coordinates already registered.
        /// </summary>
        public bool TryRegister(EntityUid uid, EntityCoordinates coordinates, NPCSteeringComponent? component = null)
        {
            if (Resolve(uid, ref component, false) && component.Coordinates.Equals(coordinates))
            {
                return false;
            }

            Register(uid, coordinates, component);
            return true;
        }

        /// <summary>
        /// Stops the steering behavior for the AI and cleans up.
        /// </summary>
        public void Unregister(EntityUid uid, NPCSteeringComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            if (EntityManager.TryGetComponent(component.Owner, out InputMoverComponent? controller))
            {
                controller.CurTickSprintMovement = Vector2.Zero;
            }

            component.PathfindToken?.Cancel();
            component.PathfindToken = null;
            RemComp<NPCRVOComponent>(uid);
            RemComp<NPCSteeringComponent>(uid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
                return;

            // Not every mob has the modifier component so do it as a separate query.
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var modifierQuery = GetEntityQuery<MovementSpeedModifierComponent>();

            var npcs = EntityQuery<NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent>()
                .ToArray();

            // TODO: Do this in parallel.
            // Main obstacle is requesting a new path needs to be done synchronously
            foreach (var (steering, _, mover, xform) in npcs)
            {
                Steer(steering, mover, xform, modifierQuery, bodyQuery, frameTime);
            }
        }

        private void SetDirection(InputMoverComponent component, NPCSteeringComponent steering, Vector2 value)
        {
            if (value.Equals(Vector2.Zero))
            {
                steering.CurrentPath.Clear();
            }

            component.CurTickSprintMovement = value;
            component.LastInputTick = _timing.CurTick;
            component.LastInputSubTick = ushort.MaxValue;
        }

        /// <summary>
        /// Go through each steerer and combine their vectors
        /// </summary>
        private void Steer(
            NPCSteeringComponent steering,
            InputMoverComponent mover,
            TransformComponent xform,
            EntityQuery<MovementSpeedModifierComponent> modifierQuery,
            EntityQuery<PhysicsComponent> bodyQuery,
            float frameTime)
        {
            var ourCoordinates = xform.Coordinates;
            var destinationCoordinates = steering.Coordinates;

            // We've arrived, nothing else matters.
            if (xform.Coordinates.TryDistance(EntityManager, destinationCoordinates, out var distance) &&
                distance <= steering.Range)
            {
                SetDirection(mover, steering, Vector2.Zero);
                steering.Status = SteeringStatus.InRange;
                return;
            }

            // Can't move at all, just noop input.
            if (!mover.CanMove)
            {
                SetDirection(mover, steering, Vector2.Zero);
                steering.Status = SteeringStatus.Moving;
                return;
            }

            // Grab the target position, either the path or our end goal.
            // TODO: Some situations we may not want to move at our target without a path.
            var targetCoordinates = GetTargetCoordinates(steering);
            var arrivalDistance = SharedInteractionSystem.InteractionRange - 0.5f;

            if (targetCoordinates.Equals(steering.Coordinates))
            {
                // What's our tolerance for arrival.
                // If it's a pathfinding node it might be different to the destination.
                arrivalDistance = steering.Range;
            }

            // Check if mapids match.
            var targetMap = targetCoordinates.ToMap(EntityManager);
            var ourMap = ourCoordinates.ToMap(EntityManager);

            if (targetMap.MapId != ourMap.MapId)
            {
                SetDirection(mover, steering, Vector2.Zero);
                steering.Status = SteeringStatus.NoPath;
                return;
            }

            var direction = targetMap.Position - ourMap.Position;

            // Are we in range
            if (direction.Length <= arrivalDistance)
            {
                // Node needs some kind of special handling like access or smashing.
                if (steering.CurrentPath.TryPeek(out var node))
                {
                    var status = TryHandleFlags(steering, node, bodyQuery);

                    switch (status)
                    {
                        case SteeringObstacleStatus.Completed:
                            break;
                        case SteeringObstacleStatus.Failed:
                            // TODO: Blacklist the poly for next query
                            SetDirection(mover, steering, Vector2.Zero);
                            steering.Status = SteeringStatus.NoPath;
                            return;
                        case SteeringObstacleStatus.Continuing:
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Otherwise it's probably regular pathing so just keep going a bit more to get to tile centre
                if (direction.Length <= TileTolerance)
                {
                    // It was just a node, not the target, so grab the next destination (either the target or next node).
                    if (steering.CurrentPath.Count > 0)
                    {
                        steering.CurrentPath.Dequeue();

                        // Alright just adjust slightly and grab the next node so we don't stop moving for a tick.
                        // TODO: If it's the last node just grab the target instead.
                        targetCoordinates = GetTargetCoordinates(steering);
                        targetMap = targetCoordinates.ToMap(EntityManager);

                        // Can't make it again.
                        if (ourMap.MapId != targetMap.MapId)
                        {
                            SetDirection(mover, steering, Vector2.Zero);
                            steering.Status = SteeringStatus.NoPath;
                            return;
                        }

                        // Gonna resume now business as usual
                        direction = targetMap.Position - ourMap.Position;
                    }
                    else
                    {
                        // This probably shouldn't happen as we check above but eh.
                        SetDirection(mover, steering, Vector2.Zero);
                        steering.Status = SteeringStatus.InRange;
                        return;
                    }
                }
            }

            // Do we have no more nodes to follow OR has the target moved sufficiently? If so then re-path.
            var needsPath = steering.CurrentPath.Count == 0;

            // TODO: Probably need partial planning support i.e. patch from the last node to where the target moved to.

            if (!needsPath)
            {
                var lastNode = GetCoordinates(steering.CurrentPath.Peek());
                // I know this is bad and doesn't account for tile size
                // However with the path I'm going to change it to return pathfinding nodes which include coordinates instead.

                if (lastNode.TryDistance(EntityManager, steering.Coordinates, out var lastDistance) &&
                    lastDistance > steering.RepathRange)
                {
                    needsPath = true;
                }
            }

            // Request the new path.
            if (needsPath && bodyQuery.TryGetComponent(steering.Owner, out var body))
            {
                RequestPath(steering, xform, body);
            }

            modifierQuery.TryGetComponent(steering.Owner, out var modifier);
            var moveSpeed = GetSprintSpeed(steering.Owner, modifier);

            var input = direction.Normalized;

            // If we're going to overshoot then... don't.
            // TODO: For tile / movement we don't need to get bang on, just need to make sure we don't overshoot the far end.
            var tickMovement = moveSpeed * frameTime;

            if (tickMovement.Equals(0f))
            {
                SetDirection(mover, steering, Vector2.Zero);
                steering.Status = SteeringStatus.NoPath;
                return;
            }

            // We may overshoot slightly but still be in the arrival distance which is okay.
            var maxDistance = direction.Length + arrivalDistance;

            if (tickMovement > maxDistance)
            {
                input *= maxDistance / tickMovement;
            }

            // TODO: This isn't going to work for space.
            if (_mapManager.TryGetGrid(xform.GridUid, out var grid))
            {
                input = (-grid.WorldRotation).RotateVec(input);
            }

            SetDirection(mover, steering, input);
        }

        /// <summary>
        /// We may be pathfinding and moving at the same time in which case early nodes may be out of date.
        /// </summary>
        /// <param name="coordinates">Our coordinates we are pruning from</param>
        /// <param name="nodes">Path we're pruning</param>
        public void PrunePath(EntityCoordinates coordinates, Queue<PathPoly> nodes)
        {
            if (nodes.Count == 0)
                return;

            // Right now the pathfinder gives EVERY TILE back but ideally it won't someday, it'll just give straightline ones.
            // For now, we just prune up until the closest node + 1 extra.
            var closest = (GetCoordinates(nodes.Peek()).Position - coordinates.Position).Length;
            // TODO: Need to handle multi-grid and stuff.

            while (nodes.TryPeek(out var node))
            {
                var length = (coordinates.Position - GetCoordinates(node).Position).Length;

                if (length < closest)
                {
                    closest = length;
                    nodes.Dequeue();
                    continue;
                }

                nodes.Dequeue();
                break;
            }
        }

        /// <summary>
        /// Get the coordinates we should be heading towards.
        /// </summary>
        private EntityCoordinates GetTargetCoordinates(NPCSteeringComponent steering)
        {
            // Depending on what's going on we may return the target or a pathfind node.

            // Even if we're at the last node may not be able to head to target in case we get stuck on a corner or the likes.
            if (steering.CurrentPath.Count >= 1 && steering.CurrentPath.TryPeek(out var nextTarget))
            {
                return GetCoordinates(nextTarget);
            }

            return steering.Coordinates;
        }

        private EntityCoordinates GetCoordinates(PathPoly poly)
        {
            if (!poly.IsValid())
                return EntityCoordinates.Invalid;

            return new EntityCoordinates(poly.GraphUid, poly.Box.Center);
        }

        /// <summary>
        /// Get a new job from the pathfindingsystem
        /// </summary>
        private async void RequestPath(NPCSteeringComponent steering, TransformComponent xform, PhysicsComponent? body)
        {
            // If we already have a pathfinding request then don't grab another.
            if (steering.Pathfind)
                return;

            steering.PathfindToken = new CancellationTokenSource();

            var result = await _pathfindingSystem.GetPath(
                steering.Owner,
                xform.Coordinates,
                steering.Coordinates,
                steering.Range,
                steering.PathfindToken.Token);

            steering.CurrentPath = result.Path;
        }

        // TODO: Move these to movercontroller

        private float GetSprintSpeed(EntityUid uid, MovementSpeedModifierComponent? modifier = null)
        {
            if (!Resolve(uid, ref modifier, false))
            {
                return MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
            }

            return modifier.CurrentSprintSpeed;
        }
    }
}
