// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Commands;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.SS220.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private ISawmill _sawmill = default!;

    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private float _volume = 0.0f;
    private float _radioVolume = 0.0f;
    private int _fileIdx = 0;

    private const int MaxQueuedPerEntity = 20;
    private const int MaxEntitiesQueued = 30;
    private readonly Dictionary<EntityUid, Queue<PlayRequest>> _playQueues = new();
    private readonly Dictionary<EntityUid, AudioSystem.PlayingStream> _playingStreams = new();

    private EntityUid _fakeRecipient = new();

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _resourceCache.AddRoot(Prefix, _contentRoot);

        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<TtsQueueResetMessage>(OnQueueResetRequest);

        InitializeAnnounces();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _contentRoot.Dispose();

        ShutdownAnnounces();
        ResetQueuesAndEndStreams();
    }

    public void RequestGlobalTTS(string text, string voiceId)
    {
        RaiseNetworkEvent(new RequestGlobalTTSEvent(text, voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnQueueResetRequest(TtsQueueResetMessage ev)
    {
        ResetQueuesAndEndStreams();
        _sawmill.Debug("TTS queue was cleared by request from the server.");
    }

    public void ResetQueuesAndEndStreams()
    {
        foreach (var (_, stream) in _playingStreams)
        {
            stream.Stop();
        }

        _playingStreams.Clear();
        _playQueues.Clear();
    }

    // Process sound queues on frame update
    public override void FrameUpdate(float frameTime)
    {
        var streamsToRemove = new List<EntityUid>();

        foreach (var (uid, stream) in _playingStreams)
        {
            if (stream.Done)
                streamsToRemove.Add(uid);
        }

        foreach (var uid in streamsToRemove)
        {
            _playingStreams.Remove(uid);
        }

        var queueUidsToRemove = new List<EntityUid>();

        foreach (var (uid, queue) in _playQueues)
        {
            if (_playingStreams.ContainsKey(uid))
                continue;

            if (!queue.TryDequeue(out var request))
                continue;

            if (queue.Count == 0)
                queueUidsToRemove.Add(uid);

            ResPath? tempFilePath = null;
            SoundPathSpecifier soundPath;
            if (request is PlayRequestById requestById)
            {
                tempFilePath = new ResPath($"{requestById.FileIdx}.ogg");
                soundPath = new SoundPathSpecifier(Prefix / tempFilePath.Value, requestById.Params);
            }
            else if (request is PlayRequestByPath requestByPath)
            {
                soundPath = new SoundPathSpecifier(requestByPath.Path, requestByPath.Params);
            }
            else
                continue;

            IPlayingAudioStream? stream;
            if (request.PlayGlobal)
                stream = _audio.PlayGlobal(soundPath, Filter.Local(), false);
            else
                stream = _audio.PlayEntity(soundPath, _fakeRecipient, uid);

            if (stream is AudioSystem.PlayingStream playingStream)
                _playingStreams.Add(uid, playingStream);

            if (tempFilePath.HasValue)
                _contentRoot.RemoveFile(tempFilePath.Value);
        }

        foreach (var queueUid in queueUidsToRemove)
        {
            _playQueues.Remove(queueUid);
        }
    }

    public void TryQueueRequest(EntityUid entity, PlayRequest request)
    {
        if (!_playQueues.TryGetValue(entity, out var queue))
        {
            if (_playQueues.Count >= MaxEntitiesQueued)
                return;

            queue = new();
            _playQueues.Add(entity, queue);
        }

        if (queue.Count >= MaxQueuedPerEntity)
            return;

        queue.Enqueue(request);
    }

    public void TryQueuePlayById(EntityUid entity, int fileIdx, AudioParams audioParams, bool globally = false)
    {
        var request = new PlayRequestById(fileIdx, audioParams, globally);
        TryQueueRequest(entity, request);
    }

    private void PlaySoundQueued(EntityUid entity, ResPath sound, AudioParams? audioParams = null, bool globally = false)
    {
        var request = new PlayRequestByPath(sound, audioParams, globally);
        TryQueueRequest(entity, request);
    }

    private void PlayTTSBytes(byte[] data, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        _sawmill.Debug($"Play TTS audio {data.Length} bytes from {sourceUid} entity");

        var finalParams = audioParams ?? AudioParams.Default;

        var filePath = new ResPath($"{_fileIdx}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, data);

        if (sourceUid == null)
        {
            var soundPath = new SoundPathSpecifier(Prefix / filePath, finalParams);
            _audio.PlayGlobal(soundPath, Filter.Local(), false);
            _contentRoot.RemoveFile(filePath);
        }
        else
        {
            if (sourceUid.HasValue && sourceUid.Value.IsValid())
                TryQueuePlayById(sourceUid.Value, _fileIdx, finalParams, globally);
        }

        _fileIdx++;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = (ev.IsRadio ? _radioVolume : _volume) * ev.VolumeModifier;
        var audioParams = AudioParams.Default.WithVolume(volume);

        PlayTTSBytes(ev.Data, GetEntity(ev.SourceUid), audioParams);
    }

    // Play requests //
    public abstract class PlayRequest
    {
        public readonly AudioParams Params = AudioParams.Default;
        public readonly bool PlayGlobal = false;

        public PlayRequest(AudioParams? audioParams = null, bool playGlobal = false)
        {
            PlayGlobal = playGlobal;
            if (audioParams.HasValue)
                Params = audioParams.Value;
        }
    }

    public sealed class PlayRequestByPath : PlayRequest
    {
        public readonly ResPath Path;

        public PlayRequestByPath(ResPath path, AudioParams? audioParams = null, bool playGlobal = false) : base(audioParams, playGlobal)
        {
            Path = path;
        }
    }

    public sealed class PlayRequestById : PlayRequest
    {
        public readonly int FileIdx = 0;

        public PlayRequestById(int fileIdx, AudioParams? audioParams = null, bool playGlobal = false) : base(audioParams, playGlobal)
        {
            FileIdx = fileIdx;
        }
    }
}
