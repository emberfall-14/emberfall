using Content.Client.Message;
using Content.Client.Pinpointer.UI;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Pinpointer;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Client.Atmos.Consoles;

[GenerateTypedNameReferences]
public sealed partial class AtmosAlertsComputerWindow : FancyWindow
{
    private readonly IEntityManager _entManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly SharedNavMapSystem _navMapSystem;

    private EntityUid? _owner;
    private NetEntity? _trackedEntity;

    private AtmosAlertsComputerEntry[]? _airAlarms = null;
    private AtmosAlertsComputerEntry[]? _fireAlarms = null;
    private IEnumerable<AtmosAlertsComputerEntry>? _allAlarms = null;

    private IEnumerable<AtmosAlertsComputerEntry>? _activeAlarms = null;
    private Dictionary<NetEntity, float> _deviceSilencingProgress = new();

    public event Action<NetEntity?>? SendFocusChangeMessageAction;
    public event Action<NetEntity, bool>? SendDeviceSilencedMessageAction;

    private bool _autoScrollActive = false;
    private bool _autoScrollAwaitsUpdate = false;

    private const float SilencingDuration = 2.5f;

    public AtmosAlertsComputerWindow(AtmosAlertsComputerBoundUserInterface userInterface, EntityUid? owner)
    {
        RobustXamlLoader.Load(this);
        _entManager = IoCManager.Resolve<IEntityManager>();
        _spriteSystem = _entManager.System<SpriteSystem>();
        _navMapSystem = _entManager.System<SharedNavMapSystem>();

        // Pass the owner to nav map
        _owner = owner;
        NavMap.Owner = _owner;

        // Set nav map colors
        NavMap.WallColor = new Color(64, 64, 64);
        NavMap.TileColor = Color.DimGray * NavMap.WallColor;

        // Set nav map grid uid
        var stationName = Loc.GetString("atmos-alerts-window-unknown-location");

        if (_entManager.TryGetComponent<TransformComponent>(owner, out var xform))
        {
            NavMap.MapUid = xform.GridUid;

            // Assign station name      
            if (_entManager.TryGetComponent<MetaDataComponent>(xform.GridUid, out var stationMetaData))
                stationName = stationMetaData.EntityName;

            var msg = new FormattedMessage();
            msg.TryAddMarkup(Loc.GetString("atmos-alerts-window-station-name", ("stationName", stationName)), out _);

            StationName.SetMessage(msg);
        }

        else
        {
            StationName.SetMessage(stationName);
            NavMap.Visible = false;
        }

        // Set trackable entity selected action
        NavMap.TrackedEntitySelectedAction += SetTrackedEntityFromNavMap;

        // Update nav map
        NavMap.ForceNavMapUpdate();

        // Set tab container headers
        MasterTabContainer.SetTabTitle(0, Loc.GetString("atmos-alerts-window-tab-no-alerts"));
        MasterTabContainer.SetTabTitle(1, Loc.GetString("atmos-alerts-window-tab-air-alarms"));
        MasterTabContainer.SetTabTitle(2, Loc.GetString("atmos-alerts-window-tab-fire-alarms"));

        // Set UI toggles
        ShowInactiveAlarms.OnToggled += _ => OnShowAlarmsToggled(ShowInactiveAlarms, AtmosAlarmType.Invalid);
        ShowNormalAlarms.OnToggled += _ => OnShowAlarmsToggled(ShowNormalAlarms, AtmosAlarmType.Normal);
        ShowWarningAlarms.OnToggled += _ => OnShowAlarmsToggled(ShowWarningAlarms, AtmosAlarmType.Warning);
        ShowDangerAlarms.OnToggled += _ => OnShowAlarmsToggled(ShowDangerAlarms, AtmosAlarmType.Danger);

        // Set atmos monitoring message action
        SendFocusChangeMessageAction += userInterface.SendFocusChangeMessage;
        SendDeviceSilencedMessageAction += userInterface.SendDeviceSilencedMessage;
    }

    #region Toggle handling

