using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Makes a mob glow.
/// </summary>
public sealed partial class PlantUnviable : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        plantholder.Seed.Viable = !plantholder.Seed.Viable;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
