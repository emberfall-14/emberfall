using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Handles generic grid-drawing data, with zoom and dragging.
/// </summary>
public abstract class MapGridControl : Control
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    /* Dragging */
    protected virtual bool Draggable { get; } = false;

    /// <summary>
    /// Control offset from whatever is being tracked.
    /// </summary>
    public Vector2 Offset;
    private bool _draggin;
    protected Vector2 StartDragPosition;
    protected bool Recentering;

    protected const float ScrollSensitivity = 8f;

    /// <summary>
    /// UI pixel radius.
    /// </summary>
    public const int UIDisplayRadius = 320;
    protected const int MinimapMargin = 4;

    protected float WorldMinRange;
    protected float WorldMaxRange;
    public float WorldRange;
    public Vector2 WorldRangeVector => new Vector2(WorldRange, WorldRange);

    /// <summary>
    /// We'll lerp between the radarrange and actual range
    /// </summary>
    protected float ActualRadarRange;

    protected float CornerRadarRange => MathF.Sqrt(ActualRadarRange * ActualRadarRange + ActualRadarRange * ActualRadarRange);

    /// <summary>
    /// Controls the maximum distance that will display.
    /// </summary>
    public float MaxRadarRange { get; private set; } = 256f * 10f;

    public Vector2 MaxRadarRangeVector => new Vector2(MaxRadarRange, MaxRadarRange);

    protected Vector2 MidPointVector => new Vector2(MidPoint, MidPoint);

    protected int MidPoint => SizeFull / 2;
    protected int SizeFull => (int) ((UIDisplayRadius + MinimapMargin) * 2 * UIScale);
    protected int ScaledMinimapRadius => (int) (UIDisplayRadius * UIScale);
    protected float MinimapScale => WorldRange != 0 ? ScaledMinimapRadius / WorldRange : 0f;

    public event Action<float>? WorldRangeChanged;

    public MapGridControl(float minRange, float maxRange, float range)
    {
        IoCManager.InjectDependencies(this);
        SetSize = new Vector2(SizeFull, SizeFull);
        RectClipContent = true;
        MouseFilter = MouseFilterMode.Stop;
        ActualRadarRange = WorldRange;
        WorldMinRange = minRange;
        WorldMaxRange = maxRange;
        WorldRange = range;
        ActualRadarRange = range;
    }

    public void ForceRecenter()
    {
        Recentering = true;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!Draggable)
            return;

        if (args.Function == EngineKeyFunctions.Use)
        {
            StartDragPosition = args.PointerLocation.Position;
            _draggin = true;
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (!Draggable)
            return;

        if (args.Function == EngineKeyFunctions.Use)
            _draggin = false;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_draggin)
            return;

        Recentering = false;
        Offset -= new Vector2(args.Relative.X, -args.Relative.Y) / MidPoint * WorldRange;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        AddRadarRange(-args.Delta.Y * 1f / ScrollSensitivity * ActualRadarRange);
    }

    public void AddRadarRange(float value)
    {
        ActualRadarRange = Math.Clamp(ActualRadarRange + value, WorldMinRange, WorldMaxRange);
    }

    protected Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPointVector;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (!ActualRadarRange.Equals(WorldRange))
        {
            var diff = ActualRadarRange - WorldRange;
            const float lerpRate = 10f;

            WorldRange += (float) Math.Clamp(diff, -lerpRate * MathF.Abs(diff) * Timing.FrameTime.TotalSeconds, lerpRate * MathF.Abs(diff) * Timing.FrameTime.TotalSeconds);
            WorldRangeChanged?.Invoke(WorldRange);
        }
    }
}
