using System.Linq;
using System.Numerics;
using System.Text;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Systems;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class DockingScreen : BoxContainer
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedShuttleSystem _shuttles;

    /// <summary>
    /// Stored by GridID then by docks
    /// </summary>
    public Dictionary<NetEntity, List<DockingPortState>> Docks = new();

    /// <summary>
    /// Store the dock buttons for the side buttons.
    /// </summary>
    private readonly Dictionary<NetEntity, Button> _ourDockButtons = new();

    public event Action<NetEntity, NetEntity>? DockRequest;
    public event Action<NetEntity>? UndockRequest;

    public DockingScreen()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _shuttles = _entManager.System<SharedShuttleSystem>();

        DockingControl.OnViewDock += OnView;
        DockingControl.DockRequest += (entity, netEntity) =>
        {
            DockRequest?.Invoke(entity, netEntity);
        };
        DockingControl.UndockRequest += entity =>
        {
            UndockRequest?.Invoke(entity);
        };
    }

    private void OnView(NetEntity obj)
    {
        if (_ourDockButtons.TryGetValue(obj, out var viewed))
        {
            viewed.Pressed = true;
        }
    }

    public void UpdateState(EntityUid? shuttle, DockingInterfaceState state)
    {
        Docks = state.Docks;
        DockingControl.DockState = state;
        DockingControl.GridEntity = shuttle;
        BuildDocks(shuttle);
    }

    private void BuildDocks(EntityUid? shuttle)
    {
        DockingControl.BuildDocks(shuttle);
        var currentDock = DockingControl.ViewedDock;
        DockedWith.DisposeAllChildren();
        DockPorts.DisposeAllChildren();
        _ourDockButtons.Clear();

        if (shuttle == null)
        {
            DockingControl.SetViewedDock(null);
            return;
        }

        var shuttleNent = _entManager.GetNetEntity(shuttle.Value);

        if (!Docks.TryGetValue(shuttleNent, out var shuttleDocks) || shuttleDocks.Count <= 0)
            return;

        var dockText = new StringBuilder();
        var buttonGroup = new ButtonGroup();
        var idx = 0;
        var selected = false;

        // Build the dock buttons for our docks.
        foreach (var dock in shuttleDocks)
        {
            idx++;
            dockText.Clear();
            dockText.Append(dock.Name);

            var button = new Button()
            {
                Text = dockText.ToString(),
                ToggleMode = true,
                Group = buttonGroup,
                Margin = new Thickness(0f, 3f),
            };

            button.OnMouseEntered += args =>
            {
                DockingControl.HighlightedDock = dock.Entity;
            };

            button.OnMouseExited += args =>
            {
                DockingControl.HighlightedDock = null;
            };

            button.Label.Margin = new Thickness(3f);

            if (currentDock == dock.Entity)
            {
                selected = true;
                button.Pressed = true;
            }

            button.OnPressed += args =>
            {
                OnDockPress(dock);
            };

            _ourDockButtons[dock.Entity] = button;
            DockPorts.AddChild(button);
        }

        // Button group needs one selected so just show the first one.
        if (!selected)
        {
            var buttonOne = shuttleDocks[0];
            OnDockPress(buttonOne);
        }

        var shuttleContainers = new Dictionary<NetEntity, DockObject>();

        foreach (var dock in shuttleDocks.OrderBy(x => x.GridDockedWith))
        {
            if (dock.GridDockedWith == null)
                continue;

            DockObject? dockContainer;

            if (!shuttleContainers.TryGetValue(dock.GridDockedWith.Value, out dockContainer))
            {
                dockContainer = new DockObject();
                shuttleContainers[dock.GridDockedWith.Value] = dockContainer;
                var dockGrid = _entManager.GetEntity(dock.GridDockedWith);
                string? iffLabel = null;

                if (_entManager.EntityExists(dockGrid))
                {
                    iffLabel = _shuttles.GetIFFLabel(dockGrid.Value);
                }

                iffLabel ??= Loc.GetString("shuttle-console-unknown");
                dockContainer.SetName(iffLabel);
                DockedWith.AddChild(dockContainer);
            }

            dockContainer.AddDock(dock, DockingControl);

            dockContainer.ViewPressed += () =>
            {
                OnDockPress(dock);
            };

            dockContainer.UndockPressed += () =>
            {
                UndockRequest?.Invoke(dock.Entity);
            };
        }
    }

    private void OnDockPress(DockingPortState state)
    {
        DockingControl.SetViewedDock(state);
    }
}
