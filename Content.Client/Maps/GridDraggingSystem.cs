using Content.Client.Administration.Managers;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Maps;

/// <inheritdoc />
public sealed class GridDraggingSystem : SharedGridDraggingSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public bool Enabled { get; set; }

    private EntityUid? _dragging;
    private Vector2 _localPosition;

    private void StartDragging(EntityUid grid, Vector2 localPosition)
    {
        _dragging = grid;
        _localPosition = localPosition;
    }

    private void StopDragging()
    {
        if (_dragging == null) return;

        _dragging = null;
        _localPosition = Vector2.Zero;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled || !_gameTiming.InPrediction) return;

        var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);

        if (state != BoundKeyState.Down)
        {
            StopDragging();
            return;
        }

        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mousePos = _eyeManager.ScreenToMap(mouseScreenPos);

        if (_dragging == null)
        {
            if (!_mapManager.TryFindGridAt(mousePos, out var grid))
                return;

            StartDragging(grid.GridEntityId, Transform(grid.GridEntityId).InvWorldMatrix.Transform(mousePos.Position));
        }

        if (!TryComp<TransformComponent>(_dragging, out var xform))
        {
            StopDragging();
            return;
        }

        if (xform.MapID != mousePos.MapId)
        {
            StopDragging();
            return;
        }

        var localToWorld = xform.WorldMatrix.Transform(_localPosition);

        if (localToWorld.EqualsApprox(mousePos.Position, 0.01f)) return;

        var requestedGridOrigin = mousePos.Position - xform.WorldRotation.RotateVec(_localPosition);

        RaiseNetworkEvent(new GridDragRequestPosition()
        {
            Grid = _dragging.Value,
            WorldPosition = requestedGridOrigin,
        });
    }
}
