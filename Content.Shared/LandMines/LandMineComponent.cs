using Robust.Shared.Audio;

namespace Content.Shared.LandMines;

[RegisterComponent]
public sealed partial class LandMineComponent : Component
{
    /// <summary>
    /// Trigger sound effect when stepping onto landmine
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Is the land mine armed and dangerous?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Armed = false;
}

[Serializable, NetSerializable]
public enum LandMineVisuals
{
    Armed,
}
