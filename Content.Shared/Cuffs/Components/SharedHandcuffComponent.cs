﻿#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    public abstract class SharedHandcuffComponent : Component
    {
        public override string Name => "Handcuff";
        public override uint? NetID => ContentNetIDs.HANDCUFFS;

        [Serializable, NetSerializable]
        protected sealed class HandcuffedComponentState : ComponentState
        {
            public string? IconState { get; }

            public HandcuffedComponentState(string? iconState)
            {
                IconState = iconState;
            }
        }
    }
}
