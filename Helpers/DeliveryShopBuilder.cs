using System.Linq;
using FurnitureDelivery.Interop;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if MONO
using TMPro;
using ScheduleOne.Delivery;
using ScheduleOne.Money;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.ItemFramework;
using ScheduleOne.Vehicles;

#else
using Il2CppTMPro;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Vehicles;
#endif

namespace FurnitureDelivery.Helpers;

public class DeliveryShopBuilder
{
    private string _shopName = "CustomShop";
    private string _shopDescription = "Custom shop description";
    private Sprite _shopImage = Utils.FindSprite("Salvador_Mugshot");
    private Color _shopColor = Color.red;
    private float _deliveryFee = 100f;
    private bool _availableByDefault = true;
    private int _insertPosition = -1;

    private DeliveryVehicle _deliveryVehicle = null;
    private readonly List<ShopListing> _listings = new List<ShopListing>();

    private readonly DeliveryShop _deliveryShopTemplate;
    private readonly DeliveryApp _deliveryApp;

    public static GameObject _fdRoot = new GameObject("FurnitureDeliveryRoot");

    public static Dictionary<LandVehicle, DeliveryVehicle> DeliveryVehicleRegistry = new();

    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-DeliveryShopBuilder");

    internal static Dictionary<DeliveryShop, float> DeliveryFeeRegistry = new();

    internal static int _originalDeliveryShopsCount = -1;


    public DeliveryShopBuilder(DeliveryApp appInstance)
    {
        _deliveryApp = appInstance;

        // Find a DeliveryShop that has UI children 
        var allShops = appInstance.GetComponentsInChildren<DeliveryShop>(true)
            .Where(ds => ds != null && ds.transform.childCount > 0)
            .ToList();

        // Prefer shops with NEW UI structure (Container + Dropshadow, NOT Header + Contents)
        _deliveryShopTemplate = allShops.FirstOrDefault(ds =>
            ds.transform.Find("Container") != null &&
            ds.transform.Find("Dropshadow") != null &&
            ds.ListingEntryPrefab != null);

        // Fallback to shops with Listings container (new style)
        if (_deliveryShopTemplate == null)
        {
            _deliveryShopTemplate = allShops.FirstOrDefault(ds =>
                ds.ListingEntryPrefab != null &&
                ds.ListingContainer != null &&
                ds.ListingContainer.name == "Listings" &&
                ds.BackButton != null);
        }

        // Fallback to any shop with ListingEntryPrefab
        if (_deliveryShopTemplate == null)
        {
            _deliveryShopTemplate = allShops.FirstOrDefault(ds => ds.ListingEntryPrefab != null);
        }

        // Last resort - any shop with children (OLD style)
        if (_deliveryShopTemplate == null)
        {
            _deliveryShopTemplate = allShops.FirstOrDefault(ds => ds.transform.childCount > 0);
        }

        // fallback - any shop
        if (_deliveryShopTemplate == null)
        {
            _deliveryShopTemplate = allShops.FirstOrDefault();
        }

        if (_deliveryShopTemplate == null)
        {
            Logger.Error("No DeliveryShop template found in app.");
        }

        if (_fdRoot == null)
        {
            _fdRoot = new GameObject("FurnitureDeliveryRoot");
        }
    }

    public DeliveryShopBuilder WithShopName(string name)
    {
        _shopName = name;
        return this;
    }

    public DeliveryShopBuilder WithDeliveryFee(float fee)
    {
        _deliveryFee = fee;
        return this;
    }

    public DeliveryShopBuilder SetAvailableByDefault(bool available)
    {
        _availableByDefault = available;
        return this;
    }

    public DeliveryShopBuilder WithDeliveryVehicle(DeliveryVehicle vehicle)
    {
        _deliveryVehicle = vehicle;
        return this;
    }

    public DeliveryShopBuilder WithShopDescription(string description)
    {
        _shopDescription = description;
        return this;
    }

