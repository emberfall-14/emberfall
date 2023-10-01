using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Decals;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Decals.UI;

[GenerateTypedNameReferences]
public sealed partial class DecalPlacerWindow : DefaultWindow
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly DecalPlacementSystem _decalPlacementSystem;

    public FloatSpinBox RotationSpinBox;

    private PaletteColorPicker? _picker;

    private SortedDictionary<string, Texture>? _decals;
    private string? _selected;
    private Color _color = Color.White;
    private bool _useColor;
    private bool _snap;
    private float _rotation;
    private bool _cleanable;
    private int _zIndex;

    private bool _auto;

    public DecalPlacerWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _decalPlacementSystem = EntitySystem.Get<DecalPlacementSystem>();

        // This needs to be done in C# so we can have custom stuff passed in the constructor
        // and thus have a proper step size
        RotationSpinBox = new FloatSpinBox(90.0f, 0)
        {
            HorizontalExpand = true
        };
        SpinBoxContainer.AddChild(RotationSpinBox);

        Search.OnTextChanged += _ => RefreshList();
        ColorPicker.OnColorChanged += OnColorPicked;

        PickerOpen.OnPressed += _ =>
        {
            if (_picker is null)
            {
                _picker = new PaletteColorPicker();
                _picker.OpenToLeft();
                _picker.PaletteList.OnItemSelected += args =>
                {
                    var color = (args.ItemList.GetSelected().First().Metadata as Color?)!.Value;
                    ColorPicker.Color = color;
                    OnColorPicked(color);
                };
            }
            else
            {
                if (_picker.IsOpen)
                {
                    _picker.Close();
                }
                else
                {
                    _picker.Open();
                }
            }
        };

        RotationSpinBox.OnValueChanged += args =>
        {
            _rotation = args.Value;
            UpdateDecalPlacementInfo();
        };
        EnableAuto.OnToggled += args =>
        {
            _auto = args.Pressed;
            if (_selected != null)
                SelectDecal(_selected);
        };
        EnableColor.OnToggled += args =>
        {
            _useColor = args.Pressed;
            UpdateDecalPlacementInfo();
            RefreshList();
        };
        EnableSnap.OnToggled += args =>
        {
            _snap = args.Pressed;
            UpdateDecalPlacementInfo();
        };
        EnableCleanable.OnToggled += args =>
        {
            _cleanable = args.Pressed;
            UpdateDecalPlacementInfo();
        };
        ZIndexSpinBox.ValueChanged += args =>
        {
            _zIndex = args.Value;
            UpdateDecalPlacementInfo();
        };
    }

    private void OnColorPicked(Color color)
    {
        _color = color;
        UpdateDecalPlacementInfo();
        RefreshList();
    }

    private void UpdateDecalPlacementInfo()
    {
        if (_selected is null)
            return;

        var color = _useColor ? _color : Color.White;
        _decalPlacementSystem.UpdateDecalInfo(_selected, color, _rotation, _snap, _zIndex, _cleanable);
    }

    private void RefreshList()
    {
        // Clear
        Grid.RemoveAllChildren();
        if (_decals == null)
            return;

        var filter = Search.Text;
        foreach (var (decal, tex) in _decals)
        {
            if (!decal.ToLowerInvariant().Contains(filter.ToLowerInvariant()))
                continue;

            var button = new TextureButton
            {
                TextureNormal = tex,
                Name = decal,
                ToolTip = decal,
                Modulate = _useColor ? _color : Color.White
            };
            button.OnPressed += ButtonOnPressed;
            if (_selected == decal)
            {
                var panelContainer = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = StyleNano.ButtonColorDefault
                    },
                    Children =
                    {
                        button
                    }
                };
                Grid.AddChild(panelContainer);
            }
            else
                Grid.AddChild(button);
        }
    }

    private void ButtonOnPressed(ButtonEventArgs obj)
    {
        if (obj.Button.Name == null)
            return;

        SelectDecal(obj.Button.Name);
    }

    private void SelectDecal(string decalId)
    {
        if (!_prototype.TryIndex<DecalPrototype>(decalId, out var decal))
            return;

        _selected = decalId;

        if (_auto)
        {
            EnableCleanable.Pressed = decal.DefaultCleanable;
            EnableColor.Pressed = decal.DefaultCustomColor;
            EnableSnap.Pressed = decal.DefaultSnap;
            _cleanable = decal.DefaultCleanable;
            _useColor = decal.DefaultCustomColor;
            _snap = decal.DefaultSnap;
        }
        UpdateDecalPlacementInfo();
        RefreshList();
    }

    public void Populate(IEnumerable<DecalPrototype> prototypes)
    {
        _decals = new SortedDictionary<string, Texture>();
        foreach (var decalPrototype in prototypes)
        {
            if (decalPrototype.ShowMenu)
                _decals.Add(decalPrototype.ID, decalPrototype.Sprite.Frame0());
        }

        RefreshList();
    }

    protected override void Opened()
    {
        base.Opened();
        _decalPlacementSystem.SetActive(true);
    }

    public override void Close()
    {
        base.Close();
        _decalPlacementSystem.SetActive(false);
    }
}
