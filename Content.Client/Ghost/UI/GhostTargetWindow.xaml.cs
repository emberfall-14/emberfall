using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content.Shared.Ghost;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Client.Ghost.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class GhostTargetWindow : DefaultWindow
    {
        private readonly IEntityNetworkManager _netManager;

        private List<(string, EntityUid)> _warps = new();

        public GhostTargetWindow(IEntityNetworkManager netManager)
        {
            RobustXamlLoader.Load(this);

            _netManager = netManager;
        }

        public void UpdateWarps(IEnumerable<GhostWarp> warps)
        {
            _warps = warps.Select(w =>
            {
                var name = w.Localizable
                    ? Loc.GetString("ghost-target-window-current-button", ("name", w.DisplayName))
                    : w.DisplayName;

                return (name, w.Entity);
            }).ToList();
            
            // Server COULD send these sorted but how about we just use the client to do it instead
            _warps.Sort((x, y) =>
                string.Compare(x.Item1, y.Item1, StringComparison.Ordinal));
        }

        public void Populate()
        {
            ButtonContainer.DisposeAllChildren();
            AddButtons();
        }

        private void AddButtons()
        {
            foreach (var (name, warp) in _warps)
            {
                var currentButtonRef = new Button
                {
                    Text = name,
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (340, 20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += _ =>
                {
                    var msg = new GhostWarpToTargetRequestEvent(warp);
                    _netManager.SendSystemNetworkMessage(msg);
                };

                ButtonContainer.AddChild(currentButtonRef);
            }
        }
    }
}
