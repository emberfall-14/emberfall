using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
/// Stores the ninja's objectives on the mind so if they die the rest of the greentext persists.
/// </summary>
[RegisterComponent, ExclusiveAntagonist]
public sealed partial class NinjaRoleComponent : AntagonistRoleComponent
{
}
