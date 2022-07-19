using System.Linq;
using Content.Shared.StationRecords;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.StationRecords;

[GenerateTypedNameReferences]
public sealed partial class GeneralStationRecordConsoleWindow : DefaultWindow
{
    public Action<StationRecordKey?>? OnKeySelected;
    private bool _isPopulating;

    public GeneralStationRecordConsoleWindow()
    {
        RobustXamlLoader.Load(this);

        RecordListing.OnItemSelected += args =>
        {
            if (RecordListing[args.ItemIndex].Metadata is not StationRecordKey cast)
            {
                return;
            }

            OnKeySelected?.Invoke(cast);
        };

        RecordListing.OnItemDeselected += _ =>
        {
            if (!_isPopulating)
                OnKeySelected?.Invoke(null);
        };
    }

    public void UpdateState(GeneralStationRecordConsoleState state)
    {
        if (state.RecordListing == null)
        {
            RecordListingStatus.Visible = true;
            RecordListing.Visible = false;
            RecordListingStatus.Text = Loc.GetString("general-station-record-console-empty-state");
            return;
        }

        RecordListingStatus.Visible = false;
        RecordListing.Visible = true;
        PopulateRecordListing(state.RecordListing!, state.SelectedKey);

        RecordContainerStatus.Visible = state.Record == null;

        if (state.Record != null)
        {
            RecordContainerStatus.Visible = state.SelectedKey == null;
            RecordContainerStatus.Text = state.SelectedKey == null
                ? Loc.GetString("general-station-record-console-no-record-found")
                : Loc.GetString("general-station-record-console-select-record-info");
            PopulateRecordContainer(state.Record);
        }
        else
        {
            RecordContainer.DisposeAllChildren();
            RecordContainer.RemoveAllChildren();
        }
    }

    private void PopulateRecordListing(Dictionary<StationRecordKey, string> listing, StationRecordKey? selected)
    {
        RecordListing.Clear();
        RecordListing.ClearSelected();

        _isPopulating = true;
        foreach (var (key, name) in listing)
        {
            var item = RecordListing.AddItem(name);
            item.Metadata = key;

            if (selected != null && key.ID == selected.Value.ID)
            {
                item.Selected = true;
            }
        }
        _isPopulating = false;

        RecordListing.SortItemsByText();
    }

    private void PopulateRecordContainer(GeneralStationRecord record)
    {
        RecordContainer.DisposeAllChildren();
        RecordContainer.RemoveAllChildren();
        // sure
        var recordControls = new Control[]
        {
            new Label()
            {
                Text = record.Name
            },
            new Label()
            {
                Text = record.Age.ToString()
            },
            new Label()
            {
                Text = Loc.GetString(record.JobTitle)
            },
            new Label()
            {
                Text = record.Species
            },
            new Label()
            {
                Text = record.Gender.ToString()
            }
        };

        foreach (var control in recordControls)
        {
            RecordContainer.AddChild(control);
        }
    }
}
