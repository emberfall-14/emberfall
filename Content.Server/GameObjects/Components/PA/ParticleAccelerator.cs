﻿using System;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using SQLitePCL;

namespace Content.Server.GameObjects.Components.PA
{
    public class ParticleAccelerator
    {
        private IEntityManager _entityManager;
        private IMapManager _mapManager;

        public ParticleAccelerator()
        {
            _entityManager = IoCManager.Resolve<IEntityManager>();
            _mapManager = IoCManager.Resolve<IMapManager>();
        }

        private EntityUid? _EntityId;

        private ParticleAcceleratorControlBoxComponent _controlBox;
        public ParticleAcceleratorControlBoxComponent ControlBox
        {
            get => _controlBox;
            set => SetControlBox(value);
        }
        private void SetControlBox(ParticleAcceleratorControlBoxComponent value, bool skipFuelChamberCheck = false)
        {
            if(!TryAddPart(ref _controlBox, value, out var gridId)) return;

            if (!skipFuelChamberCheck &&
                TryGetPart<ParticleAcceleratorFuelChamberComponent>(gridId, Direction.South, value, out var fuelChamber))
            {
                SetFuelChamber(fuelChamber, skipControlBoxCheck: true);
            }
        }

        private ParticleAcceleratorEndCapComponent _endCap;
        public ParticleAcceleratorEndCapComponent EndCap
        {
            get => _endCap;
            set => SetEndCap(value);
        }
        private void SetEndCap(ParticleAcceleratorEndCapComponent value, bool skipFuelChamberCheck = false)
        {
            if(!TryAddPart(ref _endCap, value, out var gridId)) return;

            if (!skipFuelChamberCheck &&
                TryGetPart<ParticleAcceleratorFuelChamberComponent>(gridId, Direction.West, value, out var fuelChamber))
            {
                SetFuelChamber(fuelChamber, skipEndCapCheck: true);
            }
        }

        private ParticleAcceleratorFuelChamberComponent _fuelChamber;
        public ParticleAcceleratorFuelChamberComponent FuelChamber
        {
            get => _fuelChamber;
            set => SetFuelChamber(value);
        }
        private void SetFuelChamber(ParticleAcceleratorFuelChamberComponent value, bool skipEndCapCheck = false, bool skipPowerBoxCheck = false, bool skipControlBoxCheck = false)
        {
            if(!TryAddPart(ref _fuelChamber, value, out var gridId)) return;

            if (!skipControlBoxCheck &&
                TryGetPart<ParticleAcceleratorControlBoxComponent>(gridId, Direction.North, value, out var controlBox))
            {
                SetControlBox(controlBox, skipFuelChamberCheck: true);
            }

            if (!skipEndCapCheck &&
                TryGetPart<ParticleAcceleratorEndCapComponent>(gridId, Direction.East, value, out var endCap))
            {
                SetEndCap(endCap, skipFuelChamberCheck: true);
            }

            if (!skipPowerBoxCheck &&
                TryGetPart<ParticleAcceleratorPowerBoxComponent>(gridId, Direction.West, value, out var powerBox))
            {
                SetPowerBox(powerBox, skipFuelChamberCheck: true);
            }
        }

        private ParticleAcceleratorPowerBoxComponent _powerBox;
        public ParticleAcceleratorPowerBoxComponent PowerBox
        {
            get => _powerBox;
            set => SetPowerBox(value);
        }
        private void SetPowerBox(ParticleAcceleratorPowerBoxComponent value, bool skipFuelChamberCheck = false,
            bool skipEmitterCenterCheck = false)
        {
            if(!TryAddPart(ref _powerBox, value, out var gridId)) return;

            if (!skipFuelChamberCheck &&
                TryGetPart<ParticleAcceleratorFuelChamberComponent>(gridId, Direction.East, value, out var fuelChamber))
            {
                SetFuelChamber(fuelChamber, skipPowerBoxCheck: true);
            }

            if (!skipEmitterCenterCheck && TryGetPart(gridId, Direction.West, value,
                ParticleAcceleratorEmitterType.Center, out var emitterComponent))
            {
                SetEmitterCenter(emitterComponent, skipPowerBoxCheck: true);
            }
        }

