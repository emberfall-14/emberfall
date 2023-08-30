using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor;

[RegisterComponent, NetworkedComponent]
public sealed partial class TrayGogglesComponent : Component
{
    /// <summary>
    ///     Whether the scanner is currently on.
    /// </summary>
    [ViewVariables, DataField("enabled")] public bool Enabled;

    /// <summary>
    ///     Radius in which the scanner will reveal entities. Centered on the <see cref="LastLocation"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 4f;
}

