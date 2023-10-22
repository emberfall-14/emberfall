using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.StatusIcon;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Medical.CrewMonitoring
{
    [GenerateTypedNameReferences]
    public sealed partial class CrewMonitoringWindow : FancyWindow
    {
        private List<Control> _rowsContent = new();
        private List<(DirectionIcon Icon, Vector2 Position)> _directionIcons = new();
        private readonly IEntityManager _entManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly IEyeManager _eye;
        private readonly SpriteSystem _spriteSystem;
        private EntityUid? _stationUid;
        private EntityUid? _trackedEntity;


        public static int IconSize = 16; // XAML has a `VSeparationOverride` of 20 for each row.

        public CrewMonitoringWindow(EntityUid? mapUid)
        {
            RobustXamlLoader.Load(this);
            _eye = IoCManager.Resolve<IEyeManager>();
            _entManager = IoCManager.Resolve<IEntityManager>();
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            _spriteSystem = _entManager.System<SpriteSystem>();
            _stationUid = mapUid;

            if (_entManager.TryGetComponent<TransformComponent>(mapUid, out var xform))
            {
                NavMap.MapUid = xform.GridUid;
            }
            else
            {
                NavMap.Visible = false;
                SetSize = new Vector2(775, 400);
                MinSize = SetSize;
            }
        }

        public void ShowSensors(List<SuitSensorStatus> stSensors, EntityCoordinates? monitorCoords, bool snap, float precision)
        {
            ClearAllSensors();

            var monitorCoordsInStationSpace = _stationUid != null ? monitorCoords?.WithEntityId(_stationUid.Value, _entManager).Position : null;

            // TODO scroll container
            // TODO filter by name & occupation
            // TODO make each row a xaml-control. Get rid of some of this c# control creation.
            if (stSensors.Count == 0)
            {
                NoServerLabel.Visible = true;
                return;
            }

            NoServerLabel.Visible = false;

            // add a row for each sensor
            foreach (var sensor in stSensors.OrderBy(a => a.Name))
            {
                var sensorEntity = _entManager.GetEntity(sensor.SuitSensorUid);
                var coordinates = _entManager.GetCoordinates(sensor.Coordinates);

                // add button with username
                var sensorButton = new CrewMonitoringButton()
                {
                    SuitSensorUid = sensorEntity,
                    Coordinates = coordinates,
                    Margin = new Thickness(5f, 5f),
                    Disabled = (coordinates == null),
                    HorizontalExpand = true,
                };

                if (sensorEntity == _trackedEntity)
                    sensorButton.AddStyleClass(StyleNano.StyleClassButtonColorGreen);

                SensorsTable.AddChild(sensorButton);
                _rowsContent.Add(sensorButton);

                var mainContainer = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                };

                sensorButton.AddChild(mainContainer);

                var specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "alive");

                if (!sensor.IsAlive)
                    specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "dead");

                else if (sensor.TotalDamage != null)
                {
                    var i = MathF.Round(4f * (sensor.TotalDamage.Value / 100f));

                    switch (i)
                    {
                        case 0: specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "health0"); break;
                        case 1: specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "health1"); break;
                        case 2: specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "health2"); break;
                        case 3: specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "health3"); break;
                        case 4: specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "health4"); break;
                        default: specifier = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), "critical"); break;
                    }
                }

                var statusIcon = new AnimatedTextureRect();
                statusIcon.SetFromSpriteSpecifier(specifier);
                statusIcon.HorizontalAlignment = HAlignment.Center;
                statusIcon.VerticalAlignment = VAlignment.Center;
                statusIcon.Margin = new Thickness(0, 1, 5, 0);
                statusIcon.DisplayRect.TextureScale = new Vector2(2f, 2f);

                mainContainer.AddChild(statusIcon);

                var nameLabel = new Label()
                {
                    Text = sensor.Name,
                    SetWidth = 180,
                };

                mainContainer.AddChild(nameLabel);

                var jobContainer = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Horizontal,
                    SetWidth = 180,
                };

                if (_prototypeManager.TryIndex<StatusIconPrototype>(sensor.JobIcon, out var proto))
                {
                    var jobIcon = new TextureRect()
                    {
                        TextureScale = new Vector2(2f, 2f),
                        Stretch = TextureRect.StretchMode.KeepCentered,
                        Texture = _spriteSystem.Frame0(proto.Icon),
                        Margin = new Thickness(0, 0, 5, 0),
                    };

                    jobContainer.AddChild(jobIcon);
                }

                var jobLabel = new Label()
                {
                    Text = sensor.Job,
                };

                jobContainer.AddChild(jobLabel);

                mainContainer.AddChild(jobContainer);

                

                

                // add users positions
                // format: (x, y)
                var box = GetPositionBox(sensor, monitorCoordsInStationSpace ?? Vector2.Zero, snap, precision);

                //SensorsTable.AddChild(box);
                //_rowsContent.Add(box);

                if (coordinates != null && NavMap.Visible)
                {
                    NavMap.TrackedCoordinates.TryAdd(coordinates.Value,
                        (true, sensorEntity == _trackedEntity ? StyleNano.PointGreen : Color.Red));

                    sensorButton.OnButtonUp += args =>
                    {
                        if (_trackedEntity == sensorEntity)
                        {
                            _trackedEntity = null;

                            NavMap.TrackedCoordinates[coordinates.Value] = (true, Color.Red);
                            sensorButton.RemoveStyleClass(StyleNano.StyleClassButtonColorGreen);

                            return;
                        }

                        NavMap.TrackedCoordinates[coordinates.Value] = (true, Color.LimeGreen);
                        NavMap.CenterToCoordinates(coordinates.Value);

                        sensorButton.AddStyleClass(StyleNano.StyleClassButtonColorGreen);
                        _trackedEntity = sensorEntity;
                    };
                }
            }

            // Show monitor point
            if (monitorCoords != null)
                NavMap.TrackedCoordinates.Add(monitorCoords.Value, (true, StyleNano.PointMagenta));
        }

        private BoxContainer GetPositionBox(SuitSensorStatus sensor, Vector2 monitorCoordsInStationSpace, bool snap, float precision)
        {
            EntityCoordinates? coordinates = _entManager.GetCoordinates(sensor.Coordinates);
            var box = new BoxContainer() { Orientation = LayoutOrientation.Horizontal };

            if (coordinates == null || _stationUid == null)
            {
                box.AddChild(new Label() { Text = Loc.GetString("crew-monitoring-user-interface-no-info") });
            }
            else
            {
                var local = coordinates.Value.WithEntityId(_stationUid.Value, _entManager).Position;

                var displayPos = local.Floored();
                Label label = new Label() { Text = displayPos.ToString() };

                box.AddChild(label);
            }

            return box;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {

        }

        private void ClearAllSensors()
        {
            foreach (var child in _rowsContent)
            {
                SensorsTable.RemoveChild(child);
            }

            _rowsContent.Clear();
            NavMap.TrackedCoordinates.Clear();
        }
    }

    public sealed class CrewMonitoringButton : Button
    {
        public int IndexInTable;
        public EntityUid? SuitSensorUid;
        public EntityCoordinates? Coordinates;
    }
}
