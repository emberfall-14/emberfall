using Content.Client.Stylesheets;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Controls
{
    [GenerateTypedNameReferences]
    public partial class EmbossedLabel : Container
    {
        public EmbossedLabel()
        {
            RobustXamlLoader.Load(this);
            XamlChildren = ContentsContainer.Children;
        }
    }
}