    private void OnShowAlarmsToggled(CheckBox toggle, AtmosAlarmType toggledAlarmState)
    {
        if (_owner == null)
            return;

        if (!_entManager.TryGetComponent<AtmosAlertsComputerComponent>(_owner.Value, out var console))
            return;

        foreach (var device in console.AtmosDevices)
        {
            var alarmState = GetAlarmState(device.NetEntity);

            if (toggledAlarmState != alarmState)
                continue;

            if (toggle.Pressed)
                AddTrackedEntityToNavMap(device, alarmState);

            else
                NavMap.TrackedEntities.Remove(device.NetEntity);
        }
    }

    private void OnSilenceAlertsToggled(NetEntity netEntity, bool toggleState)
    {
        if (!_entManager.TryGetComponent<AtmosAlertsComputerComponent>(_owner, out var console))
            return;

        if (toggleState)
            _deviceSilencingProgress[netEntity] = SilencingDuration;

        else
            _deviceSilencingProgress.Remove(netEntity);

        foreach (AtmosAlarmEntryContainer entryContainer in AlertsTable.Children)
        {
            if (entryContainer.NetEntity == netEntity)
                entryContainer.SilenceAlarmProgressBar.Visible = toggleState;
        }

        SendDeviceSilencedMessageAction?.Invoke(netEntity, toggleState);
    }

    #endregion

    public void UpdateUI(EntityCoordinates? consoleCoords, AtmosAlertsComputerEntry[] airAlarms, AtmosAlertsComputerEntry[] fireAlarms, AtmosAlertsFocusDeviceData? focusData)
    {
        if (_owner == null)
            return;

        if (!_entManager.TryGetComponent<AtmosAlertsComputerComponent>(_owner.Value, out var console))
            return;

        if (_trackedEntity != focusData?.NetEntity)
        {
            SendFocusChangeMessageAction?.Invoke(_trackedEntity);
            focusData = null;
        }

        // Retain alarm data for use inbetween updates
        _airAlarms = airAlarms;
        _fireAlarms = fireAlarms;
        _allAlarms = airAlarms.Concat(fireAlarms);

        var silenced = console.SilencedDevices;

        _activeAlarms = _allAlarms.Where(x => x.AlarmState > AtmosAlarmType.Normal &&
            (!silenced.Contains(x.NetEntity) || _deviceSilencingProgress.ContainsKey(x.NetEntity)));

        // Reset nav map data
        NavMap.TrackedCoordinates.Clear();
        NavMap.TrackedEntities.Clear();

        // Add tracked entities to the nav map
        foreach (var device in console.AtmosDevices)
        {
            if (!device.NetEntity.Valid)
                continue;

            if (!NavMap.Visible)
                continue;

            var alarmState = GetAlarmState(device.NetEntity);

            if (_trackedEntity != device.NetEntity)
            {
                // Skip air alarms if the appropriate overlay is off
                if (!ShowInactiveAlarms.Pressed && alarmState == AtmosAlarmType.Invalid)
                    continue;

                if (!ShowNormalAlarms.Pressed && alarmState == AtmosAlarmType.Normal)
                    continue;

                if (!ShowWarningAlarms.Pressed && alarmState == AtmosAlarmType.Warning)
                    continue;

                if (!ShowDangerAlarms.Pressed && alarmState == AtmosAlarmType.Danger)
                    continue;
            }

            AddTrackedEntityToNavMap(device, alarmState);
        }

        // Show the monitor location
        var consoleUid = _entManager.GetNetEntity(_owner);

        if (consoleCoords != null && consoleUid != null)
        {
            var texture = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png")));
            var blip = new NavMapBlip(consoleCoords.Value, texture, Color.Cyan, true, false);
            NavMap.TrackedEntities[consoleUid.Value] = blip;
        }

        // Update the nav map
        NavMap.ForceNavMapUpdate();

        // Clear excess children from the tables
        var activeAlarmCount = _activeAlarms.Count();

        while (AlertsTable.ChildCount > activeAlarmCount)
            AlertsTable.RemoveChild(AlertsTable.GetChild(AlertsTable.ChildCount - 1));

        while (AirAlarmsTable.ChildCount > airAlarms.Length)
            AirAlarmsTable.RemoveChild(AirAlarmsTable.GetChild(AirAlarmsTable.ChildCount - 1));

        while (FireAlarmsTable.ChildCount > fireAlarms.Length)
            FireAlarmsTable.RemoveChild(FireAlarmsTable.GetChild(FireAlarmsTable.ChildCount - 1));

        // Update all entries in each table
        for (int index = 0; index < _activeAlarms.Count(); index++)
        {
            var entry = _activeAlarms.ElementAt(index);
            UpdateUIEntry(entry, index, AlertsTable, console, focusData);
        }

        for (int index = 0; index < airAlarms.Count(); index++)
        {
            var entry = airAlarms.ElementAt(index);
            UpdateUIEntry(entry, index, AirAlarmsTable, console, focusData);
        }

        for (int index = 0; index < fireAlarms.Count(); index++)
        {
            var entry = fireAlarms.ElementAt(index);
            UpdateUIEntry(entry, index, FireAlarmsTable, console, focusData);
        }

        // If no alerts are active, display a message
        if (MasterTabContainer.CurrentTab == 0 && activeAlarmCount == 0)
        {
            var label = new RichTextLabel()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
            };

            label.SetMarkup(Loc.GetString("atmos-alerts-window-no-active-alerts", ("color", StyleNano.GoodGreenFore.ToHexNoAlpha())));

            AlertsTable.AddChild(label);
        }

