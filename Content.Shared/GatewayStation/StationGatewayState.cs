using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GatewayStation;

[Serializable, NetSerializable]
public enum StationGatewayUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class StationGatewayState : BoundUserInterfaceState
{
    public NetEntity? SelectedGateway;
    public List<StationGatewayStatus> Gateways;
    public StationGatewayState(List<StationGatewayStatus> gateways, NetEntity? selected = null)
    {
        Gateways = gateways;
        SelectedGateway = selected;
    }
}

[Serializable, NetSerializable]
public sealed class StationGatewayStatus
{
    public StationGatewayStatus(NetEntity gatewayUid, NetCoordinates coordinates, NetCoordinates? link, string name)
    {
        GatewayUid = gatewayUid;
        Coordinates = coordinates;
        LinkCoordinates = link;
        Name = name;
    }

    public NetEntity GatewayUid;
    public NetCoordinates? Coordinates;
    public NetCoordinates? LinkCoordinates;
    public string Name;
}

[Serializable, NetSerializable]
public sealed class StationGatewayGateClickMessage : BoundUserInterfaceMessage
{
    public NetEntity? Gateway;

    /// <summary>
    /// TODO
    /// </summary>
    public StationGatewayGateClickMessage(NetEntity? gateway)
    {
        Gateway = gateway;
    }
}
