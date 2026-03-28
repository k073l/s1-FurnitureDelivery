using System.Linq;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Shops;
using FurnitureDelivery.Shops.UI;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.ItemFramework;

#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.ItemFramework;
#endif

namespace FurnitureDelivery.Builders;

public class DeliveryShopBuilder
{
    private string _shopName = "CustomShop";
    private string _shopDescription = "Custom shop description";
    private Sprite _shopImage;
    private Color _shopColor = Color.red;
    private float _deliveryFee = 100f;
    private bool _availableByDefault = true;
    private int _insertPosition = -1;

    private DeliveryVehicle _deliveryVehicle;
    private readonly List<ShopListing> _listings = new();

    private readonly DeliveryShop _deliveryShopTemplate;
    private readonly DeliveryApp _deliveryApp;

    public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-DeliveryShopBuilder");

    public static System.Collections.Generic.Dictionary<DeliveryShop, float> DeliveryFeeRegistry => Helpers.Registries.DeliveryFeeRegistry;

    internal static int OriginalDeliveryShopsCount
    {
        get => Helpers.Registries.OriginalDeliveryShopsCount;
        set => Helpers.Registries.OriginalDeliveryShopsCount = value;
    }

    public DeliveryShopBuilder(DeliveryApp appInstance)
    {
        _deliveryApp = appInstance;

        var allShops = appInstance.GetComponentsInChildren<DeliveryShop>(true)
            .Where(ds => ds != null && ds.transform.childCount > 0)
            .ToList();

        _deliveryShopTemplate = allShops.FirstOrDefault(ds =>
            ds.transform.Find(UIPathConstants.Container) != null &&
            ds.transform.Find(UIPathConstants.Dropshadow) != null &&
            ds.ListingEntryPrefab != null);

        _deliveryShopTemplate ??= allShops.FirstOrDefault(ds =>
            ds.ListingEntryPrefab != null &&
            ds.ListingContainer != null &&
            ds.ListingContainer.name == UIPathConstants.Listings &&
            ds.BackButton != null);

        _deliveryShopTemplate ??= allShops.FirstOrDefault(ds => ds.ListingEntryPrefab != null);
        _deliveryShopTemplate ??= allShops.FirstOrDefault(ds => ds.transform.childCount > 0);
        _deliveryShopTemplate ??= allShops.FirstOrDefault();

        if (_deliveryShopTemplate == null)
            Logger.Error("No DeliveryShop template found in app.");
    }

