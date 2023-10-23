using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PAI;

/// <summary>
/// pAIs, or Personal AIs, are essentially portable ghost role generators.
/// In their current implementation in SS14, they create a ghost role anyone can access,
/// and that a player can also "wipe" (reset/kick out player).
/// Theoretically speaking pAIs are supposed to use a dedicated "offer and select" system,
///  with the player holding the pAI being able to choose one of the ghosts in the round.
/// This seems too complicated for an initial implementation, though,
///  and there's not always enough players and ghost roles to justify it.
/// All logic in PAISystem.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PAIComponent : Component
{
    /// <summary>
    /// The last person who activated this PAI.
    /// Used for assigning the name.
    /// </summary>
    [DataField("lastUser"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? LastUser;

    [DataField("midiActionId", serverOnly: true,
        customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? MidiActionId = "ActionPAIPlayMidi";

    [DataField("midiAction", serverOnly: true)] // server only, as it uses a server-BUI event !type
    public EntityUid? MidiAction;

    /// <summary>
    /// When microwaved there is this chance to brick the pai, kicking out its player and preventing it from being used again.
    /// </summary>
    [DataField("brickChance")]
    public float BrickChance = 0.5f;

    /// <summary>
    /// Locale id for the popup shown when the pai gets bricked.
    /// </summary>
    [DataField("brickPopup")]
    public string BrickPopup = "pai-system-brick-popup";

    /// <summary>
    /// Locale id for the popup shown when the pai is microwaved but does not get bricked.
    /// </summary>
    [DataField("scramblePopup")]
    public string ScramblePopup = "pai-system-scramble-popup";
}
