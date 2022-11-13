﻿using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MechComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot RiderSlot = default!;
    [ViewVariables]
    public readonly string RiderSlotId = "mech-rider-slot";

    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> Modules = new();

    #region Visualizer States
    [DataField("baseState")]
    public string? BaseState;
    [DataField("openState")]
    public string? OpenState;
    [DataField("brokenState")]
    public string? BrokenState;
    #endregion
}

[Serializable, NetSerializable]
public enum MechVisuals : byte
{
    Open, //whether or not it's open and has a rider
    Broken //if it broke and no longer works.
}

[Serializable, NetSerializable]
public enum MechVisualLayers : byte
{
    Base
}
