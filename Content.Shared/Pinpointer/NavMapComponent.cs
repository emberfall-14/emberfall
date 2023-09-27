using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Used to store grid poly data to be used for UIs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavMapComponent : Component
{
    /*
     * Don't need DataFields as this can be reconstructed
     */

    [ViewVariables]
    public readonly Dictionary<Vector2i, NavMapChunk> Chunks = new();

    [ViewVariables] public readonly List<SharedNavMapSystem.NavMapBeacon> Beacons = new();
}

public sealed class NavMapChunk
{
    public readonly Vector2i Origin;

    /// <summary>
    /// Bitmask for tiles, 1 for occupied and 0 for empty.
    /// </summary>
    public int TileData;

    /// <summary>
    /// Bitmask for HV cables, 1 for occupied and 0 for empty.
    /// </summary>
    public Dictionary<CableType, int> CableData = new Dictionary<CableType, int>
    {
        [CableType.HV] = 0,
        [CableType.MV] = 0,
        [CableType.LV] = 0,
    };

    public NavMapChunk(Vector2i origin)
    {
        Origin = origin;
    }
}
