﻿#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization;
using Content.Shared.GameObjects.EntitySystems;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    [ComponentReference(typeof(ToolComponent))]
    [ComponentReference(typeof(IToolComponent))]
    public class WelderComponent : ToolComponent, IExamine, IUse, ISuicideAct, ISolutionChange
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;

        public override string Name => "Welder";
        public override uint? NetID => ContentNetIDs.WELDER;

        /// <summary>
        /// Default Cost of using the welder fuel for an action
        /// </summary>
        public const float DefaultFuelCost = 10;

        /// <summary>
        /// Rate at which we expunge fuel from ourselves when activated
        /// </summary>
        public const float FuelLossRate = 0.5f;

        private bool _welderLit;
        private WelderSystem _welderSystem = default!;
        private SpriteComponent? _spriteComponent;
        private SolutionComponent? _solutionComponent;
        private PointLightComponent? _pointLightComponent;

        public string? WeldSoundCollection { get; set; }

        [ViewVariables]
        public float Fuel => _solutionComponent?.Solution.GetReagentQuantity("chem.WeldingFuel").Float() ?? 0f;

        [ViewVariables]
        public float FuelCapacity => _solutionComponent?.MaxVolume.Float() ?? 0f;

        /// <summary>
        /// Status of welder, whether it is ignited
        /// </summary>
        [ViewVariables]
        public bool WelderLit
        {
            get => _welderLit;
            private set
            {
                _welderLit = value;
                Dirty();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            AddQuality(ToolQuality.Welding);

            _welderSystem = _entitySystemManager.GetEntitySystem<WelderSystem>();

            Owner.TryGetComponent(out _solutionComponent);
            Owner.TryGetComponent(out _spriteComponent);
            Owner.TryGetComponent(out _pointLightComponent);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, collection => WeldSoundCollection, "weldSoundCollection", string.Empty);
        }

        public override ComponentState GetComponentState()
        {
            return new WelderComponentState(FuelCapacity, Fuel, WelderLit);
        }

        public override async Task<bool> UseTool(IEntity user, IEntity target, float doAfterDelay, ToolQuality toolQualityNeeded, Func<bool>? doAfterCheck = null)
        {
            bool ExtraCheck()
            {
                var extraCheck = doAfterCheck?.Invoke() ?? true;

                if (!CanWeld(DefaultFuelCost, user))
                {
                    _notifyManager.PopupMessage(target, user, "Can't weld!");

                    return false;
                }

                return extraCheck;
            }

            if (!CanWeld(DefaultFuelCost, user))
            {
                return false;
            }

            var canUse = await base.UseTool(user, target, doAfterDelay, toolQualityNeeded, ExtraCheck);

            return toolQualityNeeded.HasFlag(ToolQuality.Welding) ? canUse && TryWeld(DefaultFuelCost, user) : canUse;
        }

        public async Task<bool> UseTool(IEntity user, IEntity target, float doAfterDelay, ToolQuality toolQualityNeeded, float fuelConsumed, Func<bool>? doAfterCheck = null)
        {
            bool ExtraCheck()
            {
                var extraCheck = doAfterCheck?.Invoke() ?? true;

                return extraCheck && CanWeld(fuelConsumed, user);
            }

            return await base.UseTool(user, target, doAfterDelay, toolQualityNeeded, ExtraCheck) && TryWeld(fuelConsumed, user);
        }

        private bool TryWeld(float value, IEntity? user = null, bool silent = false)
        {
            if (!CanWeld(value, user))
            {
                if(!silent) _notifyManager.PopupMessage(Owner, user, Loc.GetString("The welder does not have enough fuel for that!"));
                return false;
            }

            if (_solutionComponent == null)
                return false;

            bool succeeded = _solutionComponent.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(value));

            if (succeeded && !silent)
            {
                PlaySoundCollection(WeldSoundCollection);
            }
            return succeeded;
        }

        private bool CanWeld(float value, IEntity? user = null, bool silent = false)
        {
            if (!WelderLit)
            {
                if (!silent) _notifyManager.PopupMessage(Owner, user, Loc.GetString("The welder is turned off!"));
                return false;
            }

            return Fuel > value || Qualities != ToolQuality.Welding;
        }

        private bool CanLitWelder()
        {
            return Fuel > 0 || Qualities != ToolQuality.Welding;
        }

        /// <summary>
        /// Deactivates welding tool if active, activates welding tool if possible
        /// </summary>
        private bool ToggleWelderStatus(IEntity? user = null)
        {
            var item = Owner.GetComponent<ItemComponent>();

            if (WelderLit)
            {
                WelderLit = false;
                // Layer 1 is the flame.
                item.EquippedPrefix = "off";
                _spriteComponent?.LayerSetVisible(1, false);

                if (_pointLightComponent != null) _pointLightComponent.Enabled = false;

                PlaySoundCollection("WelderOff", -5);
                _welderSystem.Unsubscribe(this);
                return true;
            }

            if (!CanLitWelder())
            {
                _notifyManager.PopupMessage(Owner, user, Loc.GetString("The welder has no fuel left!"));
                return false;
            }

            WelderLit = true;
            item.EquippedPrefix = "on";
            _spriteComponent?.LayerSetVisible(1, true);

            if (_pointLightComponent != null) _pointLightComponent.Enabled = true;

            PlaySoundCollection("WelderOn", -5);
            _welderSystem.Subscribe(this);

            Owner.Transform.GridPosition
                .GetTileAtmosphere()?.HotspotExpose(700f, 50f, true);

            return true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleWelderStatus(eventArgs.User);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (WelderLit)
            {
                message.AddMarkup(Loc.GetString("[color=orange]Lit[/color]\n"));
            }
            else
            {
                message.AddText(Loc.GetString("Not lit\n"));
            }

            if (inDetailsRange)
            {
                message.AddMarkup(Loc.GetString("Fuel: [color={0}]{1}/{2}[/color].",
                    Fuel < FuelCapacity / 4f ? "darkorange" : "orange", Math.Round(Fuel), FuelCapacity));
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            _welderSystem.Unsubscribe(this);
        }

        public void OnUpdate(float frameTime)
        {
            if (!HasQuality(ToolQuality.Welding) || !WelderLit || Owner.Deleted)
                return;

            _solutionComponent?.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(FuelLossRate * frameTime));

            Owner.Transform.GridPosition
                .GetTileAtmosphere()?.HotspotExpose(700f, 50f, true);

            if (Fuel == 0)
                ToggleWelderStatus();

        }

        public SuicideKind Suicide(IEntity victim, IChatManager chat)
        {
            if (TryWeld(5, victim, silent: true))
            {
                PlaySoundCollection(WeldSoundCollection);
                chat.EntityMe(victim, Loc.GetString("welds {0:their} every orifice closed! It looks like {0:theyre} trying to commit suicide!", victim));
                return SuicideKind.Heat;
            }

            chat.EntityMe(victim, Loc.GetString("bashes {0:themselves} with the {1}!", victim, Owner.Name));
            return SuicideKind.Blunt;
        }

        public void SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            Dirty();
        }
    }
}
