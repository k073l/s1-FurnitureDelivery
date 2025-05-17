using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.ItemFramework;
using ScheduleOne.Vehicles;
#else
using Il2CppScheduleOne.Delivery;
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

    public static readonly Dictionary<LandVehicle, DeliveryVehicle> DeliveryVehicleRegistry = new();

    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-DeliveryShopBuilder");


    public DeliveryShopBuilder(DeliveryApp appInstance)
    {
        _deliveryShopTemplate = appInstance.GetComponentsInChildren<DeliveryShop>(true).FirstOrDefault();
        if (_deliveryShopTemplate == null)
        {
            Logger.Error("No DeliveryShop template found in app.");
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

        if (DeliveryVehicleRegistry.TryGetValue(vehicle, out var cached) && cached != null)
        {
            return cached;
        }

        GameObject vehicleObject = new GameObject($"DeliveryVehicle_{vehicle.name}");
        var deliveryVehicle = vehicleObject.AddComponent<DeliveryVehicle>();
        deliveryVehicle.Vehicle = vehicle;
        deliveryVehicle.GUID = vehicle.GUID.ToString();

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


    public DeliveryShop Build()
    {
        if (_deliveryShopTemplate == null)
        {
            Logger.Error("Cannot build delivery shop without template.");
            return null;
        }

        GameObject shopObj = new GameObject($"ShopInterface_{_shopName}");
        var newInterface = shopObj.AddComponent<ShopInterface>();

        newInterface.ShopName = _shopName;
#if !MONO
        newInterface.Listings = _listings.ToIl2CppList();
#else
        newInterface.Listings = _listings;
#endif
        newInterface.DeliveryVehicle = CreateDeliveryVehicle();

        ShopInterface.AllShops.Add(newInterface);

        DeliveryShop shopInstance =
            Object.Instantiate(_deliveryShopTemplate, _deliveryShopTemplate.transform.parent);
        shopInstance.MatchingShopInterfaceName = _shopName;
        shopInstance.DeliveryFee = _deliveryFee;
        shopInstance.AvailableByDefault = _availableByDefault;
        shopInstance.gameObject.name = $"DeliveryShop_{_shopName}";

        var headerTransform = shopInstance.transform.Find("Header");
        if (headerTransform != null)
        {
            var headerBg = headerTransform.GetComponent<Image>();
            if (headerBg != null)
            {
                headerBg.color = _shopColor;
            }

            var iconTransform = headerTransform.Find("Icon");
            if (iconTransform != null)
            {
                var imageTransform = iconTransform.Find("Image");
                if (imageTransform != null)
                {
                    var iconImage = imageTransform.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        if (_shopImage != null)
                        {
                            iconImage.sprite = _shopImage;
                        }
                    }
                }
            }

            var titleTransform = headerTransform.Find("Title");
            if (titleTransform != null)
            {
                var titleText = titleTransform.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.text = _shopName;
                }
            }

            var descriptionTransform = headerTransform.Find("Description");
            if (descriptionTransform != null)
            {
                var descriptionText = descriptionTransform.GetComponent<Text>();
                if (descriptionText != null)
                {
                    descriptionText.text = _shopDescription;
                }
            }
        }

        ShopPositionRegistry.ShopPositions[shopInstance.gameObject.name] = _insertPosition;

        shopInstance.gameObject.SetActive(true);
        return shopInstance;
    }

    public static List<DeliveryShop> GetInitializedShops(DeliveryApp app, out Transform contentT)
    {
        contentT = app.transform.Find("Container/Scroll View/Viewport/Content");

        if (contentT == null)
        {
            Logger.Error("Could not find 'Container/Scroll View/Viewport/Content' under DeliveryApp");
            return null;
        }

        var shopComponents = new List<DeliveryShop>();
        for (int i = 0; i < contentT.childCount; i++)
        {
            var shop = contentT.GetChild(i).GetComponent<DeliveryShop>();
            if (shop != null)
                shopComponents.Add(shop);
        }

        return shopComponents;
    }

    public static void Apply(DeliveryApp app, DeliveryShop shop)
    {
        int insertPosition = -1;
        if (ShopPositionRegistry.ShopPositions.TryGetValue(shop.gameObject.name, out int position))
        {
            insertPosition = position;
            Logger.Debug($"Found position {insertPosition} for shop {shop.gameObject.name}");
        }

        if (insertPosition < 0)
        {
            insertPosition = app.deliveryShops.Count + insertPosition + 1;
            if (insertPosition < 0) insertPosition = 0;
        }

        if (insertPosition > app.deliveryShops.Count)
            insertPosition = app.deliveryShops.Count;

        // Insert the shop at the specified position
        if (insertPosition < app.deliveryShops.Count)
        {
            Logger.Debug($"Inserting shop at position {insertPosition}");
            app.deliveryShops.Insert(insertPosition, shop);
        }
        else
        {
            Logger.Debug($"Adding shop to end at position {insertPosition}");
            app.deliveryShops.Add(shop);
        }

        Logger.Msg($"Added new delivery shop to app: {shop.name}, {shop.gameObject.name}");

        // fix hierarchy in UI
        FixShopHierarchy(app);
    }

    private static void FixShopHierarchy(DeliveryApp app)
    {
        var shopComponents = GetInitializedShops(app, out var content);
        Logger.Debug($"Found {shopComponents.Count} shop components in UI hierarchy");

        for (int i = 0; i < app.deliveryShops.Count; i++)
        {
            var shop = app.deliveryShops.AsEnumerable().ElementAt(i);
            if (shop.gameObject.name == "Space" || shop.gameObject.name.Contains("Spacer"))
                continue;

            shop.transform.SetParent(content, false);
            shop.transform.SetSiblingIndex(i);
            Logger.Debug($"Set {shop.gameObject.name} to sibling index {i}");
        }
    }
}

public static class ShopPositionRegistry
{
    public static Dictionary<string, int> ShopPositions = new Dictionary<string, int>();
}