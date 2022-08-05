﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.Players.PlayTimeTracking;

public sealed class PlayTimeTrackingManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private Dictionary<string, TimeSpan> _roles = new();

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgPlayTime>(RxRoleTimers);

        _client.RunLevelChanged += ClientOnRunLevelChanged;
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // Reset on disconnect, just in case.
            _roles.Clear();
        }
    }

    private void RxRoleTimers(MsgPlayTime message)
    {
        _roles = message.Trackers;
    }

    public bool IsAllowed(JobPrototype job, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        if (job.Requirements == null ||
            !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        var player = _playerManager.LocalPlayer?.Session;

        if (player == null) return true;

        var roles = _roles;
        var reasonBuilder = new StringBuilder();

        var first = true;
        foreach (var requirement in job.Requirements)
        {
            if (JobRequirements.TryRequirementMet(requirement, roles, out reason, _prototypes))
                continue;

            if (!first)
                reasonBuilder.Append('\n');
            first = false;

            reasonBuilder.AppendLine(reason);
        }

        reason = reasonBuilder.Length == 0 ? null : reasonBuilder.ToString();
        return reason == null;
    }
}
