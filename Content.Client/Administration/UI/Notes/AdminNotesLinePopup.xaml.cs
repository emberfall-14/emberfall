using System.Threading.Tasks;
using Content.Shared.Administration.Notes;
using Content.Shared.Database;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Administration.UI.Notes;

[GenerateTypedNameReferences]
public sealed partial class AdminNotesLinePopup : Popup
{
    public event Action<int, NoteType>? OnEditPressed;
    public event Action<int, NoteType>? OnDeletePressed;

    public AdminNotesLinePopup(SharedAdminNote note, string playerName, bool showDelete, bool showEdit)
    {
        RobustXamlLoader.Load(this);

        NoteId = note.Id;
        NoteType = note.NoteType;
        DeleteButton.Visible = showDelete;
        EditButton.Visible = showEdit;

        UserInterfaceManager.ModalRoot.AddChild(this);

        PlayerNameLabel.Text = Loc.GetString("admin-notes-for", ("player", playerName));
        IdLabel.Text = Loc.GetString("admin-notes-id", ("id", note.Id));
        TypeLabel.Text = Loc.GetString("admin-notes-type", ("type", note.NoteType));
        SeverityLabel.Text = Loc.GetString("admin-notes-severity", ("severity", note.NoteSeverity ?? NoteSeverity.None));
        RoundIdLabel.Text = note.Round == null
            ? Loc.GetString("admin-notes-round-id-unknown")
            : Loc.GetString("admin-notes-round-id", ("id", note.Round));
        CreatedByLabel.Text = Loc.GetString("admin-notes-created-by", ("author", note.CreatedByName));
        CreatedAtLabel.Text = Loc.GetString("admin-notes-created-at", ("date", note.CreatedAt.ToString("dd MMM yyyy HH:mm:ss")));
        EditedByLabel.Text = Loc.GetString("admin-notes-last-edited-by", ("author", note.EditedByName));
        EditedAtLabel.Text = Loc.GetString("admin-notes-last-edited-at", ("date", note.LastEditedAt?.ToString("dd MMM yyyy HH:mm:ss") ?? Loc.GetString("admin-notes-edited-never")));
        ExpiryTimeLabel.Text = note.ExpiryTime == null
            ? Loc.GetString("admin-notes-expires-never")
            : Loc.GetString("admin-notes-expires", ("expires", note.ExpiryTime.Value.ToString("dd MMM yyyy HH:mm:ss")));
        NoteTextEdit.InsertAtCursor(note.Message);

        if (note.NoteType is NoteType.ServerBan or NoteType.RoleBan)
        {
            DeleteButton.Text = Loc.GetString("admin-notes-hide");
        }

        EditButton.OnPressed += EditPressed;
        DeleteButton.OnPressed += DeletePressed;
    }

    private int NoteId { get; }
    private NoteType NoteType { get; }
    private bool ConfirmingDelete { get; set; }

    private void EditPressed(ButtonEventArgs args)
    {
        OnEditPressed?.Invoke(NoteId, NoteType);
        Close();
    }

    private void DeletePressed(ButtonEventArgs args)
    {
        if (!ConfirmingDelete)
        {
            ConfirmingDelete = true;
            // Task.Delay(3000).ContinueWith(_ => ResetDeleteButton()); // TODO: fix
            DeleteButton.Text = Loc.GetString("admin-notes-delete-confirm");
            DeleteButton.ModulateSelfOverride = Color.Red;
            return;
        }

        ResetDeleteButton();
        OnDeletePressed?.Invoke(NoteId, NoteType);
        Close();
    }

    private void ResetDeleteButton()
    {
        ConfirmingDelete = false;
        DeleteButton.Text = Loc.GetString("admin-notes-delete");
        DeleteButton.ModulateSelfOverride = null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        EditButton.OnPressed -= EditPressed;
        DeleteButton.OnPressed -= DeletePressed;

        OnEditPressed = null;
        OnDeletePressed = null;
    }
}
