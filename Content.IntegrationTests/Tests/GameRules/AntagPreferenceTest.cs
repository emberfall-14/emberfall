#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.GameRules;

// Once upon a time, players in the lobby weren't ever considered eligible for antag roles.
// Lets not let that happen again.
[TestFixture]
public sealed class AntagPreferenceTest
{
    [Test]
    public async Task TestLobbyPlayersValid()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        var server = pair.Server;
        var client = pair.Client;
        var ticker = server.System<GameTicker>();
        var sys = server.System<AntagSelectionSystem>();

        // Initially in the lobby
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        EntityUid uid = default;
        await server.WaitPost(() => uid = server.EntMan.Spawn("Traitor"));
        var rule = new Entity<AntagSelectionComponent>(uid, server.EntMan.GetComponent<AntagSelectionComponent>(uid));
        var def = rule.Comp.Definitions.Single();

        // IsSessionValid & IsEntityValid are preference agnostic and should always be true for players in the lobby.
        // Though maybe that will change in the future, but then GetPlayerPool() needs to be updated to reflect that.
        Assert.That(sys.IsSessionValid(rule, pair.Player, def), Is.True);
        Assert.That(sys.IsEntityValid(client.AttachedEntity, def), Is.True);

        // By default, traitor/antag preferences are disabled, so the pool should be empty.
        var sessions = new List<ICommonSession> { pair.Player! };
        var pool = sys.GetPlayerPool(rule, sessions, def);
        Assert.That(pool.Count, Is.EqualTo(0));

        // Opt into the traitor role.
        await pair.SetAntagPreference("Traitor", true);

        Assert.That(sys.IsSessionValid(rule, pair.Player, def), Is.True);
        Assert.That(sys.IsEntityValid(client.AttachedEntity, def), Is.True);
        pool = sys.GetPlayerPool(rule, sessions, def);
        Assert.That(pool.Count, Is.EqualTo(1));
        pool.TryPickAndTake(pair.Server.ResolveDependency<IRobustRandom>(), out var picked);
        Assert.That(picked, Is.EqualTo(pair.Player));
        Assert.That(sessions.Count, Is.EqualTo(1));

        // opt back out
        await pair.SetAntagPreference("Traitor", false);

        Assert.That(sys.IsSessionValid(rule, pair.Player, def), Is.True);
        Assert.That(sys.IsEntityValid(client.AttachedEntity, def), Is.True);
        pool = sys.GetPlayerPool(rule, sessions, def);
        Assert.That(pool.Count, Is.EqualTo(0));

        await server.WaitPost(() => server.EntMan.DeleteEntity(uid));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestPlayerBannedFromAntagRole()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        var server = pair.Server;
        var client = pair.Client;
        var ticker = server.System<GameTicker>();
        var sys = server.System<AntagSelectionSystem>();
        var banManager = server.ResolveDependency<IBanManager>();

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        EntityUid uid = default;
        await server.WaitPost(() => uid = server.EntMan.Spawn("Traitor"));
        var rule = new Entity<AntagSelectionComponent>(uid, server.EntMan.GetComponent<AntagSelectionComponent>(uid));
        var def = rule.Comp.Definitions.Single();

        // Ban the player from the "Traitor" role
        await server.WaitPost(() =>
        {
            var banDef = new ServerRoleBanDef(
                null,
                client.User!.Value,
                null,
                null,
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddMinutes(60),
                null,
                TimeSpan.Zero,
                "Test ban",
                NoteSeverity.Medium,
                null,
                null,
                $"{BanManager.AntagPrefix}Traitor"
            );
            banManager.CreateRoleBan(client.User!.Value, "TestUser", null, null, null, "Traitor", 60, NoteSeverity.Medium, "Test ban", DateTimeOffset.Now);
        });

        // Opt into the traitor role.
        await pair.SetAntagPreference("Traitor", true);

        // Check if the player is valid for the antag role
        Assert.That(sys.IsSessionValid(rule, pair.Player, def), Is.False);

        var sessions = new List<ICommonSession> { pair.Player! };
        var pool = sys.GetPlayerPool(rule, sessions, def);
        Assert.That(pool.Count, Is.EqualTo(0));

        await server.WaitPost(() => server.EntMan.DeleteEntity(uid));
        await pair.CleanReturnAsync();
    }
}