    public DeliveryShopBuilder WithShopName(string name)
    {
        _shopName = name;
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

    public DeliveryShopBuilder SetPosition(int position)
    {
        _insertPosition = position;
        return this;
    }

    public DeliveryShopBuilder AddListing(StorableItemDefinition item, float? overridePrice = null, int quantity = 999)
    {
        _listings.Add(new ShopListing
        {
            name = item.name,
            Item = item,
            CanBeDelivered = true,
            OverridePrice = true,
            OverriddenPrice = overridePrice ?? item.BasePurchasePrice
        });
        return this;
    }

    public DeliveryShopBuilder AddListing(ShopListing listing)
    {
        _listings.Add(listing);
        return this;
    }

    public DeliveryShop Build()
    {
        Logger.Debug($"Build() START for {_shopName}");

        if (_deliveryShopTemplate == null)
        {
            Logger.Error("Cannot build delivery shop without template.");
            return null;
        }

        try
        {
            var shopInterface = CreateShopInterface();
            var shopInstance = CloneDeliveryShop();
            if (shopInstance == null) return null;

            LinkUIElements(shopInstance, shopInterface);
            InitializeShop(shopInstance, shopInterface);

            Helpers.Registries.RegisterShopPosition(shopInstance.gameObject.name, _insertPosition);
            if (_shopImage != null)
                Helpers.Registries.RegisterShopImage(shopInstance, _shopImage);

            Logger.Debug($"Built delivery shop: {shopInstance.name}");
            return shopInstance;
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Build() FAILED for {_shopName}: {ex.Message}");
            return null;
        }
    }

    private ShopInterface CreateShopInterface()
    {
        var builder = new ShopInterfaceBuilder(
            _shopName,
            _shopDescription,
            _shopImage,
            _listings,
            GetOrCreateDeliveryVehicle(),
            _deliveryApp);

        var (shopInterface, _) = builder.Build();
        return shopInterface;
    }

    private DeliveryShop CloneDeliveryShop()
    {
        var orderTransform = _deliveryApp.transform.Find(UIPathConstants.Order);
        if (orderTransform == null)
        {
            Logger.Error("Cannot find Order canvas!");
            return null;
        }

        try
        {
            var shopInstance = Object.Instantiate(_deliveryShopTemplate, orderTransform);
            Logger.Debug($"Cloned shop has {shopInstance.transform.childCount} children");
            return shopInstance;
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Object.Instantiate failed: {ex.Message}");
            return null;
        }
    }

    private void LinkUIElements(DeliveryShop shopInstance, ShopInterface shopInterface)
    {
        var linker = new DeliveryShopUIElementsLinker(
            shopInstance, shopInterface, _shopColor, _shopName,
            _shopDescription, _shopImage, _deliveryShopTemplate, _deliveryFee, _availableByDefault);
        linker.LinkAll();
    }

    private void InitializeShop(DeliveryShop shopInstance, ShopInterface shopInterface)
    {
        var initializer = new ShopInitializer(shopInstance, shopInterface);
        initializer.Initialize();
    }

    private DeliveryVehicle GetOrCreateDeliveryVehicle()
    {
        if (_deliveryVehicle != null) return _deliveryVehicle;

        if (Helpers.Registries.DeliveryVehicleRegistry.Count == 0)
            Helpers.Registries.RegisterAvailableVehicles();

        var firstVehicle = Helpers.Registries.DeliveryVehicleRegistry.Keys.FirstOrDefault();
        if (firstVehicle == null)
        {
            Logger.Error("No available vehicles registered.");
            return null;
        }

        return Helpers.Registries.GetOrCreateDeliveryVehicle(firstVehicle);
    }

    public static List<DeliveryShop> GetInitializedShops(DeliveryApp app, out Transform contentT)
    {
        contentT = app.transform.Find(UIPathConstants.ScrollViewContent);

        if (contentT == null)
        {
            Logger.Debug($"Could not find '{UIPathConstants.ScrollViewContent}' under DeliveryApp");
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
        var insertPosition = Helpers.Registries.GetShopPosition(shop.gameObject.name);

        if (insertPosition < 0)
        {
            insertPosition = app.deliveryShops.Count + insertPosition + 1;
            if (insertPosition < 0) insertPosition = 0;
        }

        if (insertPosition > app.deliveryShops.Count)
            insertPosition = app.deliveryShops.Count;

        if (insertPosition < app.deliveryShops.Count)
            app.deliveryShops.Insert(insertPosition, shop);
        else
            app.deliveryShops.Add(shop);

        ReorderButtons(app);

        Logger.Debug($"Added new delivery shop to app: {shop.name}, {shop.gameObject.name}");
    }

    private static void ReorderButtons(DeliveryApp app)
    {
        if (app._shopElements == null || app._shopElements.Count == 0) return;

        app._shopElements
#if !MONO
            ._items.ToList()
#endif
            .Sort((a, b) =>
            {
                if (a?.Shop == null || b?.Shop == null) return 0;
                var indexA = app.deliveryShops.IndexOf(a.Shop);
                var indexB = app.deliveryShops.IndexOf(b.Shop);
                return indexA.CompareTo(indexB);
            });

        for (var i = 0; i < app._shopElements.Count; i++)
        {
            var element = app._shopElements[i];
            if (element?.Button?.gameObject != null)
                element.Button.transform.SetSiblingIndex(i);
        }
    }

    private class DeliveryShopUIElementsLinker : UIElementsLinker
    {
        private readonly float _deliveryFee;
        private readonly bool _availableByDefault;

        public DeliveryShopUIElementsLinker(
            DeliveryShop shopInstance,
            ShopInterface shopInterface,
            Color shopColor,
            string shopName,
            string shopDescription,
            Sprite shopImage,
            DeliveryShop deliveryShopTemplate,
            float deliveryFee,
            bool availableByDefault)
            : base(shopInstance, shopInterface, shopColor, shopName, shopDescription, shopImage, deliveryShopTemplate)
        {
            _deliveryFee = deliveryFee;
            _availableByDefault = availableByDefault;
        }

        protected override float GetDeliveryFee() => _deliveryFee;
        protected override bool GetAvailableByDefault() => _availableByDefault;
    }
}