        private ParticleAcceleratorEmitterComponent _emitterLeft;
        public ParticleAcceleratorEmitterComponent EmitterLeft
        {
            get => _emitterLeft;
            set => SetEmitterLeft(value);
        }
        private void SetEmitterLeft(ParticleAcceleratorEmitterComponent value, bool skipEmitterCenterCheck = false)
        {
            if (value != null && value.Type != ParticleAcceleratorEmitterType.Left)
            {
                Logger.Error($"Something tried adding a left Emitter that doesn't have the Emittertype left to a ParticleAccelerator (Actual Emittertype: {value.Type})");
                return;
            }

            if(!TryAddPart(ref _emitterLeft, value, out var gridId)) return;

            if (!skipEmitterCenterCheck && TryGetPart(gridId, Direction.South, value,
                ParticleAcceleratorEmitterType.Center, out var emitterComponent))
            {
                SetEmitterCenter(emitterComponent, skipEmitterLeftCheck: true);
            }
        }

        private ParticleAcceleratorEmitterComponent _emitterCenter;
        public ParticleAcceleratorEmitterComponent EmitterCenter
        {
            get => _emitterCenter;
            set => SetEmitterCenter(value);
        }
        private void SetEmitterCenter(ParticleAcceleratorEmitterComponent value, bool skipEmitterLeftCheck = false,
            bool skipEmitterRightCheck = false, bool skipPowerBoxCheck = false)
        {
            if (value != null && value.Type != ParticleAcceleratorEmitterType.Center)
            {
                Logger.Error($"Something tried adding a center Emitter that doesn't have the Emittertype center to a ParticleAccelerator (Actual Emittertype: {value.Type})");
                return;
            }

            if(!TryAddPart(ref _emitterCenter, value, out var gridId)) return;

            if (!skipEmitterLeftCheck && TryGetPart(gridId, Direction.North, value, ParticleAcceleratorEmitterType.Left,
                out var emitterLeft))
            {
                SetEmitterLeft(emitterLeft, skipEmitterCenterCheck: true);
            }

            if (!skipEmitterRightCheck && TryGetPart(gridId, Direction.South, value,
                ParticleAcceleratorEmitterType.Right,
                out var emitterRight))
            {
                SetEmitterRight(emitterRight, skipEmitterCenterCheck: true);
            }

            if (!skipPowerBoxCheck &&
                TryGetPart<ParticleAcceleratorPowerBoxComponent>(gridId, Direction.East, value, out var powerBox))
            {
                SetPowerBox(powerBox, skipEmitterCenterCheck: true);
            }
        }

        private ParticleAcceleratorEmitterComponent _emitterRight;
        public ParticleAcceleratorEmitterComponent EmitterRight
        {
            get => _emitterRight;
            set => SetEmitterRight(value);
        }
        private void SetEmitterRight(ParticleAcceleratorEmitterComponent value, bool skipEmitterCenterCheck = false)
        {
            if (value != null && value.Type != ParticleAcceleratorEmitterType.Right)
            {
                Logger.Error($"Something tried adding a right Emitter that doesn't have the Emittertype right to a ParticleAccelerator (Actual Emittertype: {value.Type})");
                return;
            }

            if(!TryAddPart(ref _emitterRight, value, out var gridId)) return;

            if (!skipEmitterCenterCheck && TryGetPart(gridId, Direction.North, value,
                ParticleAcceleratorEmitterType.Center, out var emitterComponent))
            {
                SetEmitterCenter(emitterComponent, skipEmitterRightCheck: true);
            }
        }

        private ParticleAcceleratorPowerState _power = ParticleAcceleratorPowerState.Off;
        [ViewVariables(VVAccess.ReadWrite)]
        public ParticleAcceleratorPowerState Power
        {
            get => _power;
            set
            {
                if (!IsFunctional())
                {
                    _power = ParticleAcceleratorPowerState.Off;
                    return;
                }

                _power = value;
                UpdatePartVisualStates();
            }
        }

        public bool IsFunctional()
        {
            return ControlBox != null && EndCap != null && FuelChamber != null && PowerBox != null &&
                   EmitterCenter != null && EmitterLeft != null && EmitterRight != null;
        }

        private void UpdatePartVisualStates()
        {
            UpdatePartVisualState(ControlBox);
            UpdatePartVisualState(EndCap);
            UpdatePartVisualState(FuelChamber);
            UpdatePartVisualState(PowerBox);
            UpdatePartVisualState(EmitterCenter);
            UpdatePartVisualState(EmitterLeft);
            UpdatePartVisualState(EmitterRight);
        }

