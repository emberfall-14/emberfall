using System;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class HungerUI : SharedHungerComponent
    {
        private StatusEffectsUI _ui;

        private TextureRect _hungerStatusRect;

#pragma warning disable 649
        // Required dependencies
        [Dependency] private readonly IResourceCache _resourceCache;
#pragma warning restore 649

        public override void OnRemove()
        {
            base.OnRemove();

            _ui?.VBox?.RemoveChild(_hungerStatusRect);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out SpeciesUI speciesUi)) {
                _ui = speciesUi.UI;
                _ui.VBox.AddChild(_hungerStatusRect = new TextureRect
                {
                    TextureScale = (2, 2),
                    Texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Mob/UI/Hunger/Okay.png")
                });
            }
            else
            {
                // TODO: If this happens it's likely on some rando mob which shouldn't need a UI right...?
                throw new NullReferenceException();
            }

        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case HungerStateMessage msg:
                    Logger.InfoS("hunger", $"Received hunger state change to {msg.Threshold}");
                    ChangeHudIcon(msg);
                    break;

            }
        }

        private void ChangeHudIcon(HungerStateMessage hungerStateMessage)
        {
            // TODO: Use RSIs?
            // TODO: Add test check if pngs / rsi states exist for all of these
            var path = SharedSpriteComponent.TextureRoot / "Mob" / "UI" / "Hunger" / hungerStateMessage.Threshold.ToString() + ".png";
            var texture = _resourceCache.GetTexture(path);

            _hungerStatusRect.Texture = texture;
        }
    }
}
