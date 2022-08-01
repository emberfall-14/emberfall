using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.AI.Components;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.AI.Steering;
using Content.Server.AI.Systems;
using Robust.Shared.Map;

namespace Content.Server.AI.HTN.PrimitiveTasks;

/// <summary>
/// Moves an NPC to the specified target key. Hands the actual steering off to NPCSystem.Steering
/// </summary>
public sealed class MoveToOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private NPCSteeringSystem _steering = default!;
    private PathfindingSystem _pathfind = default!;

    /// <summary>
    /// Should we assume the MovementTarget is reachable during planning or should we pathfind to it?
    /// </summary>
    [ViewVariables, DataField("pathfindInPlanning")]
    public bool PathfindInPlanning = true;

    /// <summary>
    /// When we're finished moving to the target should we remove its key?
    /// </summary>
    [ViewVariables, DataField("removeKeyOnFinish")]
    public bool RemoveKeyOnFinish = true;

    /// <summary>
    /// Target Coordinates to move to. This gets removed after execution.
    /// </summary>
    [ViewVariables, DataField("key")]
    public string TargetKey = "MovementTarget";

    /// <summary>
    /// Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [ViewVariables, DataField("pathfindKey")]
    public string PathfindKey = "MovementPathfind";

    private const string MovementCancelToken = "MovementCancelToken";

    public override void Initialize()
    {
        base.Initialize();
        var sysManager = IoCManager.Resolve<IEntitySystemManager>();
        _pathfind = sysManager.GetEntitySystem<PathfindingSystem>();
        _steering = sysManager.GetEntitySystem<NPCSteeringSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard)
    {
        if (!PathfindInPlanning)
        {
            return (true, new Dictionary<string, object>()
            {
                {NPCBlackboard.OwnerCoordinates, blackboard.GetValue<EntityCoordinates>(TargetKey)}
            });
        }

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityCoordinates>(TargetKey, out var targetCoordinates))
            return (false, null);

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform) ||
            !_entManager.TryGetComponent<PhysicsComponent>(owner, out var body))
            return (false, null);

        if (!_mapManager.TryGetGrid(xform.GridUid, out var ownerGrid) ||
            !_mapManager.TryGetGrid(targetCoordinates.GetGridUid(_entManager), out var targetGrid) ||
            ownerGrid != targetGrid)
        {
            return (false, null);
        }

        var access = blackboard.GetValueOrDefault<ICollection<string>>(NPCBlackboard.Access) ?? new List<string>();

        var job = _pathfind.RequestPath(
            new PathfindingArgs(
                blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
                access,
                body.CollisionMask,
                ownerGrid.GetTileRef(xform.Coordinates),
                ownerGrid.GetTileRef(targetCoordinates)), CancellationToken.None);

        job.Run();

        await job.AsTask;

        if (job.Result == null)
            return (false, null);

        return (true, new Dictionary<string, object>()
        {
            {NPCBlackboard.OwnerCoordinates, targetCoordinates},
            {PathfindKey, job.Result}
        });
    }

    // Given steering is complicated we'll hand it off to a dedicated system rather than this singleton operator.

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        // Re-use the path we may have if applicable.
        var comp = _steering.Register(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner), blackboard.GetValue<EntityCoordinates>(TargetKey));

        if (blackboard.TryGetValue<Queue<TileRef>>(PathfindKey, out var path))
        {
            if (blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates))
            {
                _steering.PrunePath(coordinates, path);
            }

            comp.CurrentPath = path;
        }
    }

    public override void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.Shutdown(blackboard, status);

        // Cleanup the blackboard and remove steering.
        if (blackboard.TryGetValue<CancellationTokenSource>(MovementCancelToken, out var cancelToken))
        {
            cancelToken.Cancel();
            blackboard.Remove<CancellationTokenSource>(MovementCancelToken);
        }

        // OwnerCoordinates is only used in planning so dump it.
        blackboard.Remove<EntityCoordinates>(NPCBlackboard.OwnerCoordinates);
        blackboard.Remove<EntityCoordinates>(TargetKey);
        blackboard.Remove<Queue<TileRef>>(PathfindKey);

        if (RemoveKeyOnFinish)
        {
            blackboard.Remove<EntityCoordinates>(TargetKey);
        }

        _entManager.RemoveComponent<NPCSteeringComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<NPCSteeringComponent>(owner, out var steering))
            return HTNOperatorStatus.Failed;

        return steering.Status switch
        {
            SteeringStatus.InRange => HTNOperatorStatus.Finished,
            SteeringStatus.NoPath => HTNOperatorStatus.Failed,
            SteeringStatus.Moving => HTNOperatorStatus.Continuing,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
