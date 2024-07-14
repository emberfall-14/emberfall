using System.Linq;
using Content.Client.Actions;
using Content.Client.Message;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Store.Ui;

[GenerateTypedNameReferences]
public sealed partial class StoreMenu : DefaultWindow
{
    private const string DiscountedCategoryPrototypeKey = "DiscountedItems";

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private StoreWithdrawWindow? _withdrawWindow;

    public event EventHandler<string>? SearchTextUpdated;
    public event Action<BaseButton.ButtonEventArgs, ListingData>? OnListingButtonPressed;
    public event Action<BaseButton.ButtonEventArgs, string>? OnCategoryButtonPressed;
    public event Action<BaseButton.ButtonEventArgs, string, int>? OnWithdrawAttempt;
    public event Action<BaseButton.ButtonEventArgs>? OnRefundAttempt;

    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Balance = new();
    public string CurrentCategory = string.Empty;

    private List<ListingData> _cachedListings = new();
    private List<StoreDiscountData> _cachedDiscounts = new();

    public StoreMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        WithdrawButton.OnButtonDown += OnWithdrawButtonDown;
        RefundButton.OnButtonDown += OnRefundButtonDown;
        SearchBar.OnTextChanged += _ => SearchTextUpdated?.Invoke(this, SearchBar.Text);
    }

    public void UpdateBalance(Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> balance)
    {
        Balance = balance;

        var currency = balance.ToDictionary(type =>
            (type.Key, type.Value), type => _prototypeManager.Index(type.Key));

        var balanceStr = string.Empty;
        foreach (var ((_, amount), proto) in currency)
        {
            balanceStr += Loc.GetString("store-ui-balance-display", ("amount", amount),
                ("currency", Loc.GetString(proto.DisplayName, ("amount", 1))));
        }

        BalanceInfo.SetMarkup(balanceStr.TrimEnd());

        var disabled = true;
        foreach (var type in currency)
        {
            if (type.Value.CanWithdraw && type.Value.Cash != null && type.Key.Item2 > 0)
                disabled = false;
        }

        WithdrawButton.Disabled = disabled;
    }

    public void UpdateListing(List<ListingData> listings, List<StoreDiscountData> discounts)
    {
        _cachedListings = listings;
        _cachedDiscounts = discounts;
        UpdateListing();
    }

    public void UpdateListing()
    {
        var sorted = _cachedListings.OrderBy(l => l.Priority)
                                    .ThenBy(l => l.Cost.Values.Sum());

        // should probably chunk these out instead. to-do if this clogs the internet tubes.
        // maybe read clients prototypes instead?
        ClearListings();
        var storeDiscounts = _cachedDiscounts.Where(x => x.Count > 0)
                                      .ToDictionary(x => x.ListingId);

        foreach (var item in sorted)
        {
            storeDiscounts.TryGetValue(item.ID, out var discountData);
            if (discountData != null)
            {
                item.Categories.Add(DiscountedCategoryPrototypeKey);
            }
            AddListingGui(item, discountData);
        }
    }

    public void SetFooterVisibility(bool visible)
    {
        TraitorFooter.Visible = visible;
    }

    private void OnWithdrawButtonDown(BaseButton.ButtonEventArgs args)
    {
        // check if window is already open
        if (_withdrawWindow != null && _withdrawWindow.IsOpen)
        {
            _withdrawWindow.MoveToFront();
            return;
        }

        // open a new one
        _withdrawWindow = new StoreWithdrawWindow();
        _withdrawWindow.OpenCentered();

        _withdrawWindow.CreateCurrencyButtons(Balance);
        _withdrawWindow.OnWithdrawAttempt += OnWithdrawAttempt;
    }

    private void OnRefundButtonDown(BaseButton.ButtonEventArgs args)
    {
        OnRefundAttempt?.Invoke(args);
    }

    private void AddListingGui(ListingData listing, StoreDiscountData? discountData)
    {
        if (!listing.Categories.Contains(CurrentCategory))
            return;

        var listingPrice = listing.Cost;
        var hasBalance = CanBuyListing(Balance, listingPrice, discountData);

        var spriteSys = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();

        Texture? texture = null;
        if (listing.Icon != null)
            texture = spriteSys.Frame0(listing.Icon);

        if (listing.ProductEntity != null)
        {
            if (texture == null)
                texture = spriteSys.GetPrototypeIcon(listing.ProductEntity).Default;
        }
        else if (listing.ProductAction != null)
        {
            var actionId = _entityManager.Spawn(listing.ProductAction);
            if (_entityManager.System<ActionsSystem>().TryGetActionData(actionId, out var action) &&
                action.Icon != null)
            {
                texture = spriteSys.Frame0(action.Icon);
            }
        }

        var (listingInStock, discount) = GetListingPriceString(listing, discountData);

        var newListing = new StoreListingControl(listing, listingInStock, discount, hasBalance, texture);
        newListing.StoreItemBuyButton.OnButtonDown += args
            => OnListingButtonPressed?.Invoke(args, listing);

        StoreListingsContainer.AddChild(newListing);
    }

    public bool CanBuyListing(Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> currentBalance, Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> price, StoreDiscountData? discountData)
    {
        foreach (var (currency, value) in price)
        {
            if (!currentBalance.ContainsKey(currency))
                return false;

            var amount = value;
            if (discountData != null && discountData.DiscountAmountByCurrency.TryGetValue(currency, out var discount))
            {
                amount -= discount;
            }

            if (currentBalance[currency] < amount)
                return false;
        }

        return true;
    }

    private (string Price, string Discount) GetListingPriceString(ListingData listing, StoreDiscountData? discountData)
    {
        var text = string.Empty;
        var maxDiscount = 0f;

        if (listing.Cost.Count < 1)
            text = Loc.GetString("store-currency-free");
        else
        {
            foreach (var (type, amount) in listing.Cost)
            {
                var totalAmount = amount;
                if (discountData?.DiscountAmountByCurrency.TryGetValue(type, out var discountBy) != null
                    && discountBy > 0)
                {
                    var discountPercent = (float)discountBy.Value / totalAmount.Value;
                    totalAmount -= discountBy;
                    maxDiscount = Math.Max(maxDiscount, discountPercent);
                }

                var currency = _prototypeManager.Index<CurrencyPrototype>(type);
                text += Loc.GetString(
                    "store-ui-price-display",
                    ("amount", totalAmount),
                    ("currency", Loc.GetString(currency.DisplayName, ("amount", totalAmount)))
                );
            }
        }

        var discountMessage = string.Empty;
        if (maxDiscount > 0)
        {
            discountMessage = Loc.GetString(
                "store-ui-discount-display",
                ("amount", (maxDiscount * 100).ToString("####"))
            );
        }

        return (text.TrimEnd(), discountMessage);
    }

    private void ClearListings()
    {
        StoreListingsContainer.Children.Clear();
    }

    public void PopulateStoreCategoryButtons(HashSet<ListingData> listings, List<StoreDiscountData> discounts)
    {
        var allCategories = new List<StoreCategoryPrototype>();
        foreach (var listing in listings)
        {
            foreach (var cat in listing.Categories)
            {
                var proto = _prototypeManager.Index(cat);
                if (!allCategories.Contains(proto))
                    allCategories.Add(proto);
            }
        }

        if (discounts.Any(x => x.Count > 0))
        {
            var proto = _prototypeManager.Index<StoreCategoryPrototype>(DiscountedCategoryPrototypeKey);

            allCategories.Add(proto);
        }

        allCategories = allCategories.OrderBy(c => c.Priority).ToList();

        // This will reset the Current Category selection if nothing matches the search.
        if (allCategories.All(category => category.ID != CurrentCategory))
            CurrentCategory = string.Empty;

        if (CurrentCategory == string.Empty && allCategories.Count > 0)
            CurrentCategory = allCategories.First().ID;

        CategoryListContainer.Children.Clear();
        if (allCategories.Count < 1)
            return;

        var group = new ButtonGroup();
        foreach (var proto in allCategories)
        {
            var catButton = new StoreCategoryButton
            {
                Text = Loc.GetString(proto.Name),
                Id = proto.ID,
                Pressed = proto.ID == CurrentCategory,
                Group = group,
                ToggleMode = true,
                StyleClasses = { "OpenBoth" }
            };

            catButton.OnPressed += args => OnCategoryButtonPressed?.Invoke(args, catButton.Id);
            CategoryListContainer.AddChild(catButton);
        }
    }

    public override void Close()
    {
        base.Close();
        _withdrawWindow?.Close();
    }

    public void UpdateRefund(bool allowRefund)
    {
        RefundButton.Visible = allowRefund;
    }

    private sealed class StoreCategoryButton : Button
    {
        public string? Id;
    }
}
