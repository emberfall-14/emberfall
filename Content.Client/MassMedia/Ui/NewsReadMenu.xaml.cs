using Content.Client.Message;
using Content.Shared.MassMedia.Systems;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.MassMedia.Ui;

[GenerateTypedNameReferences]
public sealed partial class NewsReadMenu : DefaultWindow
{
    public event Action? NextButtonPressed;
    public event Action? PastButtonPressed;

    public NewsReadMenu(string name)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        if (Window != null)
            Window.Title = name;

        Next.OnPressed += _ => NextButtonPressed?.Invoke();
        Past.OnPressed += _ => PastButtonPressed?.Invoke();
    }

    public void UpdateUI(NewsArticle article, int targetNum, int totalNum)
    {
        PageNum.Visible = true;
        PageText.Visible = true;
        ShareTime.Visible = true;

        PageName.Text = $"{article.Name} by {article.Author ?? "Anonymous"}";
        PageText.SetMarkup(article.Content);

        PageNum.Text = $"{targetNum}/{totalNum}";

        string shareTime = article.ShareTime.ToString("hh\\:mm\\:ss");
        ShareTime.SetMarkup($"{Loc.GetString("news-read-ui-time-prefix-text")} {shareTime}");
    }

    public void UpdateEmptyUI()
    {
        PageName.Text = Loc.GetString("news-read-ui-not-found-text");

        PageNum.Visible = false;
        PageText.Visible = false;
        ShareTime.Visible = false;
    }
}
