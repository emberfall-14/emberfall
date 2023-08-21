using Content.Shared.Research.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Research.Components;

/// <summary>
/// Component for stealing technologies from a R&D server, when gloves are enabled.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedResearchStealerSystem))]
public sealed class ResearchStealerComponent : Component
{
    /// <summary>
    /// Time taken to steal research from a server
    /// </summary>
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(20);
}
