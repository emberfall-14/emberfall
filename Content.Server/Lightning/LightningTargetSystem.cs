using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// The component allows lightning to strike this target. And determining the behavior of the target when struck by lightning.
/// </summary>
public sealed class LightningTargetSystem : EntitySystem
{

    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningTargetComponent, HittedByLightningEvent>(OnHittedByLightning);
    }

    private void OnHittedByLightning(EntityUid uid, LightningTargetComponent component, ref HittedByLightningEvent args)
    {

        if (!component.LightningExplode)
            return;

        _explosionSystem.QueueExplosion(
            Transform(uid).MapPosition,
            component.ExplosionPrototype,
            component.TotalIntensity, component.Dropoff,
            component.MaxTileIntensity,
            canCreateVacuum: false);
    }
}
