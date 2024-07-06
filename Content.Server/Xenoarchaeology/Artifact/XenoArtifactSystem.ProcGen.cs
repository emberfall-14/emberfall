using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Artifact;

public sealed partial class XenoArtifactSystem
{
    private readonly List<XenoArchTriggerPrototype> _triggerPool = new();

    private void GenerateArtifactStructure(Entity<XenoArtifactComponent> ent)
    {
        var nodeCount = ent.Comp.NodeCount.Next(RobustRandom);
        CreateTriggerPool(ent, ref nodeCount);
        ResizeNodeGraph(ent, nodeCount);
        while (nodeCount > 0)
        {
            GenerateArtifactSegment(ent, ref nodeCount);
        }

        RebuildNodeData((ent, ent));
    }

    private void CreateTriggerPool(Entity<XenoArtifactComponent> ent, ref int size)
    {
        _triggerPool.Clear();
        var weightsProto = PrototypeManager.Index(ent.Comp.TriggerWeights);
        var weights = new Dictionary<string, float>(weightsProto.Weights);

        while (_triggerPool.Count < size)
        {
            // OOPS! We ran out of triggers.
            if (weights.Count == 0)
            {
                Log.Error($"Insufficient triggers for generating {ToPrettyString(ent)}! Needed {size} but had {_triggerPool.Count}");
                size = _triggerPool.Count;
                return;
            }

            var triggerId = RobustRandom.Pick(weights);
            weights.Remove(triggerId);
            var trigger = PrototypeManager.Index<XenoArchTriggerPrototype>(triggerId);
            if (_entityWhitelist.IsWhitelistFail(trigger.Whitelist, ent))
                continue;

            _triggerPool.Add(trigger);
        }
    }

    private void GenerateArtifactSegment(Entity<XenoArtifactComponent> ent, ref int nodeCount)
    {
        var segmentSize = GetArtifactSegmentSize(ent, nodeCount);
        nodeCount -= segmentSize;
        PopulateArtifactSegmentRecursive(ent, ref segmentSize, ensureLayerConnected: true);
    }

    private List<Entity<XenoArtifactNodeComponent>> PopulateArtifactSegmentRecursive(
        Entity<XenoArtifactComponent> ent,
        ref int segmentSize,
        int layerMinMod = 0,
        int layerMaxMod = 0,
        bool ensureLayerConnected = false,
        int iteration = 0)
    {
        if (segmentSize == 0)
            return new();

        var layerMin = Math.Min(ent.Comp.NodesPerSegmentLayer.Min + layerMinMod, segmentSize);
        var layerMax = Math.Min(ent.Comp.NodesPerSegmentLayer.Max + layerMaxMod, segmentSize);

        // Default to one node if we had shenanigans and ended up with weird layer counts.
        var nodeCount = 1;
        if (layerMax >= layerMin)
            nodeCount = RobustRandom.Next(layerMin, layerMax + 1); // account for non-inclusive max

        segmentSize -= nodeCount;
        var nodes = new List<Entity<XenoArtifactNodeComponent>>();
        for (var i = 0; i < nodeCount; i++)
        {
            nodes.Add(CreateRandomNode(ent, iteration));
        }

        var minMod = ent.Comp.NodeContainer.Count < 3 ? 0 : 1; // Try to stop boring linear generation.
        var maxMod = nodes.Count / 2; // cumulative modifier to enable slight growth for something like 3 -> 4
        var successors = PopulateArtifactSegmentRecursive(
            ent,
            ref segmentSize,
            layerMinMod: minMod,
            layerMaxMod: maxMod,
            iteration: iteration + 1);

        if (successors.Count == 0)
            return nodes;

        // TODO: this doesn't actually make sure that the segment is interconnected.
        // You can still occasionally get orphaned segments.

        // We do the picks from node -> successor and from successor -> node to ensure that no nodes get orphaned without connections.
        foreach (var successor in successors)
        {
            var node = RobustRandom.Pick(nodes);
            AddEdge((ent, ent), node, successor, dirty: false);
        }

        if (ensureLayerConnected)
        {
            foreach (var node in nodes)
            {
                var successor = RobustRandom.Pick(successors);
                AddEdge((ent, ent), node, successor, dirty: false);
            }
        }

        var reverseScatterCount = ent.Comp.ReverseScatterPerLayer.Next(RobustRandom);
        for (var i = 0; i < reverseScatterCount; i++)
        {
            var node = RobustRandom.Pick(nodes);
            var successor = RobustRandom.Pick(successors);
            AddEdge((ent, ent), node, successor, dirty: false);
        }

        return nodes;
    }

    private int GetArtifactSegmentSize(Entity<XenoArtifactComponent> ent, int nodeCount)
    {
        // Make sure we can't generate a single segment artifact.
        // We always want to have at least 2 segments. For variety.
        var segmentMin = ent.Comp.SegmentSize.Min;
        var segmentMax = Math.Min(ent.Comp.SegmentSize.Max, Math.Max(nodeCount / 2, segmentMin));

        var segmentSize = RobustRandom.Next(segmentMin, segmentMax + 1); // account for non-inclusive max
        var remainder = nodeCount - segmentSize;

        // If our next segment is going to be undersized, then we just absorb it into this segment.
        if (remainder < ent.Comp.SegmentSize.Min)
            segmentSize += remainder;

        // Sanity check to make sure we don't exceed the node count. (it shouldn't happen prior anyway but oh well)
        segmentSize = Math.Min(nodeCount, segmentSize);

        return segmentSize;
    }

    //todo: move this into system.node or something.
    private Entity<XenoArtifactNodeComponent> CreateRandomNode(Entity<XenoArtifactComponent> ent, int depth = 0)
    {
        var proto = PrototypeManager.Index(ent.Comp.EffectWeights).Pick(RobustRandom);

        AddNode((ent, ent), proto, out var nodeEnt, dirty: false);
        DebugTools.Assert(nodeEnt.HasValue, "Failed to create node on artifact.");

        var trigger = RobustRandom.PickAndTake(_triggerPool);

        nodeEnt.Value.Comp.Depth = depth;
        nodeEnt.Value.Comp.TriggerTip = trigger.Tip;
        EntityManager.AddComponents(nodeEnt.Value, trigger.Components);

        //Dirty(nodeEnt.Value);
        return nodeEnt.Value;
    }
}
