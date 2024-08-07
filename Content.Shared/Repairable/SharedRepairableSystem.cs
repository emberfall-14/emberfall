using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public abstract partial class SharedRepairableSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed partial class RepairFinishedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    protected sealed partial class RepairByReplacementFinishedEvent : SimpleDoAfterEvent
    {
    }
}

