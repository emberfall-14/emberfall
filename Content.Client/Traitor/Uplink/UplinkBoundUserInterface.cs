using Content.Client.Examine;
using Content.Shared.PDA;
using Content.Shared.Traitor.Uplink;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Traitor.Uplink
{
    [UsedImplicitly]
    public class UplinkBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        private UplinkMenu? _menu;
        private UplinkMenuPopup? _failPopup;

        public UplinkBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = new UplinkMenu(this, _prototypeManager);
            _menu.OpenCentered();
            _menu.OnClose += Close;

            _menu.OnListingButtonPressed += (_, listing) =>
            {
                if (_menu.CurrentLoggedInAccount?.DataBalance < listing.Price)
                {
                    _failPopup = new UplinkMenuPopup(Loc.GetString("pda-bound-user-interface-insufficient-funds-popup"));
                    _userInterfaceManager.ModalRoot.AddChild(_failPopup);
                    _failPopup.Open(UIBox2.FromDimensions(_menu.Position.X + 150, _menu.Position.Y + 60, 156, 24));
                    _menu.OnClose += () =>
                    {
                        _failPopup.Dispose();
                    };
                }

                SendMessage(new PDAUplinkBuyListingMessage(listing.ItemId));
            };

            _menu.OnCategoryButtonPressed += (_, category) =>
            {
                _menu.CurrentFilterCategory = category;
                SendMessage(new PDARequestUpdateInterfaceMessage());

            };
        }

        /// <summary>
        /// This is shitcode. It is, however, "PJB-approved shitcode".
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Color GetWeightedColor(int x)
        {
            var weightedColor = Color.Gray;
            if (x <= 0)
            {
                return weightedColor;
            }
            if (x <= 5)
            {
                weightedColor = Color.Green;
            }
            else if (x > 5 && x < 10)
            {
                weightedColor = Color.Yellow;
            }
            else if (x > 10 && x <= 20)
            {
                weightedColor = Color.Orange;
            }
            else if (x > 20 && x <= 50)
            {
                weightedColor = Color.Purple;
            }

            return weightedColor;
        }

        public sealed class UplinkMenuPopup : Popup
        {
            public UplinkMenuPopup(string text)
            {
                var label = new RichTextLabel();
                label.SetMessage(text);
                AddChild(new PanelContainer
                {
                    StyleClasses = { ExamineSystem.StyleClassEntityTooltip },
                    Children = { label }
                });
            }
        }

        private class UplinkMenu : SS14Window
        {
            public BoxContainer UplinkTabContainer { get; }

            protected readonly HSplitContainer CategoryAndListingsContainer;

            private readonly IPrototypeManager _prototypeManager;

            public readonly BoxContainer UplinkListingsContainer;

            public readonly BoxContainer CategoryListContainer;
            public readonly RichTextLabel BalanceInfo;
            public event Action<ButtonEventArgs, UplinkListingData>? OnListingButtonPressed;
            public event Action<ButtonEventArgs, UplinkCategory>? OnCategoryButtonPressed;

            public UplinkMenu(UplinkBoundUserInterface owner, IPrototypeManager prototypeManager)
            {
                _prototypeManager = prototypeManager;

                //Uplink Tab
                CategoryListContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical
                };

                BalanceInfo = new RichTextLabel
                {
                    HorizontalAlignment = HAlignment.Center,
                };

                //Red background container.
                var masterPanelContainer = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Black },
                    VerticalExpand = true
                };

                //This contains both the panel of the category buttons and the listings box.
                CategoryAndListingsContainer = new HSplitContainer
                {
                    VerticalExpand = true,
                };


                var uplinkShopScrollContainer = new ScrollContainer
                {
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    SizeFlagsStretchRatio = 2,
                    MinSize = (100, 256)
                };

                //Add the category list to the left side. The store items to center.
                var categoryListContainerBackground = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Gray.WithAlpha(0.02f) },
                    VerticalExpand = true,
                    Children =
                    {
                        CategoryListContainer
                    }
                };

                CategoryAndListingsContainer.AddChild(categoryListContainerBackground);
                CategoryAndListingsContainer.AddChild(uplinkShopScrollContainer);
                masterPanelContainer.AddChild(CategoryAndListingsContainer);

                //Actual list of buttons for buying a listing from the uplink.
                UplinkListingsContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    SizeFlagsStretchRatio = 2,
                    MinSize = (100, 256),
                };
                uplinkShopScrollContainer.AddChild(UplinkListingsContainer);

                var innerVboxContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    VerticalExpand = true,

                    Children =
                    {
                        BalanceInfo,
                        masterPanelContainer
                    }
                };

                UplinkTabContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        innerVboxContainer
                    }
                };
                PopulateUplinkCategoryButtons();
            }

            public UplinkCategory CurrentFilterCategory
            {
                get => _currentFilter;
                set
                {
                    if (value.GetType() != typeof(UplinkCategory))
                    {
                        return;
                    }

                    _currentFilter = value;
                }
            }

            public UplinkAccountData? CurrentLoggedInAccount
            {
                get => _loggedInUplinkAccount;
                set => _loggedInUplinkAccount = value;
            }

            private UplinkCategory _currentFilter;
            private UplinkAccountData? _loggedInUplinkAccount;

            public void AddListingGui(UplinkListingData listing)
            {
                if (!_prototypeManager.TryIndex(listing.ItemId, out EntityPrototype? prototype) || listing.Category != CurrentFilterCategory)
                {
                    return;
                }
                var weightedColor = GetWeightedColor(listing.Price);
                var itemLabel = new Label
                {
                    Text = listing.ListingName == string.Empty ? prototype.Name : listing.ListingName,
                    ToolTip = listing.Description == string.Empty ? prototype.Description : listing.Description,
                    HorizontalExpand = true,
                    Modulate = _loggedInUplinkAccount?.DataBalance >= listing.Price
                    ? Color.White
                    : Color.Gray.WithAlpha(0.30f)
                };

                var priceLabel = new Label
                {
                    Text = $"{listing.Price} TC",
                    HorizontalAlignment = HAlignment.Right,
                    Modulate = _loggedInUplinkAccount?.DataBalance >= listing.Price
                    ? weightedColor
                    : Color.Gray.WithAlpha(0.30f)
                };

                //Padding for the price lable.
                var pricePadding = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    MinSize = (32, 1),
                };

                //Contains the name of the item and its price. Used for spacing item name and price.
                var listingButtonHbox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        itemLabel,
                        priceLabel,
                        pricePadding
                    }
                };

                var listingButtonPanelContainer = new PanelContainer
                {
                    Children =
                    {
                        listingButtonHbox
                    }
                };

                var pdaUplinkListingButton = new PDAUplinkItemButton(listing)
                {
                    Children =
                    {
                        listingButtonPanelContainer
                    }
                };
                pdaUplinkListingButton.OnPressed += args
                    => OnListingButtonPressed?.Invoke(args, pdaUplinkListingButton.ButtonListing);
                UplinkListingsContainer.AddChild(pdaUplinkListingButton);
            }

            public void ClearListings()
            {
                UplinkListingsContainer.Children.Clear();
            }

            private void PopulateUplinkCategoryButtons()
            {

                foreach (UplinkCategory cat in Enum.GetValues(typeof(UplinkCategory)))
                {

                    var catButton = new PDAUplinkCategoryButton
                    {
                        Text = Loc.GetString(cat.ToString()),
                        ButtonCategory = cat

                    };
                    //It'd be neat if it could play a cool tech ping sound when you switch categories,
                    //but right now there doesn't seem to be an easy way to do client-side audio without still having to round trip to the server and
                    //send to a specific client INetChannel.
                    catButton.OnPressed += args => OnCategoryButtonPressed?.Invoke(args, catButton.ButtonCategory);

                    CategoryListContainer.AddChild(catButton);
                }

            }

            private sealed class PDAUplinkItemButton : ContainerButton
            {
                public PDAUplinkItemButton(UplinkListingData data)
                {
                    ButtonListing = data;
                }

                public UplinkListingData ButtonListing { get; }
            }

            private sealed class PDAUplinkCategoryButton : Button
            {
                public UplinkCategory ButtonCategory;

            }
        }    
    }
}
