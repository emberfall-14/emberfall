using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;

namespace Content.Server.Objectives.Requirements;

[DataDefinition]
public sealed class NinjaRequirement : IObjectiveRequirement
{
    public bool CanBeAssigned(Mind.Mind mind)
    {
        return mind.HasRole<NinjaRole>();
    }
}
