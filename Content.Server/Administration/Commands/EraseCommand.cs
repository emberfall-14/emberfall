using System.Linq;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class EraseCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IEntitySystemManager _system = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "erase";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("cmd-erase-invalid-args"));
            shell.WriteLine(Help);
            return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(args[0]);

        if (located == null)
        {
            shell.WriteLine(Loc.GetString("cmd-erase-player-not-found"));
            return;
        }

        if (_system.TryGetEntitySystem<AdminSystem>(out var adminSystem))
        {
            adminSystem.Erase(located.UserId);
        }
    }

  public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-erase-player-completion"));

    }
}
