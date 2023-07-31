using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectileComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("impactEffect", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ImpactEffect;

    /// <summary>
    /// User that shot this projectile.
    /// </summary>
    [DataField("shooter"), AutoNetworkedField] public EntityUid Shooter;

    /// <summary>
    /// Weapon used to shoot.
    /// </summary>
    [DataField("weapon"), AutoNetworkedField]
    public EntityUid Weapon;

    [DataField("ignoreShooter"), AutoNetworkedField]
    public bool IgnoreShooter = true;

    [DataField("damage", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField("deleteOnCollide")]
    public bool DeleteOnCollide = true;

    [DataField("canPenetrate")]
    public bool CanPenetrate = false;

    [DataField("canPenetrateWall")]
    public bool CanPenetrateWall = false;

    [DataField("penetrationStrength")]
    public float PenetrationStrength = 0f;

    [DataField("penetrationDamageFalloffMultiplier")]
    public float PenetrationDamageFalloffMultiplier = 0.5f;


    [DataField("weaponModifierAdded")]
    public bool DamageModifierAdded = false;

    [DataField("penetrationModifierAdded")]
    public bool PenetrationModifierAdded = false;

    // Get that juicy FPS hit sound
    [DataField("soundHit")] public SoundSpecifier? SoundHit;

    [DataField("soundForce")]
    public bool ForceSound = false;

    public bool DamagedEntity;
}
