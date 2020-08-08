﻿using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.NodeGroups
{
    /// <summary>
    ///     Maintains a collection of <see cref="Node"/>s, and performs operations requiring a list of
    ///     all connected <see cref="Node"/>s.
    /// </summary>
    public interface INodeGroup
    {
        IReadOnlyList<Node> Nodes { get; }

        void AddNode(Node node);

        void RemoveNode(Node node);

        void CombineGroup(INodeGroup newGroup);

        void RemakeGroup();
    }

    [NodeGroup(NodeGroupID.Default)]
    public class BaseNodeGroup : INodeGroup
    {
        [ViewVariables]
        public IReadOnlyList<Node> Nodes => _nodes;
        private readonly List<Node> _nodes = new List<Node>();

        [ViewVariables]
        public int NodeCount => Nodes.Count;

        public static readonly INodeGroup NullGroup = new NullNodeGroup();

        public void AddNode(Node node)
        {
            _nodes.Add(node);
            OnAddNode(node);
        }

        public void RemoveNode(Node node)
        {
            _nodes.Remove(node);
            OnRemoveNode(node);
            IoCManager.Resolve<INodeGroupManager>().AddDirtyNodeGroup(this);
        }

        public void CombineGroup(INodeGroup newGroup)
        {
            if (newGroup.Nodes.Count < Nodes.Count)
            {
                newGroup.CombineGroup(this);
                return;
            }
            OnGivingNodesForRemake();
            foreach (var node in Nodes)
            {
                node.NodeGroup = newGroup;
            }
        }

        /// <summary>
        ///     Causes all <see cref="Node"/>s to remake their groups. Called when a <see cref="Node"/> is removed
        ///     and may have split a group in two, so multiple new groups may need to be formed.
        /// </summary>
        public void RemakeGroup()
        {
            foreach (var node in Nodes)
            {
                node.ClearNodeGroup();
            }
            var newGroups = new List<INodeGroup>();
            foreach (var node in Nodes)
            {
                if (node.TryAssignGroupIfNeeded())
                {
                    node.SpreadGroup();
                    newGroups.Add(node.NodeGroup);
                }
            }
            AfterRemake(newGroups);
        }

        protected virtual void OnAddNode(Node node) { }
        
        protected virtual void OnRemoveNode(Node node) { }

        protected virtual void OnGivingNodesForRemake() { }

        protected virtual void AfterRemake(IEnumerable<INodeGroup> newGroups) { }

        private class NullNodeGroup : INodeGroup
        {
            public IReadOnlyList<Node> Nodes => _nodes;
            private readonly List<Node> _nodes = new List<Node>();
            public void AddNode(Node node) { }
            public void CombineGroup(INodeGroup newGroup) { }
            public void RemoveNode(Node node) { }
            public void RemakeGroup() { }
        }
    }
}
