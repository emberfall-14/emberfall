using Content.Client.UserInterface.Controls;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Client.AutoGenerated;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;

namespace Content.Client.CriminalRecords;

// TODO: dedupe shitcode from general records theres a lot
[GenerateTypedNameReferences]
public sealed partial class CriminalRecordsConsoleWindow : FancyWindow
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly AccessReaderSystem _accessReader;

    public readonly EntityUid Console;

    public Action<uint?>? OnKeySelected;
    public Action<StationRecordFilterType, string>? OnFiltersChanged;
    public Action<SecurityStatus>? OnStatusSelected;
    public Action<CriminalRecord, bool, bool>? OnHistoryUpdated;
    public Action? OnHistoryClosed;

    private bool _isPopulating;
    private bool _access;
    private uint? _selectedKey;
    private CriminalRecord? _selectedRecord;

    private StationRecordFilterType _currentFilterType;

    public CriminalRecordsConsoleWindow(EntityUid console)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _accessReader = _entity.System<AccessReaderSystem>();
        Console = console;

        _currentFilterType = StationRecordFilterType.Name;

        foreach (var item in Enum.GetValues<StationRecordFilterType>())
        {
            FilterType.AddItem(GetTypeFilterLocals(item), (int)item);
        }

        foreach (var status in Enum.GetValues<SecurityStatus>())
        {
            AddStatusSelect(status);
        }

        StatusLabel.SetMessage(FormattedMessage.FromMarkup(Loc.GetString("criminal-records-console-status")));

        RecordListing.OnItemSelected += args =>
        {
            if (_isPopulating || RecordListing[args.ItemIndex].Metadata is not uint cast)
                return;

            OnKeySelected?.Invoke(cast);
        };

        RecordListing.OnItemDeselected += _ =>
        {
            if (!_isPopulating)
                OnKeySelected?.Invoke(null);
        };

        FilterType.OnItemSelected += eventArgs =>
        {
            var type = (StationRecordFilterType)eventArgs.Id;

            if (_currentFilterType != type)
            {
                _currentFilterType = type;
                FilterListingOfRecords(FilterText.Text);
            }
        };

        FilterText.OnTextEntered += args =>
        {
            FilterListingOfRecords(args.Text);
        };

        StatusOptionButton.OnItemSelected += args =>
        {
            OnStatusSelected?.Invoke((SecurityStatus) args.Id);
        };

        HistoryButton.OnPressed += _ =>
        {
            if (_selectedRecord is {} record)
                OnHistoryUpdated?.Invoke(record, _access, true);
        };
    }

    public void UpdateState(CriminalRecordsConsoleState state)
    {
        if (state.Filter != null)
        {
            if (state.Filter.Type != _currentFilterType)
            {
                _currentFilterType = state.Filter.Type;
            }

            if (state.Filter.Value != FilterText.Text)
            {
                FilterText.Text = state.Filter.Value;
            }
        }

        _selectedKey = state.SelectedKey;

        FilterType.SelectId((int)_currentFilterType);

        // set up the records listing panel
        RecordListing.Clear();

        var hasRecords = state.RecordListing != null && state.RecordListing.Count > 0;
        NoRecords.Visible = !hasRecords;
        if (hasRecords)
            PopulateRecordListing(state.RecordListing!);

        // set up the selected person's record
        var selected = _selectedKey != null;

        PersonContainer.Visible = selected;
        RecordUnselected.Visible = !selected;

        _access = _player.LocalPlayer?.ControlledEntity is {} player
            && _accessReader.IsAllowed(player, Console);

        // hide access-required editing parts when no access
        var editing = _access && selected;
        StatusOptionButton.Disabled = !editing;

        if (state.CriminalRecord != null && state.StationRecord != null)
        {
            PopulateRecordContainer(state.StationRecord, state.CriminalRecord);
            OnHistoryUpdated?.Invoke(state.CriminalRecord, _access, false);
            _selectedRecord = state.CriminalRecord;
        }
        else
        {
            _selectedRecord = null;
            OnHistoryClosed?.Invoke();
        }
    }

    private void PopulateRecordListing(Dictionary<uint, string> listing)
    {
        _isPopulating = true;

        foreach (var (key, name) in listing)
        {
            var item = RecordListing.AddItem(name);
            item.Metadata = key;
            item.Selected = key == _selectedKey;
        }
        _isPopulating = false;

        RecordListing.SortItemsByText();
    }

    private void PopulateRecordContainer(GeneralStationRecord stationRecord, CriminalRecord criminalRecord)
    {
        var na = Loc.GetString("generic-not-available-shorthand");
        PersonName.Text = stationRecord.Name;
        PersonPrints.Text = Loc.GetString("general-station-record-console-record-fingerprint", ("fingerprint", stationRecord.Fingerprint ?? na));
        PersonDna.Text = Loc.GetString("general-station-record-console-record-dna", ("dna", stationRecord.DNA ?? na));

        StatusOptionButton.SelectId((int) criminalRecord.Status);
        if (criminalRecord.Reason is {} reason)
        {
            var message = FormattedMessage.FromMarkup(Loc.GetString("criminal-records-console-wanted-reason"));
            message.AddText($": {reason}");
            WantedReason.SetMessage(message);
            WantedReason.Visible = true;
        }
        else
        {
            WantedReason.Visible = false;
        }
    }

    private void AddStatusSelect(SecurityStatus status)
    {
        var name = Loc.GetString($"criminal-records-status-{status.ToString().ToLower()}");
        StatusOptionButton.AddItem(name, (int)status);
    }

    private void FilterListingOfRecords(string text = "")
    {
        if (!_isPopulating)
        {
            OnFiltersChanged?.Invoke(_currentFilterType, text);
        }
    }

    private string GetTypeFilterLocals(StationRecordFilterType type)
    {
        return Loc.GetString($"criminal-records-{type.ToString().ToLower()}-filter");
    }
}
