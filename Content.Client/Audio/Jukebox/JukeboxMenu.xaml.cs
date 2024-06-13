using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Audio.Components;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using FancyWindow = Content.Client.UserInterface.Controls.FancyWindow;

namespace Content.Client.Audio.Jukebox;

[GenerateTypedNameReferences]
public sealed partial class JukeboxMenu : FancyWindow
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private AudioSystem _audioSystem;

    /// <summary>
    /// True if playing, false if paused.
    /// </summary>
    public event Action<bool>? OnPlayPressed;
    public event Action? OnStopPressed;
    public event Action<ProtoId<JukeboxPrototype>>? OnSongSelected;
    public event Action<ProtoId<JukeboxPrototype>>? OnSongQueueAdd;
    public event Action<int>? OnQueueRemove;
    public event Action<float>? SetTime;

    private EntityUid? _audio;

    private float _lockTimer;

    public JukeboxMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _audioSystem = _entManager.System<AudioSystem>();

        CurrentSong.SetOnPressedStop(args =>
        {
            OnStopPressed?.Invoke();
        });
        CurrentSong.SetOnPressedPlay((song, playPauseState, args) =>
        {
            OnPlayPressed?.Invoke(playPauseState);
        });
        PlaybackSlider.OnReleased += PlaybackSliderKeyUp;
    }

    public JukeboxMenu(AudioSystem audioSystem)
    {
        _audioSystem = audioSystem;
    }

    public void SetAudioStream(EntityUid? audio)
    {
        _audio = audio;
    }

    private void PlaybackSliderKeyUp(Slider args)
    {
        SetTime?.Invoke(PlaybackSlider.Value);
        _lockTimer = 0.5f;
    }

    /// <summary>
    /// Re-populates the list of jukebox prototypes available.
    /// </summary>
    public void Populate(IEnumerable<JukeboxPrototype> jukeboxProtos)
    {
        foreach (var entry in jukeboxProtos)
        {
            // MusicList.AddItem(entry.Name, metadata: entry.ID);
            var songControl = new JukeboxEntry(entry) {EntryType = JukeboxEntry.Type.List};
            songControl.SetOnPressedPlay((song, _, args) => {
                if (song == null)
                    return;
                OnSongSelected?.Invoke(song.ID);
                OnPlayPressed?.Invoke(true);
            });

            songControl.SetOnPressedQueue((song, args) => {
                if (song == null)
                    return;


                OnSongQueueAdd?.Invoke(song.ID);
            });

            MusicList.AddChild(songControl);
        }
    }

    public void SetSelectedSong(ProtoId<JukeboxPrototype>? song, float length)
    {
        if (song == null)
            return;

        if (!_prototype.TryIndex(song, out var songProto))
            return;

        CurrentSong.SetSong(songProto);
        PlaybackSlider.MaxValue = length;
        PlaybackSlider.SetValueWithoutEvent(0);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_lockTimer > 0f)
        {
            _lockTimer -= args.DeltaSeconds;
        }

        PlaybackSlider.Disabled = _lockTimer > 0f;

        if (_entManager.TryGetComponent(_audio, out AudioComponent? audio))
        {
            DurationLabel.Text = $@"{TimeSpan.FromSeconds(audio.PlaybackPosition):mm\:ss} / {_audioSystem.GetAudioLength(audio.FileName):mm\:ss}";
        }
        else
        {
            DurationLabel.Text = $"00:00 / 00:00";
        }

        if (PlaybackSlider.Grabbed)
            return;

        if (audio != null || _entManager.TryGetComponent(_audio, out audio))
        {
            PlaybackSlider.SetValueWithoutEvent(audio.PlaybackPosition);
        }
        else
        {
            PlaybackSlider.SetValueWithoutEvent(0f);
        }
    }

    public void PopulateQueue(List<ProtoId<JukeboxPrototype>> queue)
    {
        MusicListQueue.RemoveAllChildren();
        int i = 0;
        foreach (var song in queue)
        {
            if (!_prototype.TryIndex(song, out var songProto))
                continue;

            i += 1;
            var songControl = new JukeboxEntry(songProto) {EntryType = JukeboxEntry.Type.Queue};
            MusicListQueue.AddChild(songControl);

            songControl.SetOnPressedRemove((source, args) => {
                int index = source.GetPositionInParent();

                OnQueueRemove?.Invoke(index);
            });
        }
    }
}
