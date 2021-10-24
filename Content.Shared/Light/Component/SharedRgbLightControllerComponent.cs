using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Light.Component
{
    [NetworkedComponent]
    [RegisterComponent]
    public class SharedRgbLightControllerComponent : Robust.Shared.GameObjects.Component
    {
        public override string Name => "RgbLightController";

        [DataField("cycleRate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float CycleRate { get; set; } = 10.0f;
    }
}
