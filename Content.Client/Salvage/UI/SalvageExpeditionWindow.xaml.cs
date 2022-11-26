using Content.Client.Computer;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Salvage.UI;

[GenerateTypedNameReferences]
public sealed partial class SalvageExpeditionWindow : FancyWindow,
    IComputerWindow<EmergencyConsoleBoundUserInterfaceState>
{
    public event Action<ushort>? ClaimMission;

    public SalvageExpeditionWindow()
    {
        RobustXamlLoader.Load(this);
    }

    public void UpdateState(SalvageExpeditionConsoleState state)
    {
        Container.DisposeAllChildren();

        for (var i = 0; i < state.Missions.Count; i++)
        {
            var mission = state.Missions[i];

            var claimButton = new Button()
            {
                Text = "Claim",
                HorizontalAlignment = HAlignment.Right,
            };

            claimButton.OnPressed += args =>
            {
                ClaimMission?.Invoke(mission.Index);
            };

            var lBox = new BoxContainer()
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical
            };

            // Mission
            lBox.AddChild(new Label()
            {
                Text = $"Mission:"
            });

            lBox.AddChild(new Label()
            {
                Text = mission.MissionType.ToString(),
                FontColorOverride = Color.Gold,
                HorizontalAlignment = HAlignment.Left,
            });

            // Environment
            lBox.AddChild(new Label()
            {
                Text = $"Environment:"
            });

            lBox.AddChild(new Label()
            {
                Text = mission.Environment.ToString(),
                FontColorOverride = Color.Gold,
                HorizontalAlignment = HAlignment.Left,
            });

            var box = new BoxContainer()
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Children =
                {
                    lBox,
                    new Control()
                    {
                        HorizontalExpand = true,
                    },
                    claimButton,
                },
                HorizontalExpand = true,
            };

            LayoutContainer.SetAnchorPreset(box, LayoutContainer.LayoutPreset.Wide);

            Container.AddChild(box);

            if (i == state.Missions.Count - 1)
                continue;

            Container.AddChild(new HLine()
            {
                Color = StyleNano.NanoGold,
                Thickness = 2,
                Margin = new Thickness()
                {
                    Top = 10f,
                    Bottom = 10f,
                }
            });
        }
    }
}
