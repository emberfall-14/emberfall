﻿using Content.Shared.GameObjects.Components.Body.Behavior;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SharedLungSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(SharedMetabolismSystem));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var lung in ComponentManager.EntityQuery<SharedLungBehaviorComponent>())
            {
                lung.Update(frameTime);
            }
        }
    }
}
