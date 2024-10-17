using Content.Shared.Dice;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Dice;

public sealed class LoadedDiceSystem : SharedLoadedDiceSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoadedDiceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LoadedDiceComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<LoadedDiceComponent, LoadedDiceSideSelectedMessage>(OnSelected);
        SubscribeLocalEvent<LoadedDiceComponent, DiceRollEvent>(OnDiceRoll);
        SubscribeLocalEvent<LoadedDiceComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
    }

    private void OnMapInit(EntityUid uid, LoadedDiceComponent component, MapInitEvent args)
    {
        SetSelectedSide(uid, component, null);
    }

    private void OnVerb(EntityUid uid, LoadedDiceComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.User != args.User)
            return;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("loaded-dice-set-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => TryOpenUi(uid, args.User, component)
        });

    }

    private void OnAlternativeVerb(EntityUid uid, LoadedDiceComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.User != args.User)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Priority = 1, // Make this the alt-click verb
            Text = Loc.GetString("loaded-dice-unset-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Act = () => SetSelectedSide(uid, component, null)
        });
    }

    private void OnSelected(EntityUid uid, LoadedDiceComponent component, LoadedDiceSideSelectedMessage args)
    {
        SetSelectedSide(uid, component, args.SelectedSide);
    }

    private void OnDiceRoll(EntityUid uid, LoadedDiceComponent component, ref DiceRollEvent roll)
    {
        if (component.SelectedSide == null)
            return;

        roll.Roll = component.SelectedSide.Value;
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, LoadedDiceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!TryComp(user, out ActorComponent? actor))
            return;
        _uiSystem.TryToggleUi(uid, LoadedDiceUiKey.Key, actor.PlayerSession);
    }

    private void UpdateUi(EntityUid uid, DiceComponent? die = null, LoadedDiceComponent? component = null)
    {
        if (!Resolve(uid, ref die))
            return;

        if (!Resolve(uid, ref component))
            return;

        var state = new LoadedDiceBoundUserInterfaceState(die, component.SelectedSide);

        _uiSystem.SetUiState(uid, LoadedDiceUiKey.Key, state);
    }

    public void SetSelectedSide(EntityUid uid, LoadedDiceComponent? component = null, int? selectedSide = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<DiceComponent>(uid, out var die))
            return;

        // Make sure that it is valid change
        if (selectedSide != null && (selectedSide < 1 || selectedSide > die.Sides))
            return;

        // There may not be a user if the change is not done by a player, e.g. OnMapInit
        if (component.User != null)
        {
            string message;
            if (selectedSide == null)
            {
                message = Loc.GetString("loaded-dice-unset", ("die", uid));
            }
            else
            {
                var sideValue = (selectedSide - die.Offset) * die.Multiplier;
                message = Loc.GetString("loaded-dice-set", ("die", uid), ("value", sideValue));
            }

            _popup.PopupEntity(message, uid, component.User.Value, PopupType.Small);
        }

        component.SelectedSide = selectedSide;

        UpdateUi(uid, die, component);
        Dirty(uid, component);
    }
}
