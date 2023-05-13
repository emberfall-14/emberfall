using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Marker;

public sealed class DamageMarkerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, StartCollideEvent>(OnMarkerCollide);
        SubscribeLocalEvent<DamageMarkerComponent, EntityUnpausedEvent>(OnMarkerUnpaused);
        SubscribeLocalEvent<DamageMarkerComponent, AttackedEvent>(OnMarkerAttacked);
    }

    private void OnMarkerAttacked(EntityUid uid, DamageMarkerComponent component, AttackedEvent args)
    {
        if (component.Marker != args.Used)
            return;

        args.BonusDamage += component.Damage;
        RemCompDeferred<DamageMarkerComponent>(uid);
        _audio.PlayPredicted(component.Sound, uid, args.User);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamageMarkerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime > _timing.CurTime)
                continue;

            RemCompDeferred<DamageMarkerComponent>(uid);
        }
    }

    private void OnMarkerUnpaused(EntityUid uid, DamageMarkerComponent component, ref EntityUnpausedEvent args)
    {
        component.EndTime += args.PausedTime;
    }

    private void OnMarkerCollide(EntityUid uid, DamageMarkerOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard ||
            args.OurFixture.ID != SharedProjectileSystem.ProjectileFixture ||
            component.Whitelist?.IsValid(args.OtherEntity, EntityManager) == false ||
            !TryComp<ProjectileComponent>(uid, out var projectile) ||
            projectile.Weapon == null)
        {
            return;
        }

        // Markers are exclusive, deal with it.
        var marker = EnsureComp<DamageMarkerComponent>(args.OtherEntity);
        marker.Damage = new DamageSpecifier(component.Damage);
        marker.Marker = projectile.Weapon.Value;
        marker.EndTime = _timing.CurTime + component.Duration;
        Dirty(marker);
    }
}
