using System.Text;
using Content.Shared.Inventory;
using Content.Shared.Renamer.Components;

namespace Content.Shared.Renamer.EntitySystems;

public sealed partial class RenamerSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenamerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RenamerComponent> entity, ref MapInitEvent args)
    {
        // Set the base name to the current name
        SetBaseName((entity, entity.Comp), Name(entity));
    }

    /// <summary>
    /// Sets the <see cref="RenamerComponent.BaseName"/> for this entity.
    /// This will add a <see cref="RenamerComponent"/> if there isn't one already.
    /// This will call <see cref="RefreshNameModifiers"/>.
    /// </summary>
    /// <remarks>
    /// Use this to make a permanent change to an entity's name that will play nicely
    /// with name modifiers like pre- and postfixes. For temporary changes, instead
    /// subscribe to <see cref="RefreshNameModifiersEvent"/> and call <see cref="RefreshNameModifiers"/>
    /// when needed.
    /// </remarks>
    public void SetBaseName(Entity<RenamerComponent?> entity, string name)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            entity.Comp = EnsureComp<RenamerComponent>(entity);

        if (name == entity.Comp.BaseName)
            return;

        // Set the base name to the new name
        entity.Comp.BaseName = name;
        Dirty(entity);

        // Apply any modifiers if needed
        RefreshNameModifiers(entity);
    }

    /// <summary>
    /// Raises a <see cref="RefreshNameModifiersEvent"/> to gather modifiers and
    /// updates the entity's name to its base name with modifiers applied.
    /// This will add a <see cref="RenamerComponent"/> if there isn't one already.
    /// </summary>
    /// <remarks>
    /// Call this to update the entity's name when adding or removing a modifier.
    /// </remarks>
    public void RefreshNameModifiers(Entity<RenamerComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            entity.Comp = EnsureComp<RenamerComponent>(entity);

        var meta = MetaData(entity);

        // Raise an event to get any modifiers
        var modifierEvent = new RefreshNameModifiersEvent(entity.Comp.BaseName);
        RaiseLocalEvent(entity, ref modifierEvent);

        // Get the final name with modifiers applied
        var modifiedName = modifierEvent.GetModifiedName();

        if (modifiedName != meta.EntityName)
        {
            // Set the entity's name with modifiers
            _metaData.SetEntityName(entity, modifiedName, meta);
            Dirty(entity.Owner, meta);
        }

        if (modifiedName == entity.Comp.BaseName)
        {
            // We're not actually changing the name at this point, so we can go away
            RemCompDeferred<RenamerComponent>(entity);
        }
    }
}

/// <summary>
/// Raised on an entity when <see cref="RenamerSystem.RefreshNameModifiers"/> is called.
/// Subscribe to this event and use its methods to add modifiers to the entity's name.
/// </summary>
[ByRefEvent]
public sealed class RefreshNameModifiersEvent : IInventoryRelayEvent
{
    /// <summary>
    /// The entity's name without any modifiers applied.
    /// If you want to base a modifier on the entity's name, use
    /// this so you don't include other modifiers.
    /// </summary>
    public readonly string BaseName;
    private readonly List<string> _prefixes = [];
    private readonly List<string> _postfixes = [];
    private (string Text, int Priority)? _override;

    /// <inheritdoc/>
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public RefreshNameModifiersEvent(string baseName)
    {
        BaseName = baseName;
    }

    /// <summary>
    /// Adds a prefix before the entity's name.
    /// </summary>
    public void AddPrefix(string text)
    {
        _prefixes.Add(text);
    }

    /// <summary>
    /// Adds a postfix after the entity's name.
    /// </summary>
    public void AddPostfix(string text)
    {
        _postfixes.Add(text);
    }

    /// <summary>
    /// Adds text that will override the <see cref="RenamerComponent.BaseName"/> of the entity.
    /// If multiple overrides are applied to an entity, the one with the highest <paramref name="priority"/>
    /// will be used.
    /// </summary>
    public void AddOverride(string text, int priority = 0)
    {
        if (_override == null || priority > _override.Value.Priority)
            _override = (text, priority);
    }

    /// <summary>
    /// Returns the final name with all modifiers applied.
    /// </summary>
    public string GetModifiedName()
    {
        var sb = new StringBuilder();

        // Add all prefixes
        foreach (var prefix in _prefixes)
        {
            sb.Append($"{prefix} ");
        }

        // Add the override name if there is one, otherwise the original name
        sb.Append(_override?.Text ?? BaseName);

        // Add all postfixes
        foreach (var postfix in _postfixes)
        {
            sb.Append($" {postfix}");
        }

        return sb.ToString();
    }
}
