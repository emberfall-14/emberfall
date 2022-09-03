﻿using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using static Content.Shared.Atmos.Components.SharedGasAnalyzerComponent;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly AtmosphereSystem _atmo = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<GasAnalyzerComponent, GasAnalyzerDisableMessage>(OnDisabledMessage);
            SubscribeLocalEvent<GasAnalyzerComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<GasAnalyzerComponent, UseInHandEvent>(OnUseInHand);
        }

        public override void Update(float frameTime)
        {

            foreach (var analyzer in EntityQuery<ActiveGasAnalyzerComponent>())
            {
                // Don't update every tick
                analyzer.AccumulatedFrametime += frameTime;

                if (analyzer.AccumulatedFrametime < analyzer.UpdateInterval)
                    continue;

                analyzer.AccumulatedFrametime -= analyzer.UpdateInterval;

                if (!UpdateAnalyzer(analyzer.Owner))
                    RemCompDeferred<ActiveGasAnalyzerComponent>(analyzer.Owner);
            }
        }

        /// <summary>
        /// Activates the analyzer when used in the world, scanning either the target entity or the tile clicked
        /// </summary>
        private void OnAfterInteract(EntityUid uid, GasAnalyzerComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
            {
                _popup.PopupEntity(Loc.GetString("gas-analyzer-component-player-cannot-reach-message"), args.User, Filter.Entities(args.User));
                return;
            }
            ActivateAnalyzer(uid, component, args.User, args.Target);
            OpenUserInterface(args.User, component);
            args.Handled = true;
        }

        /// <summary>
        /// Activates the analyzer with no target, so it only scans the tile the user was on when activated
        /// </summary>
        private void OnUseInHand(EntityUid uid, GasAnalyzerComponent component, UseInHandEvent args)
        {
            ActivateAnalyzer(uid, component, args.User);
            args.Handled = true;
        }

        /// <summary>
        /// Handles analyzer activation logic
        /// </summary>
        private void ActivateAnalyzer(EntityUid uid, GasAnalyzerComponent component, EntityUid user, EntityUid? target = null)
        {
            component.Target = target;
            component.User = user;
            component.LastPosition = Transform(target ?? user).Coordinates;
            component.Enabled = true;
            Dirty(component);
            SetAppearance(component);
            if(!HasComp<ActiveGasAnalyzerComponent>(uid))
                AddComp<ActiveGasAnalyzerComponent>(uid);
            UpdateAnalyzer(uid);
        }

        /// <summary>
        /// Close the UI, turn the analyzer off, and don't update when it's dropped
        /// </summary>
        private void OnDropped(EntityUid uid, GasAnalyzerComponent component, DroppedEvent args)
        {
            if(args.User is { } userId && component.Enabled)
                _popup.PopupEntity(Loc.GetString("gas-analyzer-shutoff"), userId, Filter.Entities(userId));
            DisableAnalyzer(uid, component, args.User);
        }

        /// <summary>
        /// Closes the UI, sets the icon to off, and removes it from the update list
        /// </summary>
        private void DisableAnalyzer(EntityUid uid, GasAnalyzerComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (user != null && TryComp<ActorComponent>(user, out var actor))
                _userInterface.TryClose(uid, GasAnalyzerUiKey.Key, actor.PlayerSession);

            component.Enabled = false;
            SetAppearance(component);
            Dirty(component);
            RemCompDeferred<ActiveGasAnalyzerComponent>(uid);
        }

        /// <summary>
        /// Disables the analyzer when the user closes the UI
        /// </summary>
        private void OnDisabledMessage(EntityUid uid, GasAnalyzerComponent component, GasAnalyzerDisableMessage message)
        {
            if (message.Session.AttachedEntity is not {Valid: true})
                return;
            DisableAnalyzer(uid, component);
        }

        private void OpenUserInterface(EntityUid user, GasAnalyzerComponent component)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            _userInterface.TryOpen(component.Owner, GasAnalyzerUiKey.Key, actor.PlayerSession);
        }

        /// <summary>
        /// Fetches fresh data for the analyzer. Should only be called by Update or when the user requests an update via refresh button
        /// </summary>
        private bool UpdateAnalyzer(EntityUid uid, GasAnalyzerComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            // check if the user has walked away from what they scanned
            var userPos = Transform(component.User).Coordinates;
            if (component.LastPosition.HasValue)
            {
                // Check if position is out of range => don't update and disable
                if (!component.LastPosition.Value.InRange(EntityManager, userPos, SharedInteractionSystem.InteractionRange))
                {
                    if(component.User is { } userId && component.Enabled)
                        _popup.PopupEntity(Loc.GetString("gas-analyzer-shutoff"), userId, Filter.Entities(userId));
                    DisableAnalyzer(uid, component, component.User);
                    return false;
                }
            }

            var gasMixList = new List<GasMixEntry>();

            if (component.Target != null)
            {
                // gas analyzed was used on an entity, try to request gas data via event for override
                var ev = new GasAnalyzerScanEvent();
                RaiseLocalEvent(component.Target.Value, ev, false);

                if (ev.GasMixtures != null)
                {
                    foreach (var mixes in ev.GasMixtures)
                    {
                        if(mixes.Value != null)
                            gasMixList.Add(new GasMixEntry(mixes.Key, mixes.Value.Pressure, mixes.Value.Temperature, GenerateGasEntryArray(mixes.Value)));
                    }
                }
                else
                {
                    // No override, fetch manually
                    if (TryComp(component.Target, out NodeContainerComponent? node))
                    {
                        foreach (var pair in node.Nodes)
                        {
                            if (pair.Value is PipeNode pipeNode)
                                gasMixList.Add(new GasMixEntry(pair.Key, pipeNode.Air.Pressure, pipeNode.Air.Temperature, GenerateGasEntryArray(pipeNode.Air)));
                        }
                    }
                }
            }

            // Fetch the environmental atmosphere around the scanner. This is after so the scanned item is the first/open tab for UI usability
            var tileMixture = _atmo.GetContainingMixture(component.Owner, true);
            if (tileMixture != null)
            {
                gasMixList.Add(new GasMixEntry(Loc.GetString("gas-analyzer-window-environment-tab-label"), tileMixture.Pressure, tileMixture.Temperature,
                    GenerateGasEntryArray(tileMixture)));
            }

            // Don't bother sending a UI message with no content, and stop updating I guess?
            if (gasMixList.Count == 0)
                return false;

            _userInterface.TrySendUiMessage(component.Owner, GasAnalyzerUiKey.Key, new GasAnalyzerUserMessage(gasMixList.ToArray()));
            return true;
        }

        /// <summary>
        /// Sets the appearance based on the analyzers Enabled state
        /// </summary>
        private void SetAppearance(GasAnalyzerComponent analyzer)
        {
            _appearance.SetData(analyzer.Owner, GasAnalyzerVisuals.VisualState,
                analyzer.Enabled ? GasAnalyzerVisualState.Working : GasAnalyzerVisualState.Off);
        }

        /// <summary>
        /// Generates a GasEntry array for a given GasMixture
        /// </summary>
        private GasEntry[] GenerateGasEntryArray(GasMixture? mixture)
        {
            var gases = new List<GasEntry>();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = _atmo.GetGas(i);

                if (mixture?.Moles[i] <= Atmospherics.GasMinMoles)
                    continue;

                if (mixture != null)
                    gases.Add(new GasEntry(gas.Name, mixture.Moles[i], gas.Color));
            }

            return gases.ToArray();
        }
    }
}

/// <summary>
/// Raised when the analyzer is used. An atmospherics device that does not rely on a NodeContainer or
/// wishes to override the default analyzer behaviour of fetching all nodes in the attached NodeContainer
/// should subscribe to this and return the GasMixtures as desired
/// </summary>
public sealed class GasAnalyzerScanEvent : EntityEventArgs
{
    /// <summary>
    /// Key is the mix name (ex "pipe", "inlet", "filter"), value is the mix. Add all mixes that should be reported when scanned
    /// </summary>
    public Dictionary<string, GasMixture?>? GasMixtures;
}
