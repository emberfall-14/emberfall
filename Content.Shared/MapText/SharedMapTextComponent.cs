﻿using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MapText;

/// <summary>
/// This is used for displaying text in world space
/// </summary>

[NetworkedComponent, Access(typeof(SharedMapTextSystem))]
public abstract partial class SharedMapTextComponent : Component
{
    public const string DefaultFont = "Default";

    [DataField]
    public string? Text;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public string FontId = DefaultFont;

    [DataField]
    public int FontSize = 12;

    [DataField]
    public Vector2 Offset = Vector2.Zero;
}

[Serializable, NetSerializable]
public sealed class MapTextComponentState : ComponentState
{
    public string? Text { get; init;}
    public Color Color { get; init;}
    public string FontId { get; init; } = default!;
    public int FontSize { get; init;}
    public Vector2 Offset { get; init;}
}
