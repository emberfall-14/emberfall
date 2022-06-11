using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
[Virtual]
public class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public readonly float Range;
    public readonly EntityUid? Entity;
    public readonly List<DockingInterfaceState> Docks;

    public RadarConsoleBoundInterfaceState(
        float range,
        EntityUid? entity,
        List<DockingInterfaceState> docks)
    {
        Range = range;
        Entity = entity;
        Docks = docks;
    }
}

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class DockingInterfaceState
{
    public EntityCoordinates Coordinates;
    public Angle Angle;
    public EntityUid Entity;
    public bool Connected;
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}
