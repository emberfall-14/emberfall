using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// A drink or food that can be opened.
/// Starts closed, open it with Z or E.
/// </summary>
[RegisterComponent, Access(typeof(OpenableSystem))]
public sealed partial class OpenableComponent : Component
{
    /// <summary>
    /// Whether this drink or food is opened or not.
    /// Drinks can only be drunk or poured from/into when open, and food can only be eaten when open.
    /// </summary>
    [DataField("opened"), ViewVariables(VVAccess.ReadWrite)]
    public bool Opened;

    /// <summary>
    /// Sound played when opening.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("canOpenSounds");
}
