﻿using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Corvax.EmotesMenu;


[GenerateTypedNameReferences]
public sealed partial class EmotesMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private readonly SpriteSystem _spriteSystem;

    public event Action<ProtoId<EmotePrototype>>? OnPlayEmote;

    public EmotesMenu(EntityUid owner, EmotesMenuBoundUserInterface bui)
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _spriteSystem = _entManager.System<SpriteSystem>();

        var main = FindControl<RadialContainer>("Main");

        var emotes = _prototypeManager.EnumeratePrototypes<EmotePrototype>();
        foreach (var emote in emotes)
        {
            var player = _playerManager.LocalSession?.AttachedEntity;
            if (emote.Category == EmoteCategory.Invalid ||
                emote.ChatTriggers.Count == 0 ||
                !(player.HasValue && (emote.Whitelist?.IsValid(player.Value, _entManager) ?? true)))
                continue;

            if (!emote.Available &&
                _entManager.TryGetComponent<SpeechComponent>(player.Value, out var speech) &&
                !speech.AllowedEmotes.Contains(emote.ID))
                continue;

            var parent = FindControl<RadialContainer>(emote.Category.ToString());

            var button = new EmoteMenuButton
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64f, 64f),
                ToolTip = emote.ID, // TODO: Add name to prototype
                ProtoId = emote.ID,
            };

            var tex = new TextureRect
            {
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Texture = _spriteSystem.Frame0(emote.Icon),
                TextureScale = new Vector2(2f, 2f),
            };

            button.AddChild(tex);
            parent.AddChild(button);
            foreach (var child in main.Children)
            {
                if (child is not RadialMenuTextureButton castChild)
                    continue;

                if (castChild.TargetLayer == emote.Category.ToString())
                {
                    castChild.Visible = true;
                    break;
                }
            }
        }


        // Set up menu actions
        foreach (var child in Children)
        {
            if (child is not RadialContainer container)
                continue;
            AddEmoteClickAction(container);
        }
    }

    private void AddEmoteClickAction(RadialContainer container)
    {
        foreach (var child in container.Children)
        {
            if (child is not EmoteMenuButton castChild)
                continue;

            castChild.OnButtonUp += _ =>
            {
                OnPlayEmote?.Invoke(castChild.ProtoId);
                Close();
            };
        }
    }
}


public sealed class EmoteMenuButton : RadialMenuTextureButton
{
    public ProtoId<EmotePrototype> ProtoId { get; set; }
}
