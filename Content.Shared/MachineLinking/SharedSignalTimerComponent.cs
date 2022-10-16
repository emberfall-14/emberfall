using Content.Shared.Disposal.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.MachineLinking
{
    public abstract class SharedSignalTimerComponent : Component
    {

    }

    [Serializable, NetSerializable]
    public enum SignalTimerUiKey
    {
        Key
    }

    /// <summary>
    /// Represents an <see cref="SignalTimerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class SignalTimerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentText { get; }
        public string CurrentDelayMinutes { get; }
        public string CurrentDelaySeconds { get; }
        public bool ShowText { get; }
        public TimeSpan TriggerTime { get; }
        public bool TimerStarted { get;  }
        public bool? HasAccess { get; }
        public SignalTimerBoundUserInterfaceState(string currentText, string currentDelayMinutes, string currentDelaySeconds, bool showText, TimeSpan triggerTime, bool timerStarted, bool? hasAccess)
        {
            CurrentText = currentText;
            CurrentDelayMinutes = currentDelayMinutes;
            CurrentDelaySeconds = currentDelaySeconds;
            ShowText = showText;
            TriggerTime = triggerTime;
            TimerStarted = timerStarted;
            HasAccess = hasAccess;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalTimerTextChangedMessage : BoundUserInterfaceMessage
    {
        public string Text { get; }

        public SignalTimerTextChangedMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalTimerDelayChangedMessage : BoundUserInterfaceMessage
    {
        public TimeSpan Delay { get; }
        public SignalTimerDelayChangedMessage(TimeSpan delay)
        {
            Delay = delay;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalTimerStartMessage : BoundUserInterfaceMessage
    {
        public EntityUid User { get; }
        public SignalTimerStartMessage(EntityUid user)
        {
            User = user;
        }
    }
}
