﻿using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeNode.PipeDirection"/>
    ///     correctly correspond.
    /// </summary>
    public class PipeNode : Node, IGasMixtureHolder
    {
        [ViewVariables]
        public PipeDirection PipeDirection => _pipeDirection;
        private PipeDirection _pipeDirection;

        [ViewVariables]
        private IPipeNet _pipeNet = PipeNet.NullNet;

        [ViewVariables]
        private bool _needsPipeNet = true;

        /// <summary>
        ///     The gases in this pipe.
        /// </summary>
        [ViewVariables]
        public GasMixture Air
        {
            get => _needsPipeNet ? LocalAir : _pipeNet.Air;
            set
            {
                if (_needsPipeNet)
                    LocalAir = value;
                else
                    _pipeNet.Air = value;
            }
        }

        /// <summary>
        ///     Stores gas in this pipe when disconnected from a <see cref="IPipeNet"/>.
        ///     Only for usage by <see cref="IPipeNet"/>s.
        /// </summary>
        [ViewVariables]
        public GasMixture LocalAir { get; set; }

        [ViewVariables]
        public float Volume { get; private set; }

        private AppearanceComponent _appearance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _pipeDirection, "pipeDirection", PipeDirection.None);
            serializer.DataField(this, x => Volume, "volume", 10);
        }

        public override void Initialize(IEntity owner)
        {
            base.Initialize(owner);
            LocalAir = new GasMixture(Volume);
            Owner.TryGetComponent(out _appearance);
            UpdateAppearance();
        }

        public void JoinPipeNet(IPipeNet pipeNet)
        {
            _pipeNet = pipeNet;
            _needsPipeNet = false;
        }

        public void ClearPipeNet()
        {
            _pipeNet = PipeNet.NullNet;
            _needsPipeNet = true;
        }

        protected override IEnumerable<Node> GetReachableNodes()
        {
            foreach (CardinalDirection direction in Enum.GetValues(typeof(CardinalDirection)))
            {
                PipeDirectionFromCardinal(direction, out var ownNeededConnection, out var theirNeededConnection);
                if ((_pipeDirection & ownNeededConnection) == PipeDirection.None)
                {
                    continue;
                }
                var pipeNodesInDirection = Owner.GetComponent<SnapGridComponent>()
                    .GetInDir((Direction) direction)
                    .Select(entity => entity.TryGetComponent<NodeContainerComponent>(out var container) ? container : null)
                    .Where(container => container != null)
                    .SelectMany(container => container.Nodes)
                    .OfType<PipeNode>()
                    .Where(pipeNode => (pipeNode._pipeDirection & theirNeededConnection) != PipeDirection.None);
                foreach (var pipeNode in pipeNodesInDirection)
                {
                    yield return pipeNode;
                }
            }
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(PipeVisuals.VisualState, new PipeVisualState(PipeDirection, ConduitLayer.Two)); //2 for middle conduit layer - placeholder while conduit layers are not implemented
        }

        private void PipeDirectionFromCardinal(CardinalDirection direction, out PipeDirection sameDir, out PipeDirection oppDir)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    sameDir = PipeDirection.North;
                    oppDir = PipeDirection.South;
                    break;
                case CardinalDirection.South:
                    sameDir = PipeDirection.South;
                    oppDir = PipeDirection.North;
                    break;
                case CardinalDirection.East:
                    sameDir = PipeDirection.East;
                    oppDir = PipeDirection.West;
                    break;
                case CardinalDirection.West:
                    sameDir = PipeDirection.West;
                    oppDir = PipeDirection.East;
                    break;
                default:
                    throw new ArgumentException("Invalid Direction.");
            }
        }

        private enum CardinalDirection
        {
            North = Direction.North,
            South = Direction.South,
            East = Direction.East,
            West = Direction.West,
        }
    }
}
