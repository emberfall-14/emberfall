using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Computer;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Guardian
{
    [RegisterComponent]
    public class GuardianCreatorComponent : Component
    {
        public override string Name => "GuardianCreator";

        //The injected guardian prototype entity
        [ViewVariables] [DataField("GuardianID")] public string GuardianType { get; set; } = default!;


    }
}
