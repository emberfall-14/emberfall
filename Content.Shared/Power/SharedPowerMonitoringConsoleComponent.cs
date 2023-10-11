using Content.Shared.Pinpointer;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

/// <summary>
///     Data from by the server to the client for the power monitoring console UI
/// </summary>
[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public double TotalSources;
    public double TotalLoads;
    public PowerMonitoringConsoleEntry[] AllEntries;
    public PowerMonitoringConsoleEntry[] FocusSources;
    public PowerMonitoringConsoleEntry[] FocusLoads;
    public Dictionary<Vector2i, NavMapChunkPowerCables> PowerCableChunks;
    public Dictionary<Vector2i, NavMapChunkPowerCables>? FocusCableChunks;
    public PowerMonitoringFlags Flags;

    public PowerMonitoringConsoleBoundInterfaceState
        (double totalSources,
        double totalLoads,
        PowerMonitoringConsoleEntry[] allEntries,
        PowerMonitoringConsoleEntry[] focusSources,
        PowerMonitoringConsoleEntry[] focusLoads,
        Dictionary<Vector2i, NavMapChunkPowerCables> powerCableChunks,
        Dictionary<Vector2i, NavMapChunkPowerCables>? focusCableChunks,
        PowerMonitoringFlags flags)
    {
        TotalSources = totalSources;
        TotalLoads = totalLoads;
        AllEntries = allEntries;
        FocusSources = focusSources;
        FocusLoads = focusLoads;
        PowerCableChunks = powerCableChunks;
        FocusCableChunks = focusCableChunks;
        Flags = flags;
    }
}

/// <summary>
///     Contains all the data needed to represent a single device on the power monitoring UI
/// </summary>
[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleEntry
{
    public NetEntity NetEntity;
    public NetCoordinates? Coordinates;
    public PowerMonitoringConsoleGroup Group;
    public string NameLocalized;
    public double PowerValue;

    public PowerMonitoringConsoleEntry
        (NetEntity netEntity,
        NetCoordinates? coordinates,
        PowerMonitoringConsoleGroup group,
        string name,
        double powerValue)
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        Group = group;
        NameLocalized = name;
        PowerValue = powerValue;
    }
}

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestPowerMonitoringUpdateMessage : BoundUserInterfaceMessage
{
    public NetEntity? FocusDevice;

    public RequestPowerMonitoringUpdateMessage(NetEntity? focusDevice)
    {
        FocusDevice = focusDevice;
    }
}

/// <summary>
///     Determines how entities are grouped and color coded on the power monitor
/// </summary>
public enum PowerMonitoringConsoleGroup
{
    Consumer,
    APC,
    Substation,
    SMES,
    Generator
}

[Flags]
public enum PowerMonitoringFlags
{
    None = 0,
    RoguePowerConsumer = 1,
    PowerNetAbnormalities = 2,
}

/// <summary>
///     UI key associated with the power monitoring console
/// </summary>
[Serializable, NetSerializable]
public enum PowerMonitoringConsoleUiKey
{
    Key
}
