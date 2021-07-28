using System.Linq;
using Content.Server.Body.Circulatory;
using Content.Shared.Body.Networks;
using Content.Shared.Chemistry.Metabolizable;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Behavior
{
    /// <summary>
    /// Metabolizes reagents in <see cref="SharedBloodstreamComponent"/> after they are digested.
    /// </summary>
    public class LiverBehavior : MechanismBehavior
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private float _accumulatedFrameTime;

        /// <summary>
        ///     How frequently to metabolise blood contents.
        /// </summary>
        private float _updateInterval = 1.0f;

        /// <summary>
        ///     Whether the liver is functional.
        /// </summary>
        //[ViewVariables] private bool _liverFailing = false;

        /// <summary>
        ///     Modifier for alcohol damage.
        /// </summary>
        //[DataField("alcoholLethality")]
        //[ViewVariables] private float _alcoholLethality = 0.005f;

        /// <summary>
        ///     Modifier for alcohol damage.
        /// </summary>
        //[DataField("alcoholExponent")]
        //[ViewVariables] private float _alcoholExponent = 1.6f;

        /// <summary>
        ///     Toxin volume that can be purged without damage.
        /// </summary>
        //[DataField("toxinTolerance")]
        //[ViewVariables] private float _toxinTolerance = 3f;

        /// <summary>
        ///     Toxin damage modifier.
        /// </summary>
        //[DataField("toxinLethality")]
        //[ViewVariables] private float _toxinLethality = 0.01f;

        /// <summary>
        ///     Loops through each reagent in _internalSolution,
        ///     and calls <see cref="IMetabolizable.Metabolize"/> for each of them.
        ///     Also handles toxins and alcohol.
        /// </summary>
        /// <param name="frameTime">
        ///     The time since the last update in seconds.
        /// </param>
        public override void Update(float frameTime)
        {
            if (Body == null)
            {
                return;
            }

            _accumulatedFrameTime += frameTime;

            // Update at most once every _updateInterval
            if (_accumulatedFrameTime < _updateInterval)
            {
                return;
            }

            _accumulatedFrameTime -= _updateInterval;

            if (!Body.Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
            {
                return;
            }

            if (bloodstream.Solution.CurrentVolume <= ReagentUnit.Zero)
            {
                return;
            }

            // Run metabolism for each reagent, remove metabolized reagents
            // Using ToList here lets us edit reagents while iterating
            foreach (var reagent in bloodstream.Solution.ReagentList.ToList())
            {
                if (!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? prototype))
                {
                    continue;
                }

                //TODO BODY Check if it's a Toxin. If volume < _toxinTolerance, just remove it. If greater, add damage = volume * _toxinLethality
                //TODO BODY Check if it has BoozePower > 0. Affect drunkenness, apply damage. Proposed formula (SS13-derived): damage = sqrt(volume) * BoozePower^_alcoholExponent * _alcoholLethality / 10
                //TODO BODY Liver failure.

                //TODO Make sure reagent prototypes actually have the toxin and boozepower vars set.

                // Run metabolism code for each reagent
                foreach (var metabolizable in prototype.Metabolism)
                {
                    var reagentDelta = metabolizable.Metabolize(Body.Owner, reagent.ReagentId, _updateInterval);
                    bloodstream.Solution.TryRemoveReagent(reagent.ReagentId, reagentDelta);
                }
            }
        }
    }
}
