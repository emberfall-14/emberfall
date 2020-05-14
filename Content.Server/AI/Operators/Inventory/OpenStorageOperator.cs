using Content.Server.AI.Utility;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// If the target is in EntityStorage will open its parent container
    /// </summary>
    public sealed class OpenStorageOperator : AiOperator
    {
        private readonly IEntity _owner;
        private readonly IEntity _target;
        
        public OpenStorageOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }
        
        public override Outcome Execute(float frameTime)
        {
            if ((_target.Transform.GridPosition.Position - _owner.Transform.GridPosition.Position).Length >
                InteractionSystem.InteractionRange)
            {
                return Outcome.Failed;
            }

            if (!ContainerHelpers.TryGetContainer(_target, out var container))
            {
                return Outcome.Success;
            }

            if (!container.Owner.TryGetComponent(out EntityStorageComponent storageComponent) || 
                storageComponent.Locked)
            {
                return Outcome.Failed;
            }
            
            if (!storageComponent.Open)
            {
                storageComponent.ToggleOpen();
            }
            
            var blackboard = UtilityAiHelpers.GetBlackboard(_owner);
            blackboard?.GetState<LastOpenedStorageState>().SetValue(container.Owner);
            
            return Outcome.Success;
        }
    }
}