        private void UpdatePartVisualState(ParticleAcceleratorPartComponent component)
        {
            if (!component.Owner.TryGetComponent<AppearanceComponent>(out var appearanceComponent))
            {
                Logger.Error($"ParticleAccelerator tried updating state of {component} but failed due to a missing AppearanceComponent");
                return;
            }
            appearanceComponent.SetData(ParticleAcceleratorVisuals.VisualState, _power);
        }

        private void Absorb(ParticleAccelerator particleAccelerator)
        {
            _controlBox ??= particleAccelerator._controlBox;
            _endCap ??= particleAccelerator._endCap;
            _fuelChamber ??= particleAccelerator._fuelChamber;
            _powerBox ??= particleAccelerator._powerBox;
            _emitterLeft ??= particleAccelerator._emitterLeft;
            _emitterCenter ??= particleAccelerator._emitterCenter;
            _emitterRight ??= particleAccelerator._emitterRight;

            particleAccelerator._controlBox = null;
            particleAccelerator._endCap = null;
            particleAccelerator._fuelChamber = null;
            particleAccelerator._powerBox = null;
            particleAccelerator._emitterLeft = null;
            particleAccelerator._emitterCenter = null;
            particleAccelerator._emitterRight = null;
        }

        private bool TryAddPart<T>(ref T partVar, T value, out GridId gridId) where T : ParticleAcceleratorPartComponent
        {
            var rawGridId = value?.Owner.Transform.Coordinates.GetGridId(_entityManager);
            if (rawGridId == GridId.Invalid || !rawGridId.HasValue)
            {
                Logger.Error($"Something tried adding a {value} that isn't in a Grid to a ParticleAccelerator");
                gridId = GridId.Invalid;
                return false;
            }
            gridId = rawGridId.Value;

            if (partVar == value) return false;

            if (partVar != null)
            {
                Logger.Error($"Something tried adding a {value} to a ParticleAccelerator that already has a {partVar} registered");
                return false;
            }

            if (typeof(T) != value.GetType())
            {
                Logger.Error($"Type mismatch when trying to add {partVar} to a ParticleAccelerator");
                return false;
            }

            _EntityId ??= value.Owner.Transform.Coordinates.EntityId;
            if (_EntityId != value.Owner.Transform.Coordinates.EntityId)
            {
                Logger.Error($"Something tried adding a {value} from a different EntityID to a ParticleAccelerator");
                return false;
            }

            partVar = value;

            if (value.ParticleAccelerator != this)
            {
                Absorb(value.ParticleAccelerator);
                value.ParticleAccelerator = this;
            }

            return true;
        }

        private bool TryGetPart<TP>(GridId gridId, Direction directionOffset, ParticleAcceleratorPartComponent value, out TP part)
            where TP : ParticleAcceleratorPartComponent
        {
            var partMapIndices = GetMapIndicesInDir(value, directionOffset);

            var entity = partMapIndices.GetEntitiesInTileFast(gridId).FirstOrDefault(obj => obj.TryGetComponent<TP>(out var part));
            part = entity?.GetComponent<TP>();
            return entity != null && part != null;
        }

        private bool TryGetPart(GridId gridId, Direction directionOffset, ParticleAcceleratorPartComponent value, ParticleAcceleratorEmitterType type, out ParticleAcceleratorEmitterComponent part)
        {
            var partMapIndices = GetMapIndicesInDir(value, directionOffset);

            var entity = partMapIndices.GetEntitiesInTileFast(gridId).FirstOrDefault(obj => obj.TryGetComponent<ParticleAcceleratorEmitterComponent>(out var p) && p.Type == type);
            part = entity?.GetComponent<ParticleAcceleratorEmitterComponent>();
            return entity != null && part != null;
        }

        private MapIndices GetMapIndicesInDir(Component comp, Direction offset)
        {
            var partDir = new Angle(comp.Owner.Transform.LocalRotation + offset.ToAngle()).GetCardinalDir();
            return comp.Owner.Transform.Coordinates.ToMapIndices(_entityManager, _mapManager).Offset(partDir);
        }

        public enum ParticleAcceleratorPowerState
        {
            Off = ParticleAcceleratorVisualState.Closed,
            Powered = ParticleAcceleratorVisualState.Powered,
            Level0 = ParticleAcceleratorVisualState.Level0,
            Level1 = ParticleAcceleratorVisualState.Level1,
            Level2 = ParticleAcceleratorVisualState.Level2,
            Level3 = ParticleAcceleratorVisualState.Level3
        }
    }
}
