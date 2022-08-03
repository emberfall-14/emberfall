using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Allows a store entry to be filtered out based on the user's job.
/// Supports both blacklists and whitelists
/// </summary>
public sealed class StoreJobCondition : ListingCondition
{
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<JobPrototype>))]
    public HashSet<string>? Whitelist;

    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<JobPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (!ent.TryGetComponent<MindComponent>(args.User, out var mind) || mind.Mind == null)
            return true;

        if (Blacklist != null)
        {
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role.GetType() == typeof(Job))
                {
                    var job = (Job) role;

                    if (Blacklist.Contains(job.Prototype.ID))
                        return false;
                }
            }
        }

        if (Whitelist != null)
        {
            var found = false;
            foreach (var role in mind.Mind.AllRoles)
            {
                if (role.GetType() == typeof(Job))
                {
                    var job = (Job) role;

                    if (Whitelist.Contains(job.Prototype.ID))
                        found = true;
                }
            }
            if (!found)
                return false;
        }

        return true;
    }
}
