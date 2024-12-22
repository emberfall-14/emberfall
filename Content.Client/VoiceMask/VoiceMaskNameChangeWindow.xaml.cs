using Content.Client.UserInterface.Controls;
using Content.Shared.Speech;
using Content.Shared.VoiceMask;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.VoiceMask;

[GenerateTypedNameReferences]
public sealed partial class VoiceMaskNameChangeWindow : FancyWindow
{
    public Action<string>? OnNameChange;
    public Action<string?>? OnVerbChange;
    public Action<string?>? OnSoundChange;

    /// <summary>
    ///     List of all the loaded speech verbs (name, protoID).
    /// </summary>
    private List<(string, string)> _speechVerbs = new();

    /// <summary>
    ///     The currently selected verb.
    /// </summary>
    private string? _selectedVerb;

    /// <summary>
    ///     List of all the loaded speech sounds (name, protoID).
    /// </summary>
    private List<(string, string)> _speechSounds = new();

    /// <summary>
    ///     The currently selected sound.
    /// </summary>
    private string? _selectedSound;

    public VoiceMaskNameChangeWindow()
    {
        RobustXamlLoader.Load(this);

        NameSelectorSet.OnPressed += _ =>
        {
            OnNameChange?.Invoke(NameSelector.Text);
        };

        SpeechVerbSelector.OnItemSelected += args =>
        {
            OnVerbChange?.Invoke((string?)args.Button.GetItemMetadata(args.Id));
            SpeechVerbSelector.SelectId(args.Id);
        };

        SpeechSoundSelector.OnItemSelected += args =>
        {
            OnSoundChange?.Invoke((string?)args.Button.GetItemMetadata(args.Id));
            SpeechSoundSelector.SelectId(args.Id);
        };
    }

    /// <summary>
    ///     Loads the prototypes into their respective lists. DOES NOT make any UI changes, just making the lists.
    /// </summary>
    public void ReloadVerbsAndNoises(IPrototypeManager proto)
    {
        // Before you ask, yes, this is currently the clearest way of doing this...
        // You could do some spooky inheritance shenanigans with the prototypes but it would be kind of weird.
        foreach (var verb in proto.EnumeratePrototypes<SpeechVerbPrototype>())
        {
            _speechVerbs.Add((Loc.GetString(verb.Name), verb.ID));
        }
        _speechVerbs.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        foreach (var noise in proto.EnumeratePrototypes<SpeechSoundsPrototype>())
        {
            _speechSounds.Add((Loc.GetString(noise.Name), noise.ID));
        }
        _speechSounds.Sort((a, b) => a.Item1.CompareTo(b.Item1));
    }

    /// <summary>
    ///     Populates all the buttons with the values stored in the lists.
    /// </summary>
    public void AddVerbsAndSounds()
    {
        PopulateOptionButton(SpeechVerbSelector, _selectedVerb, _speechVerbs);
        PopulateOptionButton(SpeechSoundSelector, _selectedSound, _speechSounds);
    }

    /// <summary>
    ///     Updates the window to the current state. E.g make sure the shown name is the actual name being used etc..
    /// </summary>
    public void UpdateState(VoiceMaskBuiState voiceMaskBuiState)
    {
        NameSelector.Text = voiceMaskBuiState.Name;

        _selectedVerb = voiceMaskBuiState.Verb;
        _selectedSound = voiceMaskBuiState.Sound;

        UpdateSelectedButtonOption(SpeechVerbSelector, _selectedVerb);
        UpdateSelectedButtonOption(SpeechSoundSelector, _selectedSound);
    }

    #region Helper functions

    /// <summary>
    ///     Populates a button with the given list of names / ids. Also adds a "default" button with null as the id.
    /// </summary>
    private void PopulateOptionButton(OptionButton button, string? selectedOption, List<(string, string)> values)
    {
        button.Clear();

        // Add the default option that wont do anything when selected.
        AddOption(button, selectedOption, Loc.GetString("chat-speech-verb-name-none"), null);
        foreach (var (name, id) in values)
        {
            AddOption(button, selectedOption, name, id);
        }
    }

    /// <summary>
    ///     Actually adds the option to the drop down menu. If the given value matches the selected value, it will also
    ///     make the drop down menu display it as the currently selected value.
    /// </summary>
    private void AddOption(OptionButton button, string? selectedValue, string name, string? value)
    {
        var id = button.ItemCount;
        button.AddItem(name);
        if (value is { } metadata)
            button.SetItemMetadata(id, metadata);

        if (value == selectedValue)
            button.SelectId(id);
    }
    /// <summary>
    ///     Updates the currently selected item in the drop down to the given value.
    /// </summary>
    private void UpdateSelectedButtonOption(OptionButton button, string? selected)
    {
        for (var id = 0; id < button.ItemCount; id++)
        {
            if (string.Equals(selected, button.GetItemMetadata(id)))
            {
                button.SelectId(id);
                break;
            }
        }
    }
    #endregion
}
