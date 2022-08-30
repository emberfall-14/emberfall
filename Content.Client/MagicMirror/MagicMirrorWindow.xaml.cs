using Content.Shared.MagicMirror;
using Content.Shared.Markings;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.MagicMirror;

[GenerateTypedNameReferences]
public sealed partial class MagicMirrorWindow : DefaultWindow
{
    // MMMMMMM
    public Action<(int slot, string id)>? OnHairSelected;
    public Action<(int slot, Marking marking)>? OnHairColorChanged;
    public Action<int>? OnHairSlotRemoved;
    public Action? OnHairSlotAdded;

    public Action<(int slot, string id)>? OnFacialHairSelected;
    public Action<(int slot, Marking marking)>? OnFacialHairColorChanged;
    public Action<int>? OnFacialHairSlotRemoved;
    public Action? OnFacialHairSlotAdded;

    public MagicMirrorWindow()
    {
        RobustXamlLoader.Load(this);

        HairPicker.OnMarkingSelect += OnHairSelected;
        HairPicker.OnColorChanged += OnHairColorChanged;
        HairPicker.OnSlotRemove += OnHairSlotRemoved;
        HairPicker.OnSlotAdd += OnHairSlotAdded;

        FacialHairPicker.OnMarkingSelect += OnFacialHairSelected;
        FacialHairPicker.OnColorChanged += OnFacialHairColorChanged;
        FacialHairPicker.OnSlotRemove += OnFacialHairSlotRemoved;
        FacialHairPicker.OnSlotAdd += OnFacialHairSlotAdded;
    }

    public void UpdateState(MagicMirrorUiData state)
    {
        HairPicker.UpdateData(state.Hair, state.Species, state.HairSlotTotal);
        FacialHairPicker.UpdateData(state.FacialHair, state.Species, state.FacialHairSlotTotal);
    }
}
