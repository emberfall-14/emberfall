using System;
using System.Collections.Generic;
using Content.Server.AI.Operators;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.AI.Utility.Actions
{
    /// <summary>
    /// The same DSE can be used across multiple actions.
    /// </summary>
    public abstract class UtilityAction : IAiUtility
    {
        /// <summary>
        /// If we're trying to find a new action can we replace a currently running one with one of the same type.
        /// e.g. If you're already wandering you don't want to replace it with a different wander.
        /// </summary>
        public virtual bool CanOverride => false;

        /// <summary>
        /// This is used to sort actions; if there's a top-tier action available we won't bother checking the lower tiers.
        /// Threshold doesn't necessarily mean we'll do an action at a higher threshold;
        /// if it's really un-optimal (i.e. low score) then we'll also check lower tiers
        /// </summary>
        /// Guidelines:
        // Idle should just be flavor-stuff if we reeaaalllyyy have nothing to do
        // Idle = 1
        // Normal = 5
        // Needs = 10
        // Combat prep (e.g. grabbing weapons) = 20
        // Combat = 30
        // Danger (e.g. dodging a grenade) = 50
        public virtual float Bonus { get; protected set; } = 1.0f;

        protected IEntity Owner { get; }

        /// <summary>
        /// All the considerations are multiplied together to get the final score; a consideration of 0.0 means the action is not possible.
        /// Ideally you put anything that's easy to assess and can cause an early-out first just so the rest aren't evaluated.
        /// </summary>
        protected abstract Consideration[] Considerations { get; }

        /// <summary>
        /// To keep the operators simple we can chain them together here, e.g. move to can be chained with other operators.
        /// </summary>
        public Queue<AiOperator> ActionOperators { get; protected set; }

        /// <summary>
        /// Sometimes we may need to set the target for an action or the likes.
        /// This is mainly useful for expandable states so each one can have a separate target.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void UpdateBlackboard(Blackboard context) {}

        protected UtilityAction(IEntity owner)
        {
            Owner = owner;
        }
        
        public virtual void Shutdown() {}

        /// <summary>
        /// If this action is chosen then setup the operators to run. This also allows for operators to be reset.
        /// </summary>
        public abstract void SetupOperators(Blackboard context);

        // Call the task's operator with Execute and get the outcome
        public Outcome Execute(float frameTime)
        {
            if (!ActionOperators.TryPeek(out var op))
            {
                return Outcome.Success;
            }

            op.TryStartup();
            var outcome = op.Execute(frameTime);

            switch (outcome)
            {
                case Outcome.Success:
                    op.Shutdown(outcome);
                    ActionOperators.Dequeue();
                    break;
                case Outcome.Continuing:
                    break;
                case Outcome.Failed:
                    op.Shutdown(outcome);
                    ActionOperators.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return outcome;
        }

        /// <summary>
        /// AKA the Decision Score Evaluator (DSE)
        /// This is where the magic happens
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bonus"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public float GetScore(Blackboard context, float min)
        {
            UpdateBlackboard(context);
            DebugTools.Assert(Considerations.Length > 0);
            // I used the IAUS video although I did have some confusion on how to structure it overall
            // as some of the slides seemed contradictory

            // Ideally we should early-out each action as cheaply as possible if it's not valid

            // We also need some way to tell if the action isn't going to
            // have a better score than the current action (if applicable) and early-out that way as well.

            // 23:00 Building a better centaur
            var finalScore = 1.0f;
            var minThreshold = min / Bonus;
            var modificationFactor = 1.0f - 1.0f / Considerations.Length;
            // See 10:09 for this and the adjustments

            foreach (var consideration in Considerations)
            {
                var score = consideration.GetScore(context);
                var makeUpValue = (1.0f - score) * modificationFactor;
                var adjustedScore = score + makeUpValue * score;
                var response = consideration.ComputeResponseCurve(adjustedScore);

                finalScore *= response;

                DebugTools.Assert(!float.IsNaN(response));

                // The score can only ever go down from each consideration so if we're below minimum no point continuing.
                if (0.0f >= finalScore || finalScore < minThreshold) {
                    return 0.0f;
                }
            }

            DebugTools.Assert(finalScore <= 1.0f);

            return finalScore * Bonus;
        }
    }
}
