using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[NetworkedComponent]
[AutoGenerateComponentState]
[RegisterComponent]
public sealed partial class GasMinerComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Enabled = true;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Idle = false;

    /// <summary>
    ///      If the number of moles in the external environment exceeds this number, no gas will be mined.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaxExternalAmount = float.PositiveInfinity;

    /// <summary>
    ///      If the pressure (in kPA) of the external environment exceeds this number, no gas will be mined.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaxExternalPressure = Atmospherics.GasMinerDefaultMaxExternalPressure;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public Gas? SpawnGas = null;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float SpawnTemperature = Atmospherics.T20C;

    /// <summary>
    ///     Number of moles created per second when the miner is working.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float SpawnAmount = Atmospherics.MolesCellStandard * 20f;
}
