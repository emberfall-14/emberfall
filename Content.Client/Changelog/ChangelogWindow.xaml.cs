using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Console;

namespace Content.Client.Changelog
{
    [GenerateTypedNameReferences]
    public sealed partial class ChangelogWindow : FancyWindow
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly ChangelogManager _changelog = default!;

        public ChangelogWindow()
        {
            RobustXamlLoader.Load(this);
            WindowTitle.AddStyleClass(Stylesheets.Redux.StyleClasses.LabelHeading);
            Stylesheet = IoCManager.Resolve<IStylesheetManager>().SheetSpace;
        }

        protected override void Opened()
        {
            base.Opened();

            _changelog.SaveNewReadId();
            PopulateChangelog();
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _adminManager.AdminStatusUpdated -= OnAdminStatusUpdated;
        }

        private void OnAdminStatusUpdated()
        {
            TabsUpdated();
        }

        private async void PopulateChangelog()
        {
            // Changelog is not kept in memory so load it again.
            var changelogs = await _changelog.LoadChangelog();

            Tabs.DisposeAllChildren();

            var i = 0;
            foreach (var changelog in changelogs)
            {
                var tab = new ChangelogTab { AdminOnly = changelog.AdminOnly };
                tab.PopulateChangelog(changelog);

                Tabs.AddChild(tab);
                Tabs.SetTabTitle(i++, Loc.GetString($"changelog-tab-title-{changelog.Name}"));
            }

            var version = typeof(ChangelogWindow).Assembly.GetName().Version ?? new Version(1, 0);
            VersionLabel.Text = Loc.GetString("changelog-version-tag", ("version", version.ToString()));

            TabsUpdated();
        }

        private void TabsUpdated()
        {
            var tabs = Tabs.Children.OfType<ChangelogTab>().ToArray();
            var isAdmin = _adminManager.IsAdmin(true);

            var visibleTabs = 0;
            int? firstVisible = null;
            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];

                if (!tab.AdminOnly || isAdmin)
                {
                    Tabs.SetTabVisible(i, true);
                    visibleTabs++;
                    firstVisible ??= i;
                }
                else
                {
                    Tabs.SetTabVisible(i, false);
                }
            }

            Tabs.TabsVisible = visibleTabs > 1;

            // Current tab became invisible, select the first one that is visible
            if (!Tabs.GetTabVisible(Tabs.CurrentTab) && firstVisible != null)
            {
                Tabs.CurrentTab = firstVisible.Value;
            }

            // We are only displaying one tab, hide its header
            if (!Tabs.TabsVisible && firstVisible != null)
            {
                Tabs.SetTabVisible(firstVisible.Value, false);
            }
        }
    }

    [UsedImplicitly, AnyCommand]
    public sealed class ChangelogCommand : IConsoleCommand
    {
        public string Command => "changelog";
        public string Description => "Opens the changelog";
        public string Help => "Usage: changelog";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IUserInterfaceManager>().GetUIController<ChangelogUIController>().OpenWindow();
        }
    }
}