        // Update the alerts tab with the number of active alerts
        if (activeAlarmCount == 0)
            MasterTabContainer.SetTabTitle(0, Loc.GetString("atmos-alerts-window-tab-no-alerts"));

        else
            MasterTabContainer.SetTabTitle(0, Loc.GetString("atmos-alerts-window-tab-alerts", ("value", activeAlarmCount)));

        // Update sensor regions
        NavMap.RegionOverlays.Clear();
        var prioritizedRegionOverlays = new Dictionary<NavMapRegionOverlay, int>();

        if (_owner != null &&
            _entManager.TryGetComponent<TransformComponent>(_owner, out var xform) &&
            _entManager.TryGetComponent<NavMapComponent>(xform.GridUid, out var navMap))
        {
            var regionOverlays = _navMapSystem.GetNavMapRegionOverlays(_owner.Value, navMap, AtmosAlertsComputerUiKey.Key);

            foreach (var (regionOwner, regionOverlay) in regionOverlays)
            {
                var alarmState = GetAlarmState(regionOwner);

                if (!TryGetSensorRegionColor(regionOwner, alarmState, out var regionColor))
                    continue;

                regionOverlay.Color = regionColor.Value;

                var priority = (_trackedEntity == regionOwner) ? 999 : (int)alarmState;
                prioritizedRegionOverlays.Add(regionOverlay, priority);
            }

            // Sort overlays according to their priority
            var sortedOverlays = prioritizedRegionOverlays.OrderBy(x => x.Value).Select(x => x.Key).ToList();
            NavMap.RegionOverlays = sortedOverlays;
        }

