using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.GatewayStation;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Content.Client.Pinpointer.UI;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.GatewayStation;

[GenerateTypedNameReferences]
public sealed partial class StationGatewayWindow : FancyWindow
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SpriteSystem _spriteSystem;

    public event Action<NetEntity?>? SendGatewayLinkChangeAction;

    private NetEntity? _trackedEntity;
    private Texture? _ringTexture;

    public StationGatewayWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _spriteSystem = _entManager.System<SpriteSystem>();

        NavMap.TrackedEntitySelectedAction += SetTrackedEntityFromNavMap;
    }

    public void Set(StationGatewayBoundUserInterface userInterface, string stationName, EntityUid? mapUid)
    {
        _ringTexture = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/ring.png")));

        if (_entManager.TryGetComponent<TransformComponent>(mapUid, out var xform))
            NavMap.MapUid = xform.GridUid;
        else
            NavMap.Visible = false;

        StationName.AddStyleClass("LabelBig");
        StationName.Text = stationName;
        NavMap.ForceNavMapUpdate();

        SendGatewayLinkChangeAction += userInterface.SendGatewayLinkChangeMessage;
    }

    public void ShowGateways(StationGatewayState state, EntityUid monitor, EntityCoordinates? monitorCoords)
    {
        ClearOutDatedData();

        var gateways = state.Gateways;

        //No gateways
        if (gateways.Count == 0)
        {
            NoGatewayLabel.Visible = true;
            return;
        }

        NoGatewayLabel.Visible = false;


        // Show all gateways
        foreach (var gate in gateways)
        {
            var coordinates = _entManager.GetCoordinates(gate.Coordinates);

            var selected = gate.GatewayUid == state.SelectedGateway;
            var linked = gate.LinkCoordinates is not null;

            var bgColor = linked ? new Color(18, 61, 82) : new Color(30, 30, 34);
            if (selected)
                bgColor = new Color(49, 117, 7);


            // Primary container to hold the button UI elements
            var panelContainer = new PanelContainer()
            {
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                Margin = new Thickness(10),
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = bgColor,
                    BorderColor = Color.Black,
                    BorderThickness = new(2),
                },
            };

            GatewaysTable.AddChild(panelContainer);

            var mainBox = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };

            panelContainer.AddChild(mainBox);


            // Gate name
            var nameLabel = new RichTextLabel()
            {
                HorizontalExpand = true,
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 5),
            };
            nameLabel.SetMarkup($"[bold]{gate.Name}[/bold]");

            mainBox.AddChild(nameLabel);


            //Left subpart
            var leftBox = new BoxContainer()
            {
                SetWidth = 30,
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };

            mainBox.AddChild(leftBox);


            //Right subpart
            var rightBox = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };

            mainBox.AddChild(rightBox);


            // Centering button
            var centerButton = new GatewayButton()
            {
                Text = Loc.GetString("gateway-console-user-interface-locate"),
                GatewayUid = gate.GatewayUid,
                Coordinates = coordinates,
                HorizontalAlignment = HAlignment.Right,
                SetWidth = 200f,
            };

            rightBox.AddChild(centerButton);


            // Link\Unlink button
            var linkButton = new GatewayButton()
            {
                Text = linked ? Loc.GetString("gateway-console-user-interface-cut-connection") : Loc.GetString("gateway-console-user-interface-start-connection"),
                GatewayUid = gate.GatewayUid,
                Coordinates = coordinates,
                HorizontalAlignment = HAlignment.Right,
                SetWidth = 200f,
            };
            linkButton.OnButtonUp += _ =>
            {
                SendGatewayLinkChangeAction?.Invoke(gate.GatewayUid);
            };

            rightBox.AddChild(linkButton);


            //Add gateway coordinates to the NavMap
            if (coordinates != null && NavMap.Visible && _ringTexture != null)
            {
                var blip = new NavMapBlip(coordinates.Value, _ringTexture, Color.Aqua, false);
                NavMap.TrackedEntities.TryAdd(gate.GatewayUid, blip);

                NavMap.Focus = _trackedEntity;

                centerButton.OnButtonUp += _ =>
                {
                    if (_trackedEntity == gate.GatewayUid)
                        _trackedEntity = null;
                    else
                    {
                        _trackedEntity = gate.GatewayUid;
                        NavMap.CenterToCoordinates(coordinates.Value);
                    }

                    NavMap.Focus = _trackedEntity;

                    UpdateGatewaysTable();
                };
            }

            //Add gateways links lines
            if (gate.Coordinates is not null && gate.LinkCoordinates is not null)
            {
                var coordsOne = _entManager.GetCoordinates(gate.Coordinates);
                var coordTwo = _entManager.GetCoordinates(gate.LinkCoordinates);
                if (coordsOne is not null && coordTwo is not null)
                {
                    NavMap.LinkLines.Add(new GatewayLinkLine(coordsOne.Value, coordTwo.Value));
                }
            }
        }
    }

    private void SetTrackedEntityFromNavMap(NetEntity? netEntity)
    {
        NavMap.Focus = netEntity;
        UpdateGatewaysTable();
    }

    private void ClearOutDatedData()
    {
        GatewaysTable.RemoveAllChildren();
        NavMap.TrackedCoordinates.Clear();
        NavMap.TrackedEntities.Clear();
        NavMap.LinkLines.Clear();
    }

    private void UpdateGatewaysTable()
    {
        foreach (var gate in GatewaysTable.Children)
        {
            if (gate is not GatewayButton)
                continue;

            var castGate = (GatewayButton)gate;

            if (castGate?.Coordinates == null)
                continue;

            if (NavMap.TrackedEntities.TryGetValue(castGate.GatewayUid, out var data))
            {
                data = new NavMapBlip(
                    data.Coordinates,
                    data.Texture,
                    Color.Aqua,
                    false);

                NavMap.TrackedEntities[castGate.GatewayUid] = data;
            }
        }
    }
}
public sealed class GatewayButton : Button
{
    public int IndexInTable;
    public NetEntity GatewayUid;
    public EntityCoordinates? Coordinates;
}
