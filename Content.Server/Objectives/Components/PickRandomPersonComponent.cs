using Content.Server.Objectives.Systems;
using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person.
/// </summary>
[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class PickRandomPersonComponent : Component
{
    /// <summary>
    /// If non-null, a player must have a role matching this whitelist to be chosen.
    /// </summary>
    [DataField]
    public EntityWhitelist? RoleWhitelist;

    /// <summary>
    /// If non-null, a player cannot have a role matching this blacklist to be chosen.
    /// </summary>
    [DataField]
    public EntityWhitelist? RoleBlacklist;
}
