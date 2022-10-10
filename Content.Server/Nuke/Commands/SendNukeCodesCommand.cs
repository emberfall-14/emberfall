﻿using System.Linq;
using Content.Server.Administration;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Nuke.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SendNukeCodesCommand : IConsoleCommand
    {
        public string Command => "nukecodes";
        public string Description => "Send nuke codes to a station's communication consoles";
        public string Help => "nukecodes [station EntityUid]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
                return;
            }

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            IoCManager.Resolve<EntityManager>().System<NukeCodePaperSystem>().SendNukeCodes(uid);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length != 1)
            {
                return CompletionResult.Empty;
            }

            var entityManager = IoCManager.Resolve<EntityManager>();

            var stations = entityManager
                .System<StationSystem>()
                .Stations
                .Select(station =>
                {
                    var meta = entityManager.GetComponent<MetaDataComponent>(station);

                    return new CompletionOption(station.ToString(), meta.EntityName);
                });

            return CompletionResult.FromHintOptions(stations, null);
        }
    }
}
