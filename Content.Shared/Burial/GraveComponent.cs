using Robust.Shared.Audio;

namespace Content.Shared.Burial;

[RegisterComponent]
public sealed partial class GraveComponent : Component
{
    /// <summary>
    /// How long it takes to dig this grave, without modifiers
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DigDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Modifier if digging yourself out by hand if buried alive
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DigOutByHandModifier = 0.1f;

    /// <summary>
    /// Sound to make when digging/filling this grave
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SoundPathSpecifier DigSound = new SoundPathSpecifier("/Audio/Items/shovel_dig.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };

    /// <summary>
    /// Is this grave in the process of being dug/filled?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool DiggingComplete = false;
}
