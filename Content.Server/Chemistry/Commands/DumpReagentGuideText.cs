﻿using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Commands;

public sealed class DumpReagentGuideText : IConsoleCommand
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;

    public string Command => "dumpreagentguidetext";
    public string Description => "Dumps the guidebook text for a reagent to the console";
    public string Help => "dumpreagentguidetext <reagent>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var reagent = _prototype.Index<ReagentPrototype>(args[0]);

        if (reagent.Metabolisms is null)
        {
            shell.WriteLine("Nothing to dump.");
            return;
        }

        foreach (var (_, entry) in reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                shell.WriteLine(effect.GuidebookEffectDescription(_prototype, _entSys) ?? $"[skipped effect of type {effect.GetType()}]");
            }
        }
    }
}
