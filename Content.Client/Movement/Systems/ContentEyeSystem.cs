using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public void RequestZoom(EntityUid uid, Vector2 zoom, ContentEyeComponent? content = null)
    {
        if (!Resolve(uid, ref content, false))
            return;

        RaisePredictiveEvent(new RequestTargetZoomEvent()
        {
            TargetZoom = zoom,
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var localPlayer = _player.LocalPlayer?.ControlledEntity;

        if (!TryComp<ContentEyeComponent>(localPlayer, out var content) ||
            !TryComp<EyeComponent>(localPlayer, out var eye))
        {
            return;
        }

        UpdateEye(localPlayer.Value, content, eye, frameTime);
    }
}
