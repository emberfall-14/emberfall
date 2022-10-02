namespace Content.Server.Nuke;

/// <summary>
///     This handles labelling an entity with a nuclear bomb label.
/// </summary>
public sealed class NukeLabelSystem : EntitySystem
{
    [Dependency] private readonly NukeSystem _nuke = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NukeLabelComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, NukeLabelComponent nuke, MapInitEvent args)
    {
        var label = Loc.GetString(nuke.NukeLabel, ("serial", _nuke.GenerateRandomNumberString(nuke.SerialLength)));
        var meta = MetaData(uid);
        MetaData(uid).EntityName = $"{meta.EntityName} ({label})";
    }
}