    public DeliveryShopBuilder WithShopImage(Sprite image)
    {
        _shopImage = image;
        return this;
    }

    public DeliveryShopBuilder WithShopColor(Color color)
    {
        _shopColor = color;
        return this;
    }

    public static void RegisterAvailableVehicles()
    {
        var vehicleManager = VehicleManager.Instance;
        if (vehicleManager == null || vehicleManager.AllVehicles == null)
        {
            Logger.Error("VehicleManager or AllVehicles is null.");
            return;
        }

        foreach (var vehicle in vehicleManager.AllVehicles.AsEnumerable())
        {
            if (vehicle == null) continue;
            if (!DeliveryVehicleRegistry.ContainsKey(vehicle))
            {
                DeliveryVehicleRegistry[vehicle] = null; // not yet wrapped
            }
        }

        Logger.Debug(
            $"Registered {DeliveryVehicleRegistry.Count} vehicles for lazy wrapping.");
    }

    public static DeliveryVehicle GetOrCreateDeliveryVehicle(LandVehicle vehicle)
    {
        if (vehicle == null) return null;

        if (vehicle?.gameObject?.GetComponent<DeliveryVehicle>() != null)
            return vehicle.gameObject.GetComponent<DeliveryVehicle>();

        if (DeliveryVehicleRegistry.TryGetValue(vehicle, out var cached) && cached != null)
        {
            Logger.Debug("Using cached DeliveryVehicle for " + vehicle.name);
            if (cached.Vehicle != vehicle)
            {
                Logger.Warning(
                    $"Cached DeliveryVehicle's Vehicle does not match for {vehicle.name}. Updating reference.");
                cached.Vehicle = vehicle;
            }

            return cached;
        }

        var vehicleObject = vehicle.gameObject;
        vehicleObject.transform.SetParent(_fdRoot.transform);
        var deliveryVehicle = vehicleObject.AddComponent<DeliveryVehicle>();
        deliveryVehicle.Vehicle = vehicle;
        deliveryVehicle.GUID = vehicle.GUID.ToString();
        Logger.Debug(
            $"Created new DeliveryVehicle for {vehicle.name} with GUID {deliveryVehicle.GUID} and vehicle GUID {vehicle.GUID}");

        DeliveryVehicleRegistry[vehicle] = deliveryVehicle;

        return deliveryVehicle;
    }

    private DeliveryVehicle CreateDeliveryVehicle()
    {
        if (_deliveryVehicle != null)
            return _deliveryVehicle;

        if (DeliveryVehicleRegistry.Count == 0)
        {
            RegisterAvailableVehicles();
        }

        var firstVehicle = DeliveryVehicleRegistry.Keys.FirstOrDefault();
        if (firstVehicle == null)
        {
            Logger.Error("No available vehicles registered.");
            return null;
        }

        return GetOrCreateDeliveryVehicle(firstVehicle);
    }

    public DeliveryShopBuilder AddListing(StorableItemDefinition item, float? overridePrice = null,
        int quantity = 999)
    {
        var listing = new ShopListing
        {
            name = item.name,
            Item = item,
            CanBeDelivered = true,
            OverridePrice = true,
            OverriddenPrice = overridePrice ?? item.BasePurchasePrice
        };
        _listings.Add(listing);
        return this;
    }

    public DeliveryShopBuilder AddListing(ShopListing listing)
    {
        _listings.Add(listing);
        return this;
    }

    public DeliveryShopBuilder SetPosition(int position)
    {
        _insertPosition = position;
        return this;
    }


