﻿using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blocking;

public sealed class SharedBlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBlockingComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<SharedBlockingComponent, DroppedEvent>(OnDrop);

        SubscribeLocalEvent<SharedBlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);

        SubscribeLocalEvent<SharedBlockingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SharedBlockingComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnUserDamageModified(EntityUid uid, SharedBlockingUserComponent component, DamageModifyEvent args)
    {
        if (TryComp(component.BlockingItem, out SharedBlockingComponent? blockingComponent))
        {
            if (_proto.TryIndex(blockingComponent.PassiveBlockDamageModifer, out DamageModifierSetPrototype? passiveblockModifier) && !blockingComponent.IsBlocking)
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, passiveblockModifier);

            if (_proto.TryIndex(blockingComponent.ActiveBlockDamageModifier, out DamageModifierSetPrototype? activeBlockModifier) && blockingComponent.IsBlocking)
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, activeBlockModifier);
        }
    }

    private void OnPickupAttempt(EntityUid uid, SharedBlockingComponent component, GettingPickedUpAttemptEvent args)
    {
        component.User = args.User;

        //To make sure that this bodytype doesn't get set as anything but the original
        if (TryComp(args.User, out PhysicsComponent? physicsComponent) && physicsComponent.BodyType != BodyType.Static
                                                                       && !TryComp(args.User, out SharedBlockingUserComponent? blockingUserComponent))
        {
            var userComp = EnsureComp<SharedBlockingUserComponent>(args.User);
            userComp.BlockingItem = uid;
            userComp.OriginalBodyType = physicsComponent.BodyType;
        }
    }

    private void OnDrop(EntityUid uid, SharedBlockingComponent component, DroppedEvent args)
    {
        if (component.IsBlocking)
            StopBlocking(uid, component, args.User);

        RemComp<SharedBlockingUserComponent>(args.User);
        component.User = null;
    }


    private void OnGetActions(EntityUid uid, SharedBlockingComponent component, GetItemActionsEvent args)
    {
        if (component.BlockingToggleAction == null
            && _proto.TryIndex(component.BlockingToggleActionId, out InstantActionPrototype? act))
        {
            component.BlockingToggleAction = new(act);
        }

        if (component.BlockingToggleAction != null)
            args.Actions.Add(component.BlockingToggleAction);
    }

    private void OnToggleAction(EntityUid uid, SharedBlockingComponent component, ToggleActionEvent args)
    {
        if(args.Handled)
            return;

        if (component.IsBlocking)
            StopBlocking(uid, component, args.Performer);
        else
            StartBlocking(uid, component, args.Performer);

        args.Handled = true;
    }

    /// <summary>
    /// Called where you want the user to start blocking
    /// Creates a new hard fixture to bodyblock
    /// Also makes the user static to prevent prediction issues
    /// </summary>
    /// <param name="uid"> The entity with the blocking component</param>
    /// <param name="component"> The blocking component</param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    private bool StartBlocking(EntityUid item, SharedBlockingComponent component, EntityUid user)
    {
        if (component.IsBlocking) return false;

        var xform = Transform(user);

        component.IsBlocking = true;

        var msgUser = Loc.GetString("action-popup-blocking");

        var shape = new PhysShapeCircle();
        shape.Radius = component.BlockRadius;

        if (TryComp(user, out PhysicsComponent? physicsComponent))
        {
            var fixture = new Fixture(physicsComponent, shape)
            {
                ID = component.BlockFixtureID,
                Hard = true,
                CollisionLayer = (int) CollisionGroup.WallLayer
            };

            _fixtureSystem.TryCreateFixture(physicsComponent, fixture);
        }

        if (component.BlockingToggleAction != null)
        {
            _actionsSystem.SetToggled(component.BlockingToggleAction, true);
            _transformSystem.AnchorEntity(xform);
            _popupSystem.PopupEntity(msgUser, user, Filter.Entities(user));
        }

        return true;
    }

    /// <summary>
    /// Called where you want the user to stop blocking.
    /// </summary>
    /// <param name="item"> The entity with the blocking component</param>
    /// <param name="component"> The blocking component</param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    private bool StopBlocking(EntityUid item, SharedBlockingComponent component, EntityUid user)
    {
        if (!component.IsBlocking) return false;

        component.IsBlocking = false;

        var xform = Transform(user);

        var msgUser = Loc.GetString("action-popup-blocking-disabling");

        //If the component blocking toggle isn't null, grab the users SharedBlockingUserComponent and PhysicsComponent
        //then toggle the action to false, unanchor the user, remove the hard fixture
        //and set the users bodytype back to their original type
        if (component.BlockingToggleAction != null && TryComp(user, out SharedBlockingUserComponent? blockingUserComponent)
                                                     && TryComp(user, out PhysicsComponent? physicsComponent))
        {
            _actionsSystem.SetToggled(component.BlockingToggleAction, false);
            _transformSystem.Unanchor(xform);
            _fixtureSystem.DestroyFixture(physicsComponent, component.BlockFixtureID);
            physicsComponent.BodyType = blockingUserComponent.OriginalBodyType;
            _popupSystem.PopupEntity(msgUser, user, Filter.Entities(user));
        }

        return true;
    }

}
