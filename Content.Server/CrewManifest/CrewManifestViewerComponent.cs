using Content.Shared.Actions;

namespace Content.Server.CrewManifest;

[RegisterComponent]
public sealed class CrewManifestViewerComponent : Component
{
    /// <summary>
    ///     If this manifest viewer is unsecure or not. If it is,
    ///     CCVars.CrewManifestUnsecure being false will
    ///     not allow this entity to be processed by CrewManifestSystem.
    /// </summary>
    [DataField("unsecure")] public bool Unsecure;
}

public sealed class ViewManifestActionEvent : InstantActionEvent { }
