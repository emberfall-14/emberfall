using Content.Server.Contests;
using Content.Server.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Resist;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;

    /// <summary>
    /// You can't escape the hands of an entity this many times more massive than you.
    /// </summary>
    public const float MaximumMassDisadvantage = 6f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeInventoryEvent>(OnEscape);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (!_containerSystem.TryGetContainingContainer(uid, out var container) || !_actionBlockerSystem.CanInteract(uid, container.Owner))
            return;

        // Make sure there's nothing stopped the removal (like being glued)
        if (!_containerSystem.CanRemove(uid, container))
        {
            _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-failed-resisting"), uid, uid);
            return;
        }

        // Contested
        if (_handsSystem.IsHolding(container.Owner, uid, out var inHand))
        {
            var contestResults = _contests.MassContest(uid, container.Owner);

            // Inverse if we aren't going to divide by 0, otherwise just use a default multiplier of 1.
            if (contestResults != 0)
                contestResults = 1 / contestResults;
            else
                contestResults = 1;

            if (contestResults >= MaximumMassDisadvantage)
            {
                _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-failed-resisting"), uid, uid);
                return;
            }

            AttemptEscape(uid, container.Owner, component, contestResults);
            return;
        }

        // Uncontested
        if (HasComp<StorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner) || HasComp<SecretStashComponent>(container.Owner))
            AttemptEscape(uid, container.Owner, component);
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component, float multiplier = 1f)
    {
        if (component.IsEscaping)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, component.BaseResistTime * multiplier, new EscapeInventoryEvent(), user, target: container)
        {
            BreakOnTargetMove = false,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out component.DoAfter))
            return;

        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), user, user);
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);
    }

    private void OnEscape(EntityUid uid, CanEscapeInventoryComponent component, EscapeInventoryEvent args)
    {
        component.DoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        _containerSystem.AttachParentToContainerOrGrid((uid, Transform(uid)));
        args.Handled = true;
    }

    private void OnDropped(EntityUid uid, CanEscapeInventoryComponent component, DroppedEvent args)
    {
        if (component.DoAfter != null)
            _doAfterSystem.Cancel(component.DoAfter);
    }
}
