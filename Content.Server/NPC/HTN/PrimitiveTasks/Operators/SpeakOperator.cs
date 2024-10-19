using Content.Server.Chat.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SpeakOperator : HTNOperator
{
    private ChatSystem _chat = default!;

    [DataField]
    public string Speech = string.Empty;

    [DataField]
    public string SpeechRaw = string.Empty;

    /// <summary>
    /// Whether to hide message from chat window and logs.
    /// </summary>
    [DataField]
    public bool Hidden;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _chat = sysManager.GetEntitySystem<ChatSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if(SpeechRaw != string.Empty)
        _chat.TrySendInGameICMessage(speaker, SpeechRaw, InGameICChatType.Speak, hideChat: Hidden, hideLog: Hidden);
        else
        _chat.TrySendInGameICMessage(speaker, Loc.GetString(Speech), InGameICChatType.Speak, hideChat: Hidden, hideLog: Hidden);

        return base.Update(blackboard, frameTime);
    }
}
