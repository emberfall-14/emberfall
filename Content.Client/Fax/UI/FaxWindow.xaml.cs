using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Content.Shared.Fax;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.UserInterface;

namespace Content.Client.Fax.UI;

[GenerateTypedNameReferences]
public sealed partial class FaxWindow : DefaultWindow
{
    private FaxBoundUi _owner;

    public event Action? FileButtonPressed;
    public event Action? PaperButtonPressed;
    public event Action? CopyButtonPressed;
    public event Action? SendButtonPressed;
    public event Action? RefreshButtonPressed;
    public event Action<string>? PeerSelected;

    public bool OfficePaper = false;

    public FaxWindow(FaxBoundUi owner)
    {
        RobustXamlLoader.Load(this);

        _owner = owner;

        PaperButtonPressed += OnPaperButtonPressed;
        FileButtonPressed += OnFileButtonPressed;

        FileButton.OnPressed += _ => FileButtonPressed?.Invoke(); 
        PaperButton.OnPressed += _ => PaperButtonPressed?.Invoke(); 
        CopyButton.OnPressed += _ => CopyButtonPressed?.Invoke();
        SendButton.OnPressed += _ => SendButtonPressed?.Invoke();
        RefreshButton.OnPressed += _ => RefreshButtonPressed?.Invoke();
        PeerSelector.OnItemSelected += args =>
            PeerSelected?.Invoke((string) args.Button.GetItemMetadata(args.Id)!);
    }

    public void UpdateState(FaxUiState state)
    {
        CopyButton.Disabled = !state.CanCopy;
        SendButton.Disabled = !state.CanSend;
        FromLabel.Text = state.DeviceName;

        if (state.IsPaperInserted)
        {
            PaperStatusLabel.FontColorOverride = Color.Green;
            PaperStatusLabel.Text = Loc.GetString("fax-machine-ui-paper-inserted");
        }
        else
        {
            PaperStatusLabel.FontColorOverride = Color.Red;
            PaperStatusLabel.Text = Loc.GetString("fax-machine-ui-paper-not-inserted");
        }

        if (state.AvailablePeers.Count == 0)
        {
            PeerSelector.AddItem(Loc.GetString("fax-machine-ui-no-peers"));
            PeerSelector.Disabled = true;
        }

        if (PeerSelector.Disabled && state.AvailablePeers.Count != 0)
        {
            PeerSelector.Clear();
            PeerSelector.Disabled = false;
        }

        // always must be selected destination
        if (string.IsNullOrEmpty(state.DestinationAddress) && state.AvailablePeers.Count != 0)
        {
            PeerSelected?.Invoke(state.AvailablePeers.First().Key);
            return;
        }

        if (state.AvailablePeers.Count != 0)
        {
            PeerSelector.Clear();

            foreach (var (address, name) in state.AvailablePeers)
            {
                var id = AddPeerSelect(name, address);
                if (address == state.DestinationAddress)
                    PeerSelector.Select(id);
            }
        }
    }

    private int AddPeerSelect(string name, string address)
    {
        PeerSelector.AddItem(name);
        PeerSelector.SetItemMetadata(PeerSelector.ItemCount - 1, address);
        return PeerSelector.ItemCount - 1;
    }

    private void OnPaperButtonPressed()
    {
        OfficePaper = !OfficePaper;

        if(OfficePaper)
            PaperButton.Text = Loc.GetString("fax-machine-ui-paper-button-office");
        else
            PaperButton.Text = Loc.GetString("fax-machine-ui-paper-button-normal");
    }

    private async void OnFileButtonPressed()
    {
        //Open file select dialog
        var filters = new FileDialogFilters(new FileDialogFilters.Group("txt"));
        await using var file = await _owner.FileDialogManager.OpenFile(filters);

        //If UI gets closed of file is null return.
        if (Disposed)
            return;
        if(file == null)
            return;

        //Read the file contents and raise event.
        StreamReader reader = new StreamReader(file);
        var content = reader.ReadToEnd();
        _owner.PrintFile(content, "printed paper", OfficePaper);
    }
}
