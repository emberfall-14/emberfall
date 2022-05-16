﻿using Robust.Shared.Serialization;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Networked event from client.
///     Raised on server when client started/stopped typing in chat input field.
/// </summary>
[Serializable, NetSerializable]
public sealed class TypingChangedEvent : EntityEventArgs
{
    public readonly EntityUid Uid;
    public readonly bool IsTyping;

    public TypingChangedEvent(EntityUid uid, bool isTyping)
    {
        Uid = uid;
        IsTyping = isTyping;
    }
}
