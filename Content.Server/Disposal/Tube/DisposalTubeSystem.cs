using System.Linq;
using System.Text;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Completions;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Destructible;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly DisposableSystem _disposableSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DisposalTubeComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<DisposalTubeComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<DisposalTubeComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<DisposalTubeComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<DisposalTubeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DisposalTubeComponent, ConstructionBeforeDeleteEvent>(OnDeconstruct);

            SubscribeLocalEvent<DisposalBendComponent, GetDisposalsConnectableDirectionsEvent>(OnGetBendConnectableDirections);
            SubscribeLocalEvent<DisposalBendComponent, GetDisposalsNextDirectionEvent>(OnGetBendNextDirection);

            SubscribeLocalEvent<DisposalEntryComponent, GetDisposalsConnectableDirectionsEvent>(OnGetEntryConnectableDirections);
            SubscribeLocalEvent<DisposalEntryComponent, GetDisposalsNextDirectionEvent>(OnGetEntryNextDirection);

            SubscribeLocalEvent<DisposalJunctionComponent, GetDisposalsConnectableDirectionsEvent>(OnGetJunctionConnectableDirections);
            SubscribeLocalEvent<DisposalJunctionComponent, GetDisposalsNextDirectionEvent>(OnGetJunctionNextDirection);

            SubscribeLocalEvent<DisposalTransitComponent, GetDisposalsConnectableDirectionsEvent>(OnGetTransitConnectableDirections);
            SubscribeLocalEvent<DisposalTransitComponent, GetDisposalsNextDirectionEvent>(OnGetTransitNextDirection);

            SubscribeLocalEvent<DisposalTaggerComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<DisposalTaggerComponent, GetDisposalsNextDirectionEvent>(OnGetTaggerNextDirection);

            SubscribeLocalEvent<DisposalTaggerComponent, ActivatableUIOpenAttemptEvent>(OnOpenTaggerUIAttempt);

            SubscribeLocalEvent<DisposalTaggerComponent, TaggerSetTagMessage>(OnTaggerSetTag);
        }


        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="msg">A user interface message from the client.</param>
        private void OnTaggerSetTag(EntityUid uid, DisposalTaggerComponent tagger, TaggerSetTagMessage msg)
        {
            if (!DisposalTaggerUiKey.Key.Equals(msg.UiKey))
                return;

            // Ignore malformed strings
            if (!TaggerSetTagMessage.TagRegex.IsMatch(msg.Tag))
                return;

            if (TryComp<PhysicsComponent>(uid, out var physBody) && physBody.BodyType != BodyType.Static)
                return;

            tagger.Tag = msg.Tag;
            _audioSystem.PlayPvs(tagger.ClickSound, uid);
        }

        private void OnComponentInit(EntityUid uid, DisposalTubeComponent tube, ComponentInit args)
        {
            tube.Contents = _containerSystem.EnsureContainer<Container>(uid, tube.ContainerId);
        }

        private void OnComponentRemove(EntityUid uid, DisposalTubeComponent tube, ComponentRemove args)
        {
            DisconnectTube(uid, tube);
        }

        private void OnComponentRemove(EntityUid uid, DisposalTaggerComponent tagger, ComponentRemove args)
        {
            _uiSystem.TryCloseAll(uid, DisposalTaggerUiKey.Key);
        }

        private void OnGetBendConnectableDirections(EntityUid uid, DisposalBendComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            var direction = Transform(uid).LocalRotation;
            var side = new Angle(MathHelper.DegreesToRadians(direction.Degrees - 90));

            args.Connectable = new[] { direction.GetDir(), side.GetDir() };
        }

        private void OnGetBendNextDirection(EntityUid uid, DisposalBendComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(uid, ref ev);

            var previousDF = args.Holder.PreviousDirectionFrom;

            if (previousDF == Direction.Invalid)
            {
                args.Next = ev.Connectable[0];
                return;
            }

            args.Next = previousDF == ev.Connectable[0] ? ev.Connectable[1] : ev.Connectable[0];
        }

        private void OnGetEntryConnectableDirections(EntityUid uid, DisposalEntryComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            args.Connectable = new[] { Transform(uid).LocalRotation.GetDir() };
        }

        private void OnGetEntryNextDirection(EntityUid uid, DisposalEntryComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            // Ejects contents when they come from the same direction the entry is facing.
            if (args.Holder.PreviousDirectionFrom != Direction.Invalid)
            {
                args.Next = Direction.Invalid;
                return;
            }

            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(uid, ref ev);
            args.Next = ev.Connectable[0];
        }

        private void OnGetJunctionConnectableDirections(EntityUid uid, DisposalJunctionComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            var direction = Transform(uid).LocalRotation;

            args.Connectable = component.Degrees
                .Select(degree => new Angle(degree.Theta + direction.Theta).GetDir())
                .ToArray();
        }

        private void OnGetJunctionNextDirection(EntityUid uid, DisposalJunctionComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            var next = Transform(uid).LocalRotation.GetDir();
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(uid, ref ev);
            var directions = ev.Connectable.Skip(1).ToArray();

            if (args.Holder.PreviousDirectionFrom == Direction.Invalid ||
                args.Holder.PreviousDirectionFrom == next)
            {
                args.Next = _random.Pick(directions);
                return;
            }

            args.Next = next;
        }

        private void OnGetTransitConnectableDirections(EntityUid uid, DisposalTransitComponent component, ref GetDisposalsConnectableDirectionsEvent args)
        {
            var rotation = Transform(uid).LocalRotation;
            var opposite = new Angle(rotation.Theta + Math.PI);

            args.Connectable = new[] { rotation.GetDir(), opposite.GetDir() };
        }

        private void OnGetTransitNextDirection(EntityUid uid, DisposalTransitComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(uid, ref ev);
            var previousDF = args.Holder.PreviousDirectionFrom;
            var forward = ev.Connectable[0];

            if (previousDF == Direction.Invalid)
            {
                args.Next = forward;
                return;
            }

            var backward = ev.Connectable[1];
            args.Next = previousDF == forward ? backward : forward;
        }

        private void OnGetTaggerNextDirection(EntityUid uid, DisposalTaggerComponent component, ref GetDisposalsNextDirectionEvent args)
        {
            args.Holder.Tags.Add(component.Tag);
        }

        private void OnDeconstruct(EntityUid uid, DisposalTubeComponent component, ConstructionBeforeDeleteEvent args)
        {
            DisconnectTube(uid, component);
        }

        private void OnStartup(EntityUid uid, DisposalTubeComponent component, ComponentStartup args)
        {
            UpdateAnchored(uid, component, Transform(uid).Anchored);
        }

        private void OnRelayMovement(EntityUid uid, DisposalTubeComponent component, ref ContainerRelayMovementEntityEvent args)
        {
            if (_gameTiming.CurTime < component.LastClang + DisposalTubeComponent.ClangDelay)
            {
                return;
            }

            component.LastClang = _gameTiming.CurTime;
            _audioSystem.PlayPvs(component.ClangSound, uid);
        }

        private void OnBreak(EntityUid uid, DisposalTubeComponent component, BreakageEventArgs args)
        {
            DisconnectTube(uid, component);
        }

        private void OnOpenTaggerUIAttempt(EntityUid uid, DisposalTaggerComponent tagger, ActivatableUIOpenAttemptEvent args)
        {
            if (!TryComp<HandsComponent>(args.User, out var hands))
            {
                _popups.PopupEntity(Loc.GetString("disposal-tagger-window-activate-no-hands"), uid, args.User);
                return;
            }

            var activeHandEntity = hands.ActiveHandEntity;
            if (activeHandEntity != null)
            {
                args.Cancel();
            }

            if (_uiSystem.TryGetUi(uid, DisposalTaggerUiKey.Key, out var bui))
                _uiSystem.SetUiState(bui,
                    new DisposalTaggerUserInterfaceState(tagger.Tag));
        }

        private void OnAnchorChange(EntityUid uid, DisposalTubeComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateAnchored(uid, component, args.Anchored);
        }

        private void UpdateAnchored(EntityUid uid, DisposalTubeComponent component, bool anchored)
        {
            if (anchored)
            {
                ConnectTube(uid, component);

                // TODO this visual data should just generalized into some anchored-visuals system/comp, this has nothing to do with disposal tubes.
                _appearanceSystem.SetData(uid, DisposalTubeVisuals.VisualState, DisposalTubeVisualState.Anchored);
            }
            else
            {
                DisconnectTube(uid, component);
                _appearanceSystem.SetData(uid, DisposalTubeVisuals.VisualState, DisposalTubeVisualState.Free);
            }
        }

        public EntityUid? NextTubeFor(EntityUid target, Direction nextDirection, DisposalTubeComponent? targetTube = null)
        {
            if (!Resolve(target, ref targetTube))
                return null;
            var oppositeDirection = nextDirection.GetOpposite();

            var xform = Transform(target);
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return null;

            var position = xform.Coordinates;
            foreach (var entity in grid.GetInDir(position, nextDirection))
            {
                if (!TryComp(entity, out DisposalTubeComponent? tube))
                {
                    continue;
                }

                if (!CanConnect(entity, tube, oppositeDirection))
                {
                    continue;
                }

                if (!CanConnect(target, targetTube, nextDirection))
                {
                    continue;
                }

                return entity;
            }

            return null;
        }

        public static void ConnectTube(EntityUid _, DisposalTubeComponent tube)
        {
            if (tube.Connected)
            {
                return;
            }

            tube.Connected = true;
        }


        public void DisconnectTube(EntityUid _, DisposalTubeComponent tube)
        {
            if (!tube.Connected)
            {
                return;
            }

            tube.Connected = false;

            var query = GetEntityQuery<DisposalHolderComponent>();
            foreach (var entity in tube.Contents.ContainedEntities.ToArray())
            {
                if (query.TryGetComponent(entity, out var holder))
                    _disposableSystem.ExitDisposals(entity, holder);
            }
        }

        public bool CanConnect(EntityUid tubeId, DisposalTubeComponent tube, Direction direction)
        {
            if (!tube.Connected)
            {
                return false;
            }

            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(tubeId, ref ev);
            return ev.Connectable.Contains(direction);
        }

        public void PopupDirections(EntityUid tubeId, DisposalTubeComponent _, EntityUid recipient)
        {
            var ev = new GetDisposalsConnectableDirectionsEvent();
            RaiseLocalEvent(tubeId, ref ev);
            var directions = string.Join(", ", ev.Connectable);

            _popups.PopupEntity(Loc.GetString("disposal-tube-component-popup-directions-text", ("directions", directions)), tubeId, recipient);
        }

        public bool TryInsert(EntityUid uid, DisposalUnitComponent from, IEnumerable<string>? tags = default, DisposalEntryComponent? entry = null)
        {
            if (!Resolve(uid, ref entry))
                return false;

            var xform = Transform(uid);
            var holder = Spawn(DisposalEntryComponent.HolderPrototypeId, xform.MapPosition);
            var holderComponent = Comp<DisposalHolderComponent>(holder);

            foreach (var entity in from.Container.ContainedEntities.ToArray())
            {
                _disposableSystem.TryInsert(holder, entity, holderComponent);
            }

            _atmosSystem.Merge(holderComponent.Air, from.Air);
            from.Air.Clear();

            if (tags != default)
                holderComponent.Tags.UnionWith(tags);

            return _disposableSystem.EnterTube(holder, uid, holderComponent);
        }
    }
}