        // Auto-scroll re-enable
        if (_autoScrollAwaitsUpdate)
        {
            _autoScrollActive = true;
            _autoScrollAwaitsUpdate = false;
        }
    }

    private void AddTrackedEntityToNavMap(AtmosAlertsDeviceNavMapData metaData, AtmosAlarmType alarmState)
    {
        var data = GetBlipTexture(alarmState);

        if (data == null)
            return;

        var texture = data.Value.Item1;
        var color = data.Value.Item2;
        var coords = _entManager.GetCoordinates(metaData.NetCoordinates);

        if (_trackedEntity != null && _trackedEntity != metaData.NetEntity)
            color *= Color.DimGray;

        var selectable = true;
        var blip = new NavMapBlip(coords, _spriteSystem.Frame0(texture), color, _trackedEntity == metaData.NetEntity, selectable);

        NavMap.TrackedEntities[metaData.NetEntity] = blip;
    }

    private bool TryGetSensorRegionColor(NetEntity regionOwner, AtmosAlarmType alarmState, [NotNullWhen(true)] out Color? color)
    {
        color = null;

        var blip = GetBlipTexture(alarmState);

        if (blip == null)
            return false;

        // Color the region based on alarm state and entity tracking
        color = blip.Value.Item2 * Color.DimGray;

        if (_trackedEntity != null && _trackedEntity != regionOwner)
            color *= Color.DimGray;

        return true;
    }

    private void UpdateUIEntry(AtmosAlertsComputerEntry entry, int index, Control table, AtmosAlertsComputerComponent console, AtmosAlertsFocusDeviceData? focusData = null)
    {
        // Make new UI entry if required
        if (index >= table.ChildCount)
        {
            var newEntryContainer = new AtmosAlarmEntryContainer(entry.NetEntity, _entManager.GetCoordinates(entry.Coordinates));

            // On click
            newEntryContainer.FocusButton.OnButtonUp += args =>
            {
                if (_trackedEntity == newEntryContainer.NetEntity)
                {
                    _trackedEntity = null;
                }

                else
                {
                    _trackedEntity = newEntryContainer.NetEntity;

                    if (newEntryContainer.Coordinates != null)
                        NavMap.CenterToCoordinates(newEntryContainer.Coordinates.Value);
                }

                // Send message to console that the focus has changed
                SendFocusChangeMessageAction?.Invoke(_trackedEntity);

                // Update affected UI elements across all tables
                UpdateConsoleTable(console, AlertsTable, _trackedEntity);
                UpdateConsoleTable(console, AirAlarmsTable, _trackedEntity);
                UpdateConsoleTable(console, FireAlarmsTable, _trackedEntity);
            };

            // On toggling the silence check box
            newEntryContainer.SilenceCheckBox.OnToggled += _ => OnSilenceAlertsToggled(newEntryContainer.NetEntity, newEntryContainer.SilenceCheckBox.Pressed);

            // Add the entry to the current table
            table.AddChild(newEntryContainer);
        }

        // Update values and UI elements
        var tableChild = table.GetChild(index);

        if (tableChild is not AtmosAlarmEntryContainer)
        {
            table.RemoveChild(tableChild);
            UpdateUIEntry(entry, index, table, console, focusData);

            return;
        }

        var entryContainer = (AtmosAlarmEntryContainer)tableChild;

        entryContainer.UpdateEntry(entry, entry.NetEntity == _trackedEntity, focusData);

        if (_trackedEntity != entry.NetEntity)
        {
            var silenced = console.SilencedDevices;
            entryContainer.SilenceCheckBox.Pressed = (silenced.Contains(entry.NetEntity) || _deviceSilencingProgress.ContainsKey(entry.NetEntity));
        }

        entryContainer.SilenceAlarmProgressBar.Visible = (table == AlertsTable && _deviceSilencingProgress.ContainsKey(entry.NetEntity));
    }

    private void UpdateConsoleTable(AtmosAlertsComputerComponent console, Control table, NetEntity? currTrackedEntity)
    {
        foreach (var tableChild in table.Children)
        {
            if (tableChild is not AtmosAlarmEntryContainer)
                continue;

            var entryContainer = (AtmosAlarmEntryContainer)tableChild;

            if (entryContainer.NetEntity != currTrackedEntity)
                entryContainer.RemoveAsFocus();

            else if (entryContainer.NetEntity == currTrackedEntity)
                entryContainer.SetAsFocus();
        }
    }

    private void SetTrackedEntityFromNavMap(NetEntity? netEntity)
    {
        if (netEntity == null)
            return;

        if (!_entManager.TryGetComponent<AtmosAlertsComputerComponent>(_owner, out var console))
            return;

        _trackedEntity = netEntity;

        if (netEntity != null)
        {
            // Tab switching
            if (MasterTabContainer.CurrentTab != 0 || _activeAlarms?.Any(x => x.NetEntity == netEntity) == false)
            {
                var device = console.AtmosDevices.FirstOrNull(x => x.NetEntity == netEntity);

                switch (device?.Group)
                {
                    case AtmosAlertsComputerGroup.AirAlarm:
                        MasterTabContainer.CurrentTab = 1; break;
                    case AtmosAlertsComputerGroup.FireAlarm:
                        MasterTabContainer.CurrentTab = 2; break;
                }
            }

            // Get the scroll position of the selected entity on the selected button the UI
            ActivateAutoScrollToFocus();
        }

        // Send message to console that the focus has changed
        SendFocusChangeMessageAction?.Invoke(_trackedEntity);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        AutoScrollToFocus();

        // Device silencing update
        foreach ((var device, var remainingTime) in _deviceSilencingProgress)
        {
            var t = remainingTime - args.DeltaSeconds;

            if (t <= 0)
            {
                _deviceSilencingProgress.Remove(device);

                if (device == _trackedEntity)
                    _trackedEntity = null;
            }

            else
                _deviceSilencingProgress[device] = t;
        }
    }

    private void ActivateAutoScrollToFocus()
    {
        _autoScrollActive = false;
        _autoScrollAwaitsUpdate = true;
    }

    private void AutoScrollToFocus()
    {
        if (!_autoScrollActive)
            return;

        var scroll = MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) as ScrollContainer;
        if (scroll == null)
            return;

        if (!TryGetVerticalScrollbar(scroll, out var vScrollbar))
            return;

        if (!TryGetNextScrollPosition(out float? nextScrollPosition))
            return;

        vScrollbar.ValueTarget = nextScrollPosition.Value;

        if (MathHelper.CloseToPercent(vScrollbar.Value, vScrollbar.ValueTarget))
            _autoScrollActive = false;
    }

    private bool TryGetVerticalScrollbar(ScrollContainer scroll, [NotNullWhen(true)] out VScrollBar? vScrollBar)
    {
        vScrollBar = null;

        foreach (var child in scroll.Children)
        {
            if (child is not VScrollBar)
                continue;

            var castChild = child as VScrollBar;

            if (castChild != null)
            {
                vScrollBar = castChild;
                return true;
            }
        }

        return false;
    }

    private bool TryGetNextScrollPosition([NotNullWhen(true)] out float? nextScrollPosition)
    {
        nextScrollPosition = null;

        var scroll = MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) as ScrollContainer;
        if (scroll == null)
            return false;

        var container = scroll.Children.ElementAt(0) as BoxContainer;
        if (container == null || container.Children.Count() == 0)
            return false;

        // Exit if the heights of the children haven't been initialized yet
        if (!container.Children.Any(x => x.Height > 0))
            return false;

        nextScrollPosition = 0;

        foreach (var control in container.Children)
        {
            if (control == null || control is not AtmosAlarmEntryContainer)
                continue;

            if (((AtmosAlarmEntryContainer)control).NetEntity == _trackedEntity)
                return true;

            nextScrollPosition += control.Height;
        }

        // Failed to find control
        nextScrollPosition = null;

        return false;
    }

    private AtmosAlarmType GetAlarmState(NetEntity netEntity)
    {
        var alarmState = _allAlarms?.FirstOrNull(x => x.NetEntity == netEntity)?.AlarmState;

        if (alarmState == null)
            return AtmosAlarmType.Invalid;

        return alarmState.Value;
    }

    private (SpriteSpecifier.Texture, Color)? GetBlipTexture(AtmosAlarmType alarmState)
    {
        (SpriteSpecifier.Texture, Color)? output = null;

        switch (alarmState)
        {
            case AtmosAlarmType.Invalid:
                output = (new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png")), StyleNano.DisabledFore); break;
            case AtmosAlarmType.Normal:
                output = (new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png")), Color.LimeGreen); break;
            case AtmosAlarmType.Warning:
                output = (new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_triangle.png")), new Color(255, 182, 72)); break;
            case AtmosAlarmType.Danger:
                output = (new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_square.png")), new Color(255, 67, 67)); break;
        }

        return output;
    }
}
