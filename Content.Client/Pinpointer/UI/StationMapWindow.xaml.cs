using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Pinpointer.UI;

[GenerateTypedNameReferences]
public sealed partial class StationMapWindow : FancyWindow
{
    private readonly IEntityManager _entManager;

    private const float UpdateTime = 1f;
    private float _updateTimer = UpdateTime;

    private readonly Color _markerColor = Color.Cyan;
    private readonly Color _wallColor = new Color(64, 64, 64);
    private readonly Color _tileColor = new Color(32, 32, 32);
    private readonly Color _regionOverlayColor = Color.DarkGray;

    private EntityUid? _owner;

    public StationMapWindow()
    {
        RobustXamlLoader.Load(this);
        _entManager = IoCManager.Resolve<IEntityManager>();
    }

    public void Set(EntityUid? mapUid, EntityUid? trackedEntity)
    {
        NavMapScreen.MapUid = mapUid;

        if (trackedEntity != null)
        {
            _owner = trackedEntity;
            NavMapScreen.TrackedCoordinates.Add(new EntityCoordinates(trackedEntity.Value, Vector2.Zero), (true, _markerColor));
        }

        if (_entManager.TryGetComponent<MetaDataComponent>(mapUid, out var metadata))
        {
            Title = metadata.EntityName;
        }

        NavMapScreen.WallColor = _wallColor;
        NavMapScreen.TileColor = _tileColor;

        NavMapScreen.ForceNavMapUpdate();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _updateTimer += args.DeltaSeconds;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            UpdateNavMap();
        }
    }

    private void UpdateNavMap()
    {
        NavMapScreen.RegionOverlays.Clear();

        if (_owner == null)
            return;

        if (!_entManager.TryGetComponent<TransformComponent>(_owner, out var xform) ||
            !_entManager.TryGetComponent<NavMapComponent>(xform.GridUid, out var navMapRegions))
            return;

        foreach (var (regionOwner, regionData) in navMapRegions.FloodedRegions)
            NavMapScreen.RegionOverlays[regionOwner] = regionData;
    }
}
