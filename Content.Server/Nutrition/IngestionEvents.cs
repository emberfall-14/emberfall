using Content.Shared.Nutrition;

namespace Content.Server.Nutrition;

/// <summary>
///     Raised directed at the consumer when attempting to ingest something.
/// </summary>
public sealed class IngestionAttemptEvent : SharedIngestionAttemptEvent
{
    /// <summary>
    ///     The equipment that is blocking consumption. Should only be non-null if the event was canceled.
    /// </summary>
}
/// <summary>
/// Raised directed at the food after finishing eating a food before it's deleted.
/// Cancel this if you want to do something special before a food is deleted.
/// </summary>
public sealed class BeforeFullyEatenEvent : SharedBeforeFullyEatenEvent
{
    /// <summary>
    /// The person that ate the food.
    /// </summary>
}

/// <summary>
/// Raised directed at the food being sliced before it's deleted.
/// Cancel this if you want to do something special before a food is deleted.
/// </summary>
public sealed class BeforeFullySlicedEvent : SharedBeforeFullySlicedEvent
{
    /// <summary>
    /// The person slicing the food.
    /// </summary>
}
