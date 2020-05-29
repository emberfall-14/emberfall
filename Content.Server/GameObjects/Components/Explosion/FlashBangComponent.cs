using Content.Server.GameObjects.Components.Weapon;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Explosion
{
    /// <summary>
    /// When triggered will flash in an area around the object and destroy itself
    /// </summary>
    [RegisterComponent]
    public class FlashBangComponent : Component, ITimerTrigger, IDestroyAct
    {
        public override string Name => "FlashBang";

        private float _range;
        private double _duration;
        private string _sound;
        private bool _deleteOnFlash;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", 7.0f);
            serializer.DataField(ref _duration, "duration", 8.0);
            serializer.DataField(ref _sound, "sound", "/Audio/effects/flash_bang.ogg");
            serializer.DataField(ref _deleteOnFlash, "deleteOnFlash", true);
        }

        public bool Explode()
        {
            // If we're in a locker or whatever then can't flash anything
            ContainerHelpers.TryGetContainer(Owner, out var container);
            if (container == null || !container.Owner.HasComponent<EntityStorageComponent>())
            {
                ServerFlashableComponent.FlashAreaHelper(Owner, _range, _duration);
            }

            if (_sound != null)
            {
                var soundSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>();
                soundSystem.Play(_sound);
            }

            if (_deleteOnFlash && !Owner.Deleted)
            {
                Owner.Delete();
            }
            
            return true;
        }

        bool ITimerTrigger.Trigger(TimerTriggerEventArgs eventArgs)
        {
            return Explode();
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            Explode();
        }
    }
}
