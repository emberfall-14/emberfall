using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Administration.Managers;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Headset
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
#pragma warning disable 618
    public sealed class HeadsetComponent : Component, IListen, IRadio, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        [Dependency] private readonly IAdminManager _adminManager = default!;

        private RadioSystem _radioSystem = default!;

        [DataField("channels")]
        private List<int> _channels = new(){1459};

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        private int BroadcastFrequency { get; set; } = 1459;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; }

        public IReadOnlyList<int> Channels => _channels;

        public bool RadioRequested { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
        }

        public bool CanListen(string message, EntityUid source)
        {
            return RadioRequested;
        }

        public void Receive(string message, int channel, string speakerVoice, string adminVoice)
        {
            if (Owner.TryGetContainer(out var container))
            {
                if (!_entMan.TryGetComponent(container.Owner, out ActorComponent? actor))
                    return;

                var playerChannel = actor.PlayerSession.ConnectedClient;

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                //Square brackets are added here to avoid issues with escaping
                if (!_adminManager.IsAdmin(actor.PlayerSession))
                {
                    msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", speakerVoice));
                } else
                {
                    msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", adminVoice));

                }
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public void Listen(string message, EntityUid speaker)
        {
            Broadcast(message, speaker);
        }

        public void Broadcast(string message, EntityUid speaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, BroadcastFrequency);
            RadioRequested = false;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddText(Loc.GetString("examine-radio-frequency", ("frequency", BroadcastFrequency)));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset"));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
        }
    }
}
