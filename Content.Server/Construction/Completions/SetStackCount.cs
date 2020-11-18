﻿#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SetStackCount : IGraphAction
    {
        public int Amount { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;
            if(!entity.TryGetComponent(out StackComponent? stackComponent)) return;

            if (Amount > stackComponent.MaxCount)
            {
                Amount = stackComponent.MaxCount;
                Logger.Error("StackCount is bigger than maximum stack capacity, for entity " + entity.Name);
            }

            stackComponent.Count = Amount;
        }
    }
}
