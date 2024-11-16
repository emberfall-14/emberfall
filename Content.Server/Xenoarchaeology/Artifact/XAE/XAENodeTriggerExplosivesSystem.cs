using Content.Server.Explosion.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAENodeTriggerExplosivesSystem : BaseXAESystem<XAENodeTriggerExplosivesComponent>
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAENodeTriggerExplosivesComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if(!TryComp<ExplosiveComponent>(ent, out var explosiveComp))
            return;

        _explosion.TriggerExplosive(ent, explosiveComp);
    }
}
