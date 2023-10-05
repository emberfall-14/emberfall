using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Mind.Components
{
    /// <summary>
    ///     Stores a <see cref="MindComponent"/> on a mob.
    /// </summary>
    [RegisterComponent, Access(typeof(SharedMindSystem))]
    public sealed partial class MindContainerComponent : Component
    {
        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        [DataField]
        [Access(typeof(SharedMindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public EntityUid? Mind;

        /// <summary>
        ///     True if we have a mind, false otherwise.
        /// </summary>
        [ViewVariables]
        [MemberNotNullWhen(true, nameof(Mind))]
        public bool HasMind => Mind != null;

        /// <summary>
        ///     Whether examining should show information about the mind or not.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool ShowExamineInfo;

        /// <summary>
        ///     Whether the mind will be put on a ghost after this component is shutdown.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(SharedMindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public bool GhostOnShutdown = true;
    }

    public sealed class MindRemovedMessage : EntityEventArgs
    {
        public EntityUid OldMindId;
        public MindComponent OldMind;

        public MindRemovedMessage(EntityUid oldMindId, MindComponent oldMind)
        {
            OldMindId = oldMindId;
            OldMind = oldMind;
        }
    }

    public sealed class MindAddedMessage : EntityEventArgs
    {
    }
}
