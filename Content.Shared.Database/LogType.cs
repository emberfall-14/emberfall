namespace Content.Shared.Database;

// DO NOT CHANGE THE NUMERIC VALUES OF THESE
public enum LogType
{
    Unknown = 0, // do not use
    // DamageChange = 1
    Damaged = 2,
    Healed = 3,
    Slip = 4,
    EventAnnounced = 5,
    EventStarted = 6,
    EventRan = 16,
    EventStopped = 7,
    Verb = 19,
    ShuttleCalled = 8,
    ShuttleRecalled = 9,
    ExplosiveDepressurization = 10,
    Respawn = 13,
    RoundStartJoin = 14,
    LateJoin = 15,
    ChemicalReaction = 17,
    ReagentEffect = 18,
    CanisterValve = 20,
    CanisterPressure = 21,
    CanisterPurged = 22,
    CanisterTankEjected = 23,
    CanisterTankInserted = 24,
    DisarmedAction = 25,
    DisarmedKnockdown = 26,
    AttackArmedClick = 27,
    AttackArmedWide = 28,
    AttackUnarmedClick = 29,
    AttackUnarmedWide = 30,
    InteractHand = 31,
    InteractActivate = 32,
    Throw = 33,
    Landed = 34,
    ThrowHit = 35,
    Pickup = 36,
    Drop = 37,
    BulletHit = 38,
    ForceFeed = 40, // involuntary
    Ingestion = 53, // voluntary
    MeleeHit = 41,
    HitScanHit = 42,
    Suicide = 43,
    Explosion = 44,
    Radiation = 45, // Unused
    Barotrauma = 46,
    Flammable = 47,
    Asphyxiation = 48,
    Temperature = 49,
    Hunger = 50,
    Thirst = 51,
    Electrocution = 52,
    CrayonDraw = 39,
}
