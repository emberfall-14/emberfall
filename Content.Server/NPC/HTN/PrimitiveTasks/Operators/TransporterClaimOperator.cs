using Content.Server.Transporters.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class TransporterClaimOperator : HTNOperator
{
    private TransporterSystem _transporters = default!;

    public string TargetKey = "Target";

    public override void Initialize(IEntitySystemManager systemManager)
    {
        base.Initialize(systemManager);
        _transporters = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TransporterSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(TargetKey);

        Logger.Debug($"Transporter {owner.ToString()} will attempt to claim item {target.ToString()}...");

        if (_transporters.ClaimItem(owner, target))
            Logger.Debug($"Transporter {owner.ToString()} has claimed item {target.ToString()}!");
        else
            Logger.Debug($"Transporter {owner.ToString()} failed to claim item {target.ToString()}.");

        return HTNOperatorStatus.Finished;
    }
}
