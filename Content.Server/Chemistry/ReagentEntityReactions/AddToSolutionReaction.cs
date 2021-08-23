using System.Collections.Generic;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Chemistry.ReagentEntityReactions
{
    [UsedImplicitly]
    public class AddToSolutionReaction : ReagentEntityReaction
    {
        [DataField("reagents", true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<ReagentPrototype>))]
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly HashSet<string> _reagents = new();

        protected override void React(IEntity entity, ReagentPrototype reagent, ReagentUnit volume, Solution? source)
        {
            // TODO see if this is correct
            if (!EntitySystem.Get<SolutionContainerSystem>()
                    .TryGetSolution(entity, "reagents", out var solutionContainer)
                || (_reagents.Count > 0 && !_reagents.Contains(reagent.ID))) return;

            if (EntitySystem.Get<SolutionContainerSystem>()
                .TryAddReagent(solutionContainer, reagent.ID, volume, out var accepted))
                source?.RemoveReagent(reagent.ID, accepted);
        }
    }
}
