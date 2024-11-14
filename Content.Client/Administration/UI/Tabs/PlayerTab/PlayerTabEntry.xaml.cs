﻿using Content.Shared.Administration;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
// using Robust.Shared.Prototypes;

namespace Content.Client.Administration.UI.Tabs.PlayerTab;

[GenerateTypedNameReferences]
public sealed partial class PlayerTabEntry : PanelContainer
{
    public NetEntity? PlayerEntity;

    public PlayerTabEntry(PlayerInfo player, StyleBoxFlat styleBoxFlat)
    {
        RobustXamlLoader.Load(this);

        UsernameLabel.Text = player.Username;
        if (!player.Connected)
            UsernameLabel.StyleClasses.Add("Disabled");
        JobLabel.Text = player.StartingJob;
        CharacterLabel.Text = player.CharacterName;
        if (player.IdentityName != player.CharacterName)
            CharacterLabel.Text += $" [{player.IdentityName}]";
        AntagonistLabel.Text = Loc.GetString(player.Antag ? "player-tab-is-antag-yes" : "player-tab-is-antag-no");
        RoleTypeLabel.Text = Loc.GetString(player.RoleProto.Name);
        RoleTypeLabel.FontColorOverride = player.RoleProto.Color;
        BackgroundColorPanel.PanelOverride = styleBoxFlat;
        OverallPlaytimeLabel.Text = player.PlaytimeString;
        PlayerEntity = player.NetEntity;
    }
}
