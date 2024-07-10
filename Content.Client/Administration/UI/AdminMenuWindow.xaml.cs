using System.Numerics;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI;

[GenerateTypedNameReferences]
public sealed partial class AdminMenuWindow : DefaultWindow
{
    public event Action? OnDisposed;

        public AdminMenuWindow()
        {
            MinSize = new Vector2(650, 250);
            Title = Loc.GetString("admin-menu-title");
            RobustXamlLoader.Load(this);
            MasterTabContainer.SetTabTitle(0, Loc.GetString("admin-menu-admin-tab"));
            MasterTabContainer.SetTabTitle(1, Loc.GetString("admin-menu-adminbus-tab"));
            MasterTabContainer.SetTabTitle(2, Loc.GetString("admin-menu-atmos-tab"));
            MasterTabContainer.SetTabTitle(3, Loc.GetString("admin-menu-round-tab"));
            MasterTabContainer.SetTabTitle(4, Loc.GetString("admin-menu-server-tab"));
            MasterTabContainer.SetTabTitle(5, Loc.GetString("admin-menu-panic-bunker-tab"));
            MasterTabContainer.SetTabTitle(6, Loc.GetString("admin-menu-players-tab"));
            MasterTabContainer.SetTabTitle(7, Loc.GetString("admin-menu-objects-tab"));
        }

    protected override void Dispose(bool disposing)
    {
        OnDisposed?.Invoke();
        base.Dispose(disposing);
        OnDisposed = null;
    }

    private enum TabIndex
    {
        Admin = 0,
        Adminbus,
        Atmos,
        Round,
        Server,
        PanicBunker,
        BabyJail,
        Players,
        Objects,
    }
}
