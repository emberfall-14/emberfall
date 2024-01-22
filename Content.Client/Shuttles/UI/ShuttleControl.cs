using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Provides common functionality for radar-like displays on shuttle consoles.
/// </summary>
public abstract class ShuttleControl : MapGridControl
{
    protected static readonly Color BackingColor = new Color(0.08f, 0.08f, 0.08f);

    protected ShuttleControl(float minRange, float maxRange, float range) : base(minRange, maxRange, range)
    {
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var backing = BackingColor;
        handle.DrawRect(new UIBox2(0f, Height, Width, 0f), backing);

        // Equatorial lines
        var gridLines = Color.LightGray.WithAlpha(0.01f);

        // Each circle is this x distance of the last one.
        const float EquatorialMultiplier = 2f;

        var minDistance = MathF.Pow(EquatorialMultiplier, EquatorialMultiplier * 1.5f);
        var maxDistance = MathF.Pow(2f, EquatorialMultiplier * 6f);
        var cornerDistance = MathF.Sqrt(WorldRange * WorldRange + WorldRange * WorldRange);

        for (var radius = minDistance; radius <= maxDistance; radius *= EquatorialMultiplier)
        {
            if (radius > cornerDistance)
                continue;

            var color = Color.ToSrgb(gridLines).WithAlpha(0.05f);
            handle.DrawCircle(new Vector2(MidPoint, MidPoint), MinimapScale * radius, color, false);
        }

        const int gridLinesRadial = 8;

        // TODO: If bounds entirely within circle don't draw it goob.
        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            // TODO: Handle distance properly.
            var aExtent = angle.ToVec() * ScaledMinimapRadius * 1.5f;
            var lineColor = Color.MediumSpringGreen.WithAlpha(0.02f);
            handle.DrawLine(new Vector2(MidPoint, MidPoint) - aExtent, new Vector2(MidPoint, MidPoint) + aExtent, lineColor);
        }
    }
}
