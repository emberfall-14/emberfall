using Content.Shared.Actions.ActionTypes;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Sericulture;

/// <summary>
/// Should be applied to any mob that you want to be able to produce any material with an action and the cost of hunger.
/// TODO: Probably adjust this to utilize organs?
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSericultureSystem))]
public sealed partial class SericultureComponent : Component
{
    /// <summary>
    /// The text that pops up whenever sericulture fails for not having enough hunger.
    /// </summary>
    [DataField("popupText"), ViewVariables(VVAccess.ReadWrite)]
    public string PopupText = "sericulture-failure-hunger";

    /// <summary>
    /// What will be produced at the end of the action.
    /// </summary>
    [DataField("entityProduced", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string EntityProduced = string.Empty;

    /// <summary>
    /// The <see cref="InstantActionPrototype"/> needed to actually preform sericulture. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField("actionProto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string ActionProto = string.Empty;

    /// <summary>
    /// How long will it take to make.
    /// </summary>
    [DataField("productionLength"), ViewVariables(VVAccess.ReadWrite)]
    public float ProductionLength = 3f;

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current hunger of the mob doing sericulture.
    /// </summary>
    [DataField("hungerCost"), ViewVariables(VVAccess.ReadWrite)]
    public float HungerCost = 5f;

    /// <summary>
    /// The lowest hunger threshold that this mob can be in before it's allowed to spin silk.
    /// </summary>
    [DataField("minHungerThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public HungerThreshold MinHungerThreshold = HungerThreshold.Okay;
}
