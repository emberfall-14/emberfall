using Content.Server.Tesla.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Tesla.Components;

/// <summary>
/// Generates electricity from lightning bolts
/// </summary>
[RegisterComponent, Access(typeof(TeslaCoilSystem))]
public sealed partial class TeslaCoilComponent : Component
{
    /// <summary>
    /// How much power will the coil generate from a lightning strike
    /// </summary>
    // To Do: Different lightning bolts have different powers and generate different amounts of energy
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChargeFromLightning = 30000f;

    /// <summary>
    /// Spark duration.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LightningTime = TimeSpan.FromSeconds(4);

    /// <summary>
    /// When the spark visual should turn off.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LightningEndTime;

    /// <summary>
    /// Is this coil sparking right now?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsSparking;

    /// <summary>
    /// Was machine activated by user?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Enabled;

    [DataField]
    public SoundSpecifier SoundOpen = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField]
    public SoundSpecifier SoundClose = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");
}
