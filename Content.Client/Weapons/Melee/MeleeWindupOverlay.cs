using Content.Client.DoAfter;
using Content.Client.Resources;
using Content.Shared.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Melee;

public sealed class MeleeWindupOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private Texture _texture;
    private readonly ShaderInstance _shader;

    public MeleeWindupOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var cache = IoCManager.Resolve<IResourceCache>();
        _texture = cache.GetTexture("/Textures/Interface/Misc/progress_bar.rsi/icon.png");
        _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var tickTime = (float) _timing.TickPeriod.TotalSeconds;
        var tickFraction = _timing.TickFraction / (float) ushort.MaxValue * tickTime;

        // If you use the display UI scale then need to set max(1f, displayscale) because 0 is valid.
        const float scale = 1f;
        var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3.CreateRotation(-rotation);
        handle.UseShader(_shader);

        // TODO: Need active DoAfter component (or alternatively just make DoAfter itself active)
        foreach (var comp in _entManager.EntityQuery<MeleeWeaponComponent>(true))
        {
            if (comp.WindupAccumulator < SharedMeleeWeaponSystem.AttackBuffer)
                continue;

            if (!xformQuery.TryGetComponent(comp.Owner, out var xform) ||
                xform.MapID != args.MapId)
            {
                continue;
            }

            var worldPosition = _transform.GetWorldPosition(xform);
            var worldMatrix = Matrix3.CreateTranslation(worldPosition);
            Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
            Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);

            handle.SetTransform(matty);
            var offset = -_texture.Height / scale;

            // Use the sprite itself if we know its bounds. This means short or tall sprites don't get overlapped
            // by the bar.
            float yOffset;
            if (spriteQuery.TryGetComponent(comp.Owner, out var sprite))
            {
                yOffset = -sprite.Bounds.Height / 2f - 0.05f;
            }
            else
            {
                yOffset = -0.5f;
            }

            // Position above the entity (we've already applied the matrix transform to the entity itself)
            // Offset by the texture size for every do_after we have.
            var position = new Vector2(-_texture.Width / 2f / EyeManager.PixelsPerMeter,
                yOffset / scale + offset / EyeManager.PixelsPerMeter * scale);

            // Draw the underlying bar texture
            handle.DrawTexture(_texture, position);

            // Draw the items overlapping the texture
            const float startX = 2f;
            const float endX = 22f;

            // Area marking where to release
            var ReleaseWidth = 2f * SharedMeleeWeaponSystem.GracePeriod / comp.WindupTime * EyeManager.PixelsPerMeter;
            var releaseMiddle = (endX - startX) / 2f + startX;

            var releaseBox = new Box2(new Vector2(releaseMiddle - ReleaseWidth / 2f, 3f) / EyeManager.PixelsPerMeter,
                new Vector2(releaseMiddle + ReleaseWidth / 2f, 4f) / EyeManager.PixelsPerMeter);

            releaseBox = releaseBox.Translated(position);
            handle.DrawRect(releaseBox, Color.LimeGreen);

            var fraction = (comp.WindupAccumulator + SharedMeleeWeaponSystem.GracePeriod - SharedMeleeWeaponSystem.AttackBuffer) / (comp.WindupTime - SharedMeleeWeaponSystem.AttackBuffer);

            var lerp = fraction.Equals(0f) ? 0f : tickFraction;
            var sign = comp.Accumulating ? 1 : -1;
            var lerpedFraction = MathF.Min(1f, (fraction + lerp * sign));
            lerpedFraction = SharedMeleeWeaponSystem.GetModifier(lerpedFraction);

            var xPos = (endX - startX) * lerpedFraction + startX;

            // In pixels
            const float Width = 2f;
            // If we hit the end we won't draw half the box so we need to subtract the end pos from it
            var endPos = xPos + Width / 2f;

            var box = new Box2(new Vector2(Math.Max(startX, endPos - Width), 3f) / EyeManager.PixelsPerMeter,
                new Vector2(Math.Min(endX, endPos), 4f) / EyeManager.PixelsPerMeter);

            box = box.Translated(position);
            handle.DrawRect(box, Color.White);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }
}
