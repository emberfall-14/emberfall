using Content.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Inventory
{
    public class DropHandItemsOperator : AiOperator
    {
        private readonly IEntity _owner;

        public DropHandItemsOperator(IEntity owner)
        {
            _owner = owner;
        }

        public override Outcome Execute(float frameTime)
        {
            _owner.TryGetComponent(out HandsComponent handsComponent);
            foreach (var item in handsComponent.GetAllHeldItems())
            {
                handsComponent.Drop(item.Owner);
            }

            return Outcome.Success;
        }
    }
}
