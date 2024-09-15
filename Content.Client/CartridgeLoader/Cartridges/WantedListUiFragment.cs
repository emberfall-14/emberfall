using System.Linq;
using Content.Client.CartridgeLoader.UI;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Security;
using Robust.Client.AutoGenerated;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class WantedListUiFragment : BoxContainer
{
    [Dependency] private readonly IResourceCache _cache = default!;

    private string? _selectedTargetName;
    private List<WantedRecord> _wantedRecords = new();

    public WantedListUiFragment()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        SearchBar.OnTextChanged += OnSearchBarTextChanged;
    }

    private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
    {
        var found = !String.IsNullOrWhiteSpace(args.Text)
            ? _wantedRecords.FindAll(r =>
                r.TargetInfo.Name.Contains(args.Text) ||
                r.Status.ToString().Contains(args.Text, StringComparison.OrdinalIgnoreCase))
            : _wantedRecords;

        UpdateState(found, false);
    }

    public void UpdateState(List<WantedRecord> records, bool refresh = true)
    {
        RecordsList.Clear();

        if (records.Count == 0)
        {
            NoRecords.Visible = true;
            RecordsList.Visible = false;
            RecordUnselected.Visible = false;
            PersonContainer.Visible = false;

            _selectedTargetName = null;
            if (refresh)
                _wantedRecords.Clear();

            return;
        }

        NoRecords.Visible = false;
        RecordsList.Visible = true;
        RecordUnselected.Visible = true;
        PersonContainer.Visible = false;

        foreach (var record in records)
        {
            var rsi = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot /
                                                      "Interface/Misc/security_icons.rsi");
            var stateName = "hud_" + record.Status switch
            {
                SecurityStatus.Detained => "incarcerated",
                _ => record.Status.ToString().ToLower(),
            };
            var addedItem = RecordsList.AddItem(
                record.TargetInfo.Name,
                rsi.RSI.TryGetState(stateName, out var state) ? state.Frame0 : null,
                metadata: record);
            addedItem.Selected = String.Equals(record.TargetInfo.Name, _selectedTargetName);
        }

        RecordsList.OnItemSelected += OnItemSelected;

        if (refresh)
            _wantedRecords = records;
    }

    private void OnItemSelected(StatusList.ItemListSelectedEventArgs args)
    {
        // Destruct args
        var (index, list) = (args.ItemIndex, args.ItemList);
        // Get record from metadata
        if (!list.TryGetValue(index, out var item) || item.Metadata is not WantedRecord record)
            return;

        FormattedMessage GetLoc(string fluentId, params (string,object)[] args)
        {
            var msg = new FormattedMessage();
            var fluent = Loc.GetString(fluentId, args);
            msg.AddMarkupPermissive(fluent);
            return msg;
        }

        // Set personal info
        PersonName.Text = record.TargetInfo.Name;
        TargetAge.SetMessage(GetLoc(
            "wanted-list-age-label",
            ("age", record.TargetInfo.Age)
        ));
        TargetJob.SetMessage(GetLoc(
            "wanted-list-job-label",
            ("job", record.TargetInfo.JobTitle.ToLower())
        ));
        TargetSpecies.SetMessage(GetLoc(
            "wanted-list-species-label",
            ("species", record.TargetInfo.Species.ToLower())
        ));
        TargetGender.SetMessage(GetLoc(
            "wanted-list-gender-label",
            ("gender", record.TargetInfo.Gender)
        ));

        // Set reason
        WantedReason.SetMessage(GetLoc(
            "wanted-list-reason-label",
            ("reason", record.Reason ?? Loc.GetString("wanted-list-unknown-reason-label"))
        ));

        // Set status
        PersonState.SetMessage(GetLoc(
            "wanted-list-status-label",
            ("status", record.Status.ToString().ToLower())
        ));

        // Set initiator
        InitiatorName.SetMessage(GetLoc(
            "wanted-list-initiator-label",
            ("initiator", record.Initiator ?? Loc.GetString("wanted-list-unknown-initiator-label"))
        ));

        // History table

        // Clear table if it exists
        HistoryTable.RemoveAllChildren();

        HistoryTable.AddChild(new Label()
        {
            Text = Loc.GetString("wanted-list-history-table-time-col"),
            StyleClasses = { "LabelSmall" },
            HorizontalAlignment = HAlignment.Center,
        });
        HistoryTable.AddChild(new Label()
        {
            Text = Loc.GetString("wanted-list-history-table-reason-col"),
            StyleClasses = { "LabelSmall" },
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
        });

        HistoryTable.AddChild(new Label()
        {
            Text = Loc.GetString("wanted-list-history-table-initiator-col"),
            StyleClasses = { "LabelSmall" },
            HorizontalAlignment = HAlignment.Center,
        });

        if (record.History.Count > 0)
        {
            HistoryTable.Visible = true;

            foreach (var history in record.History.OrderByDescending(h => h.AddTime))
            {
                HistoryTable.AddChild(new Label()
                {
                    Text = $"{history.AddTime.Hours:00}:{history.AddTime.Minutes:00}:{history.AddTime.Seconds:00}",
                    StyleClasses = { "LabelSmall" },
                    VerticalAlignment = VAlignment.Top,
                });

                HistoryTable.AddChild(new RichTextLabel()
                {
                    Text = $"[color=white]{history.Crime}[/color]",
                    HorizontalExpand = true,
                    VerticalAlignment = VAlignment.Top,
                    StyleClasses = { "LabelSubText" },
                    Margin = new(10f, 0f),
                });

                HistoryTable.AddChild(new RichTextLabel()
                {
                    Text = $"[color=white]{history.InitiatorName}[/color]",
                    StyleClasses = { "LabelSubText" },
                    VerticalAlignment = VAlignment.Top,
                });
            }
        }

        RecordUnselected.Visible = false;
        PersonContainer.Visible = true;

        // Save selected item
        _selectedTargetName = record.TargetInfo.Name;
    }
}
