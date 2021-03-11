#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;

        private Texture LeftHandTexture { get; }
        private Texture MiddleHandTexture { get; }
        private Texture RightHandTexture { get; }
        private Texture StorageTexture { get; }
        private Texture BlockedTexture { get; }

        private ItemStatusPanel LeftPanel { get; }
        private ItemStatusPanel TopPanel { get; }
        private ItemStatusPanel RightPanel { get; }

        private VBoxContainer HandBox { get; }
        private HBoxContainer HandsContainer { get; }


        [ViewVariables]
        public IReadOnlyList<GuiHand> Hands => _hands;
        private List<GuiHand> _hands = new();

        private int ActiveHand { get; set; }

        public Action<HandClickEventArgs>? HandClick;

        public Action<HandRightClickEventArgs>? HandRightClick;

        public Action<HandActivateEventArgs>? HandActivate;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);
            AddChild(new HBoxContainer
            {
                SeparationOverride = 0,
                Children =
                        {
                            (LeftPanel = ItemStatusPanel.FromSide(HandLocation.Left)),
                            (HandBox = new VBoxContainer
                            {
                                Children =
                                {
                                    (TopPanel = ItemStatusPanel.FromSide(HandLocation.Middle)),
                                    (HandsContainer = new HBoxContainer()),
                                }
                            }),
                            (RightPanel = ItemStatusPanel.FromSide(HandLocation.Right))
                        }
            });
            LeftHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            MiddleHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            RightHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_r.png");
            StorageTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");
            BlockedTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/blocked.png");

            foreach (HandLocation location in Enum.GetValues(typeof(HandLocation)))
            {
                GetStatusPanel(location).Update(null);
            }
        }

        public void SetState(HandsGuiState state)
        {
            ActiveHand = state.ActiveHand;
            _hands = state.GuiHands;
            UpdateGui();
        }

        private void UpdateGui()
        {
            HandsContainer.DisposeAllChildren();

            foreach (var hand in _hands)
            {
                var location = hand.HandLocation;
                var heldItem = hand.HeldItem;

                var newButton = MakeHandButton(location);
                HandsContainer.AddChild(newButton);
                hand.HandButton = newButton;

                var handIndex = _hands.IndexOf(hand);
                newButton.OnPressed += args => OnHandPressed(args, handIndex);
                newButton.OnStoragePressed += args => OnStoragePressed(handIndex);

                newButton.Blocked.Visible = !hand.Enabled;
                GetStatusPanel(location).Update(heldItem);
                _itemSlotManager.SetItemSlot(newButton, heldItem);
            }
            if (TryGetHandButton(ActiveHand, out var handButton))
            {
                handButton.SetActiveHand(true);
            }
            HandleTopPanel();
        }

        private void OnHandPressed(GUIBoundKeyEventArgs pressed, int handIndex)
        {
            
            if (pressed.Function == EngineKeyFunctions.UIClick)
            {
                HandClick?.Invoke(new HandClickEventArgs(handIndex));
            }
            else if (pressed.Function == EngineKeyFunctions.UIRightClick)
            {
                HandRightClick?.Invoke(new HandRightClickEventArgs(handIndex));
            }
        }

        private void OnStoragePressed(int handIndex)
        {
            HandActivate?.Invoke(new HandActivateEventArgs(handIndex));
        }

        private bool TryGetHandButton(int handNumber, [NotNullWhen(true)] out HandButton? handButton)
        {
            handButton = _hands.ElementAtOrDefault(handNumber)?.HandButton;
            return handButton != null;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            foreach (var hand in _hands)
                _itemSlotManager.UpdateCooldown(hand.HandButton, hand.HeldItem);
        }

        private ItemStatusPanel GetStatusPanel(HandLocation handLocation)
        {
            return handLocation switch
            {
                HandLocation.Left => RightPanel, //The player's left hand is the rightmost panel
                HandLocation.Middle => TopPanel,
                HandLocation.Right => LeftPanel, //The player's right hand is the leftmost panel
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private HandButton MakeHandButton(HandLocation buttonLocation)
        {
            var buttonTexture = buttonLocation switch
            {
                HandLocation.Left => LeftHandTexture,
                HandLocation.Middle => MiddleHandTexture,
                HandLocation.Right => RightHandTexture,
                _ => throw new ArgumentOutOfRangeException()
            };
            return new HandButton(buttonTexture, StorageTexture, BlockedTexture, buttonLocation);
        }

        /// <summary>
        ///     Hack to keep invisible top panel from pushing out other panels when there is no middle hand (only 2 hands),
        ///     by making it not attached unless needed.
        /// </summary>
        private void HandleTopPanel()
        {
            if (HandBox.Children.Contains(TopPanel))
            {
                HandBox.RemoveChild(TopPanel);
            }
            foreach (var hand in Hands)
            {
                if (hand.HandLocation == HandLocation.Middle)
                {
                    HandBox.AddChild(TopPanel);
                    break;
                }
            }
        }
    }

    /// <summary>
    ///     Info on a set of hands to be displayed.
    /// </summary>
    public class HandsGuiState
    {
        /// <summary>
        ///     The set of hands to be displayed.
        /// </summary>
        [ViewVariables]
        public List<GuiHand> GuiHands { get; } = new();

        /// <summary>
        ///     The index of the currently active hand. not guranteed to be in bounds of hand list.
        /// </summary>
        [ViewVariables]
        public int ActiveHand { get; }

        public HandsGuiState() { }

        public HandsGuiState(List<GuiHand> guiHands, int activeHand)
        {
            GuiHands = guiHands;
            ActiveHand = activeHand;
        }
    }

    public class HandClickEventArgs
    {
        public int HandClicked { get; }

        public HandClickEventArgs(int handClicked)
        {
            HandClicked = handClicked;
        }
    }

    public class HandRightClickEventArgs
    {
        public int HandClicked { get; }

        public HandRightClickEventArgs(int handClicked)
        {
            HandClicked = handClicked;
        }
    }

    public class HandActivateEventArgs
    {
        public int HandUsed { get; }

        public HandActivateEventArgs(int handUsed)
        {
            HandUsed = handUsed;
        }
    }

    /// <summary>
    ///     Info on an individual hand to be displayed.
    /// </summary>
    public class GuiHand
    {
        /// <summary>
        ///     The name of this hand.
        /// </summary>
        [ViewVariables]
        public string Name { get; }

        /// <summary>
        ///     Where this hand is located.
        /// </summary>
        [ViewVariables]
        public HandLocation HandLocation { get; }

        /// <summary>
        ///     The item being held in this hand.
        /// </summary>
        [ViewVariables]
        public IEntity? HeldItem { get; }

        /// <summary>
        ///     The button in the gui associayted with this hand.
        /// </summary>
        [ViewVariables]
        public HandButton HandButton { get; set; } = default!;

        /// <summary>
        ///     If this hand can be used by the player.
        /// </summary>
        [ViewVariables]
        public bool Enabled { get; }

        public GuiHand(string name, HandLocation handLocation, IEntity? heldItem, bool enabled)
        {
            Name = name;
            HandLocation = handLocation;
            HeldItem = heldItem;
            Enabled = enabled;
        }
    }
}
