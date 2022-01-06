﻿using Content.Server.Chat.Managers;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal class WhisperCommand : IConsoleCommand
    {
        public string Command => "whisper";
        public string Description => "Send chat messages to the local channel as a whisper";
        public string Help => "whisper <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteError("This command cannot be run from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            if (player.AttachedEntity is not {} playerEntity)
            {
                shell.WriteError("You don't have an entity!");
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            // If player is ghost, send dead chat instead
            // Else Check for mind component of Entity.
            // If Entity has no mind, we return without sending a message and display an error.
            if (IoCManager.Resolve<IEntityManager>().HasComponent<GhostComponent>(playerEntity))
                chat.SendDeadChat(player, message);
            else
            {
                var mindComponent = player.ContentData()?.Mind;

                if (mindComponent == null)
                {
                    shell.WriteError("You don't have a mind!");
                    return;
                }

                if (mindComponent.OwnedEntity is not {Valid: true} owned)
                {
                    shell.WriteError("You don't have an entity!");
                    return;
                }

                // Check if string contains a written smiley.
                // If string does contain a smiley, remove it and generate emote instead.
                var chatSanitizer = IoCManager.Resolve<IChatSanitizationManager>();
                var emote = chatSanitizer.TrySanitizeOutSmilies(message, owned, out var sanitized, out var emoteStr);
                if (sanitized.Length != 0)
                    chat.EntityWhisper(owned, sanitized);
                if (emote)
                    chat.EntityMe(owned, emoteStr!);
            }

        }
    }
}
