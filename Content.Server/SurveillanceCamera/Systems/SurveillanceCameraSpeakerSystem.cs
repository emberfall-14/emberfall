using Content.Server.Chat.Systems;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SurveillanceCamera;

/// <summary>
///     This handles speech for surveillance camera monitors.
/// </summary>
public sealed class SurveillanceCameraSpeakerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraSpeakerComponent, SurveillanceCameraSpeechSendEvent>(OnSpeechSent);
        SubscribeLocalEvent<SurveillanceCameraSpeakerComponent, TransformSpeakerNameEvent>(OnTransformSpeech);
    }

    private void OnSpeechSent(EntityUid uid, SurveillanceCameraSpeakerComponent component,
        SurveillanceCameraSpeechSendEvent args)
    {
        if (!component.SpeechEnabled)
        {
            return;
        }

        var time = _gameTiming.CurTime;
        var cd = TimeSpan.FromSeconds(component.SpeechSoundCooldown);

        // this part's mostly copied from speech
        if (time - component.LastSoundPlayed < cd
            && TryComp<SharedSpeechComponent>(args.Speaker, out var speech)
            && speech.SpeechSounds != null
            && _prototypeManager.TryIndex(speech.SpeechSounds, out SpeechSoundsPrototype? speechProto))
        {
            var sound = args.Message[^1] switch
            {
                '?' => speechProto.Value.AskSound,
                '!' => speechProto.Value.ExclaimSound,
                _ => speechProto.Value.SaySound
            };

            var uppercase = 0;
            for (var i = 0; i < args.Message.Length; i++)
            {
                if (char.IsUpper(args.Message[i]))
                {
                    uppercase++;
                }
            }

            if (uppercase > args.Message.Length / 2)
            {
                sound = speechProto.Value.ExclaimSound;
            }

            var scale = (float) _random.NextGaussian(1, speechProto.Value.Variation);
            var param = speech.AudioParams.WithPitchScale(scale);
            _audioSystem.PlayPvs(sound, uid, param);

            component.LastSoundPlayed = time;
        }

        var nameEv = new TransformSpeakerNameEvent(args.Speaker, Name(args.Speaker));
        RaiseLocalEvent(args.Speaker, nameEv);
        component.LastSpokenNames.Enqueue(nameEv.Name);

        _chatSystem.TrySendInGameICMessage(uid, args.Message, InGameICChatType.Speak, false);
    }

    private void OnTransformSpeech(EntityUid uid, SurveillanceCameraSpeakerComponent component,
        TransformSpeakerNameEvent args)
    {
        args.Name = Loc.GetString("surveillance-camera-microphone-message", ("speaker", Name(uid)),
            ("originalName", component.LastSpokenNames.Dequeue()));
    }
}