    CartEntry CreateCartEntryPrefab()
    {
        var go = new GameObject("CartEntry");
        go.transform.SetParent(_fdRoot.transform);
        var cartEntry = go.AddComponent<CartEntry>();

        var nameGO = new GameObject("NameLabel");
        cartEntry.NameLabel = nameGO.AddComponent<TextMeshProUGUI>();
        nameGO.transform.SetParent(go.transform, false);

        var priceGO = new GameObject("PriceLabel");
        cartEntry.PriceLabel = priceGO.AddComponent<TextMeshProUGUI>();
        priceGO.transform.SetParent(go.transform, false);

        var incBtnGO = new GameObject("IncrementButton");
        cartEntry.IncrementButton = incBtnGO.AddComponent<Button>();
        incBtnGO.transform.SetParent(go.transform, false);

        var decBtnGO = new GameObject("DecrementButton");
        cartEntry.DecrementButton = decBtnGO.AddComponent<Button>();
        decBtnGO.transform.SetParent(go.transform, false);

        var removeBtnGO = new GameObject("RemoveButton");
        cartEntry.RemoveButton = removeBtnGO.AddComponent<Button>();
        removeBtnGO.transform.SetParent(go.transform, false);

        return cartEntry;
    }


    public DeliveryShop Build()
    {
        Logger.Debug($"Build() START for {_shopName}");

        try
        {
            if (_deliveryShopTemplate == null)
            {
                Logger.Error("Cannot build delivery shop without template.");
                return null;
            }

            var shopObj = new GameObject($"ShopInterface_{_shopName}");
            shopObj.transform.SetParent(_fdRoot.transform);
            shopObj.SetActive(false);
            var newInterface = shopObj.AddComponent<ShopInterface>();

            newInterface.gameObject.name = _shopName;
            newInterface.ShopName = _shopName;

            Logger.Debug($"Created ShopInterface: {newInterface.ShopName}");

#if !MONO
            newInterface.Listings = _listings.ToIl2CppList();
#else
            newInterface.Listings = _listings;
#endif
            foreach (var item in _listings)
            {
                item.Shop = newInterface;
            }

            newInterface.DeliveryVehicle = CreateDeliveryVehicle();
            newInterface.ShopCode = _shopName;

            var cart = newInterface.gameObject.AddComponent<Cart>();
            cart.name = $"Cart_{_shopName}";
            cart.Shop = newInterface;
            cart.TotalText = cart.gameObject.AddComponent<TextMeshProUGUI>();
            cart.WarningText = cart.gameObject.AddComponent<TextMeshProUGUI>();
            cart.ProblemText = cart.gameObject.AddComponent<TextMeshProUGUI>();
            // cart.ViewCartText = cart.gameObject.AddComponent<TextMeshProUGUI>();

            // cart.BuyButton = cart.gameObject.AddComponent<Button>();
            cart.LoadVehicleToggle = cart.gameObject.AddComponent<Toggle>();
            cart.CartArea = cart.gameObject.AddComponent<Image>();

            cart.EntryPrefab = CreateCartEntryPrefab();

            var container = new GameObject("CartEntryContainer");
            container.transform.SetParent(cart.transform);
            cart.CartEntryContainer = container.AddComponent<RectTransform>();

            Logger.Debug($"Cart for shop {_shopName} is {cart.name} and is {cart.gameObject.activeSelf}");
            newInterface.Cart = cart;

            // Find template from DeliveryApp's deliveryShops which are populated by Awake()
            Logger.Debug("Looking for template from deliveryShops");

            ShopInterface templateShop = null;

            if (_originalDeliveryShopsCount < 0 && _deliveryApp?.deliveryShops != null)
                _originalDeliveryShopsCount = _deliveryApp.deliveryShops.Count;

            if (_deliveryApp?.deliveryShops != null)
            {
                var originalCount = _originalDeliveryShopsCount;
                foreach (var ds in _deliveryApp.deliveryShops)
                {
                    var index = _deliveryApp.deliveryShops.IndexOf(ds);
                    if (index >= originalCount) continue;

                    if (ds?.MatchingShop?.ListingUIPrefab != null)
                    {
                        templateShop = ds.MatchingShop;
                        break;
                    }
                }
            }

            if (templateShop == null)
                templateShop = ShopInterface.AllShops.AsEnumerable()
                                   .FirstOrDefault(shop => shop != null && shop.ListingUIPrefab != null)
                               ?? ShopInterface.AllShops.AsEnumerable().FirstOrDefault(shop => shop != null);

            if (templateShop == null)
            {
                Logger.Error("No valid template shop found!");
                return null;
            }

            // Only copy prefab assets, NOT scene object references
            // Sharing scene references (ListingContainer, StoreNameLabel, etc.) from Dan's Hardware
            // causes the original shop to break when we clear listings later
            newInterface.ListingUIPrefab = templateShop.ListingUIPrefab;

            if (newInterface.ListingUIPrefab != null)
                newInterface.ListingUIPrefab.gameObject.SetActive(false);

            Logger.Debug("Adding ShopInterface to AllShops");

#if MONO
            ShopInterface.AllShops.RemoveAll(si => si == null);
#else
            ShopInterface.AllShops.RemoveAll((Il2CppSystem.Predicate<ShopInterface>)(si => si == null));
#endif
            ShopInterface.AllShops.Add(newInterface);

            var orderTransform = _deliveryApp.transform.Find("Container/Contents/Container/Shops/Order");
            if (orderTransform == null)
            {
                Logger.Error("Cannot find Order canvas!");
                return null;
            }

            DeliveryShop shopInstance = null;
            try
            {
                shopInstance = Object.Instantiate(_deliveryShopTemplate, orderTransform);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Object.Instantiate failed: {ex.Message}");
                return null;
            }

            if (shopInstance == null)
            {
                Logger.Error("Object.Instantiate returned null!");
                return null;
            }

            Logger.Debug($"Cloned shop has {shopInstance.transform.childCount} children");

            // Set the ShopColor field
            shopInstance.ShopColor = _shopColor;

            // Try NEW UI structure first (Container/Header)
            var headerTransform = shopInstance.transform.Find("Container/Header");

            // If no Header, try OLD UI structure (Header)
            if (headerTransform == null)
            {
                headerTransform = shopInstance.transform.Find("Header");
            }

            if (headerTransform != null)
            {
                var headerBg = headerTransform.GetComponent<Image>();
                if (headerBg != null)
                {
                    headerBg.color = _shopColor;
                }

                // Icon is at Container/Header/Container/Icon/Image
                var iconTransform = headerTransform.Find("Container/Icon/Image");
                if (iconTransform != null)
                {
                    var iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null && _shopImage != null)
                    {
                        iconImage.sprite = _shopImage;
                    }
                }

                // Title is at Container/Header/Container/Labels/Container/Title
                var titleTransform = headerTransform.Find("Container/Labels/Container/Title");
                if (titleTransform != null)
                {
                    var titleText = titleTransform.GetComponent<Text>();
                    if (titleText != null)
                    {
                        titleText.text = _shopName;
                    }
                }

                // Description is at Container/Header/Container/Labels/Container/Description
                var descTransform = headerTransform.Find("Container/Labels/Container/Description");
                if (descTransform != null)
                {
                    var descText = descTransform.GetComponent<Text>();
                    if (descText != null)
                    {
                        descText.text = _shopDescription;
                    }
                }

                // Also set the MatchingShop's name
                if (newInterface != null)
                {
                    newInterface.ShopName = _shopName;
                    newInterface.ShopDescription = _shopDescription;
                }
            }

            // Explicitly set required fields - clone should have copied them but be safe
            shopInstance.ListingEntryPrefab = _deliveryShopTemplate.ListingEntryPrefab;
            Logger.Debug(
                $"Set ListingEntryPrefab: {(shopInstance.ListingEntryPrefab != null ? shopInstance.ListingEntryPrefab.name : "NULL")}");

            // Find ListingContainer - check for both old and new naming conventions
            var listingContainerTransform = shopInstance.transform.Find("Listings") ??
                                            shopInstance.transform.Find("ListingContainer") ??
                                            shopInstance.transform.Find("Entries");
            if (listingContainerTransform != null)
            {
                if (Utils.Is<RectTransform>(listingContainerTransform, out var rectTransform))
                    shopInstance.ListingContainer = rectTransform;
                Logger.Debug($"Found ListingContainer: {shopInstance.ListingContainer?.name}");
            }

            // Clear any pre-existing children from the template (including listing entries)
            // This prevents items from other shops (e.g., Dan's Hardware) from appearing
            if (shopInstance.ListingContainer != null)
            {
                for (var i = 0; i < shopInstance.ListingContainer.transform.childCount; i++)
                {
                    var listingChild = shopInstance.ListingContainer.transform.GetChild(i);
                    if (listingChild != null)
                    {
                        Object.Destroy(listingChild.gameObject);
                    }
                }

                Logger.Debug($"Cleared children from ListingContainer");
            }

            // Find BackButton - new UI has it inside Container, old UI has it at root
            var backButtonTransform = shopInstance.transform.Find("Container/BackButton") ??
                                      shopInstance.transform.Find("BackButton");
            if (backButtonTransform != null)
            {
                shopInstance.BackButton = backButtonTransform.GetComponent<Button>();
                Logger.Debug("Found BackButton");
            }

            // Find DeliveryTimeLabel - new UI has it inside Container
            var deliveryTimeTransform = shopInstance.transform.Find("Container/DeliveryTime") ??
                                        shopInstance.transform.Find("Container/DeliveryTimeLabel") ??
                                        shopInstance.transform.Find("DeliveryTimeLabel");
            if (deliveryTimeTransform != null)
            {
                shopInstance.DeliveryTimeLabel = deliveryTimeTransform.GetComponent<Text>();
                Logger.Debug("Found DeliveryTimeLabel");
            }

            // Find OrderButton - new UI has it inside Container
            var orderButtonTransform = shopInstance.transform.Find("Container/Confirm") ??
                                       shopInstance.transform.Find("Container/OrderButton") ??
                                       shopInstance.transform.Find("OrderButton");
            if (orderButtonTransform != null)
            {
                shopInstance.OrderButton = orderButtonTransform.GetComponent<Button>();
                Logger.Debug("Found OrderButton");
            }

            // Find DestinationDropdown - new UI has it inside Container
            var destDropdownTransform = shopInstance.transform.Find("Container/DestinationDropdown") ??
                                        shopInstance.transform.Find("DestinationDropdown");
            if (destDropdownTransform != null)
            {
                shopInstance.DestinationDropdown = destDropdownTransform.GetComponent<Dropdown>();
                Logger.Debug("Found DestinationDropdown");
            }

            // Find LoadingDockDropdown - new UI has it inside Container
            var dockDropdownTransform = shopInstance.transform.Find("Container/LoadingDockDropdown") ??
                                        shopInstance.transform.Find("LoadingDockDropdown");
            if (dockDropdownTransform != null)
            {
                shopInstance.LoadingDockDropdown = dockDropdownTransform.GetComponent<Dropdown>();
                Logger.Debug("Found LoadingDockDropdown");
            }

            // Find labels - new UI has them inside Container
            var itemTotalTransform = shopInstance.transform.Find("Container/ItemTotal/Amount") ??
                                     shopInstance.transform.Find("Container/ItemTotal") ??
                                     shopInstance.transform.Find("ItemTotalLabel");
            if (itemTotalTransform != null)
            {
                shopInstance.ItemTotalLabel = itemTotalTransform.GetComponent<Text>();
                Logger.Debug("Found ItemTotalLabel");
            }

            var orderTotalTransform = shopInstance.transform.Find("Container/OrderTotal/Amount") ??
                                      shopInstance.transform.Find("Container/OrderTotal") ??
                                      shopInstance.transform.Find("OrderTotalLabel");
            if (orderTotalTransform != null)
            {
                shopInstance.OrderTotalLabel = orderTotalTransform.GetComponent<Text>();
                Logger.Debug("Found OrderTotalLabel");
            }

            var deliveryFeeTransform = shopInstance.transform.Find("Container/DeliveryFee/Amount") ??
                                       shopInstance.transform.Find("Container/DeliveryFee") ??
                                       shopInstance.transform.Find("DeliveryFeeLabel");
            if (deliveryFeeTransform != null)
            {
                shopInstance.DeliveryFeeLabel = deliveryFeeTransform.GetComponent<Text>();
                Logger.Debug("Found DeliveryFeeLabel");
            }

            shopInstance.MatchingShopInterfaceName = _shopName;
            shopInstance.MatchingShop = newInterface;
            DeliveryFeeRegistry.TryAdd(shopInstance, _deliveryFee);
            shopInstance.AvailableByDefault = _availableByDefault;
            shopInstance.gameObject.name = $"DeliveryShop_{_shopName}";

            Logger.Debug(
                $"Cloned shop ListingEntryPrefab: {(shopInstance.ListingEntryPrefab != null ? shopInstance.ListingEntryPrefab.name : "NULL")}");
            Logger.Debug(
                $"Cloned shop ListingContainer: {(shopInstance.ListingContainer != null ? shopInstance.ListingContainer.name : "NULL")}");

            ShopPositionRegistry.ShopPositions[shopInstance.gameObject.name] = _insertPosition;

            shopInstance.gameObject.SetActive(true);

            Logger.Debug($"Manual Initialize for {shopInstance.gameObject.name}");

            try
            {
                // Create listing entries for each listing that can be delivered
                foreach (var listing in shopInstance.MatchingShop.Listings)
                {
                    try
                    {
                        if (listing.CanBeDelivered)
                        {
                            var entry = Object.Instantiate(shopInstance.ListingEntryPrefab,
                                shopInstance.ListingContainer);
                            entry.Initialize(listing);
                            entry.onQuantityChanged.AddListener((UnityAction)(shopInstance.RefreshCart));
                            shopInstance.OnSelect += (Action<DeliveryShop>)((shop) => entry.RefreshLocked());

                            // Add entry to the listingEntries list so GetCartCost() works
                            shopInstance.listingEntries.Add(entry);
                        }
                    }
                    catch (System.Exception listingEx)
                    {
                        Logger.Warning($"Failed to create entry for {listing.name}: {listingEx.Message}");
                    }
                }

                // Set up button listeners
                if (shopInstance.BackButton != null)
                {
                    shopInstance.BackButton.onClick.RemoveAllListeners();
                    shopInstance.BackButton.onClick.AddListener((UnityAction)(() =>
                        shopInstance.OnSelect?.Invoke(shopInstance)));
                }

                if (shopInstance.OrderButton != null)
                {
                    shopInstance.OrderButton.onClick.RemoveAllListeners();
                    shopInstance.OrderButton.onClick.AddListener((UnityAction)(() =>
                        shopInstance.SubmitOrder(string.Empty)));
                }

                if (shopInstance.DestinationDropdown != null)
                {
                    shopInstance.DestinationDropdown.onValueChanged.RemoveAllListeners();
                    shopInstance.DestinationDropdown.onValueChanged.AddListener(
                        (UnityAction<int>)(shopInstance.DestinationDropdownSelected));
                }

                if (shopInstance.LoadingDockDropdown != null)
                {
                    shopInstance.LoadingDockDropdown.onValueChanged.RemoveAllListeners();
                    shopInstance.LoadingDockDropdown.onValueChanged.AddListener(
                        (UnityAction<int>)(shopInstance.LoadingDockDropdownSelected));
                }

                Logger.Debug("Manual Initialize complete");
            }
            catch (System.Exception initEx)
            {
                Logger.Error($"Manual Initialize failed: {initEx.Message}");
            }

            // Set shop inactive after initialization (matching game's Initialize behavior)
            shopInstance.gameObject.SetActive(false);

            Logger.Debug($"Built delivery shop: {shopInstance.name}");
            if (_shopImage != null)
                ShopImageRegistry.Images.TryAdd(shopInstance, _shopImage);
            return shopInstance;
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Build() FAILED for {_shopName}: {ex.Message}");
            return null;
        }
    }

    public static List<DeliveryShop> GetInitializedShops(DeliveryApp app, out Transform contentT)
    {
        contentT = app.transform.Find("Container/Scroll View/Viewport/Content");

        if (contentT == null)
        {
            Logger.Debug("Could not find 'Container/Scroll View/Viewport/Content' under DeliveryApp");
            return null;
        }

        var shopComponents = new List<DeliveryShop>();
        for (var i = 0; i < contentT.childCount; i++)
        {
            var shop = contentT.GetChild(i).GetComponent<DeliveryShop>();
            if (shop != null)
                shopComponents.Add(shop);
        }

        return shopComponents;
    }

    public static void Apply(DeliveryApp app, DeliveryShop shop)
    {
        var insertPosition = -1;
        if (ShopPositionRegistry.ShopPositions.TryGetValue(shop.gameObject.name, out var position))
        {
            insertPosition = position;
        }

        if (insertPosition < 0)
        {
            insertPosition = app.deliveryShops.Count + insertPosition + 1;
            if (insertPosition < 0) insertPosition = 0;
        }

        if (insertPosition > app.deliveryShops.Count)
            insertPosition = app.deliveryShops.Count;

        if (insertPosition < app.deliveryShops.Count)
        {
            app.deliveryShops.Insert(insertPosition, shop);
        }
        else
        {
            app.deliveryShops.Add(shop);
        }

        // Reorder buttons to match deliveryShops list order
        ReorderButtons(app);

        Logger.Msg($"Added new delivery shop to app: {shop.name}, {shop.gameObject.name}");
    }

    private static void ReorderButtons(DeliveryApp app)
    {
        if (app._shopElements == null || app._shopElements.Count == 0) return;

        // Sort _shopElements to match deliveryShops order
        app._shopElements.Sort((a, b) =>
        {
            if (a?.Shop == null || b?.Shop == null) return 0;
            var indexA = app.deliveryShops.IndexOf(a.Shop);
            var indexB = app.deliveryShops.IndexOf(b.Shop);
            return indexA.CompareTo(indexB);
        });

        // Update sibling indices for button visuals
        for (var i = 0; i < app._shopElements.Count; i++)
        {
            var element = app._shopElements[i];
            if (element?.Button?.gameObject != null)
            {
                element.Button.transform.SetSiblingIndex(i);
            }
        }
    }
}

internal static class ShopImageRegistry
{
    public static Dictionary<DeliveryShop, Sprite> Images = new();
}

public static class ShopPositionRegistry
{
    public static Dictionary<string, int> ShopPositions = new Dictionary<string, int>();
}

// I'd postfix DeliveryShop.RefreshShop, but IL2CPP inlined it
[HarmonyPatch(typeof(DeliveryApp), nameof(DeliveryApp.SetOpen))]
internal class DeliveryApp_SetOpen_UpdateFee
{
    public static void Postfix(DeliveryApp __instance, bool open)
    {
        if (__instance == null) return;
        if (!open) return;
        foreach (var shop in __instance.deliveryShops)
        {
            if (shop?.DeliveryFeeLabel == null) continue;
            if (!DeliveryShopBuilder.DeliveryFeeRegistry.TryGetValue(shop, out var fee)) continue;
            shop.DeliveryFeeLabel.text = MoneyManager.FormatAmount(fee);
        }
    }
}