using System.Linq;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Shops.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if MONO
using TMPro;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.ItemFramework;
#else
using Il2CppTMPro;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.ItemFramework;
#endif

namespace FurnitureDelivery.Builders;

public class ShopInterfaceBuilder
{
    private readonly string _shopName;
    private readonly string _shopDescription;
    private readonly Sprite _shopImage;
    private readonly List<ShopListing> _listings;
    private readonly DeliveryVehicle _deliveryVehicle;
    private readonly DeliveryApp _deliveryApp;

    public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-ShopInterfaceBuilder");

    public ShopInterfaceBuilder(
        string shopName,
        string shopDescription,
        Sprite shopImage,
        List<ShopListing> listings,
        DeliveryVehicle deliveryVehicle,
        DeliveryApp deliveryApp)
    {
        _shopName = shopName;
        _shopDescription = shopDescription;
        _shopImage = shopImage;
        _listings = listings;
        _deliveryVehicle = deliveryVehicle;
        _deliveryApp = deliveryApp;
    }

    public (ShopInterface shopInterface, Cart cart) Build()
    {
        var shopObj = new GameObject($"ShopInterface_{_shopName}");
        shopObj.transform.SetParent(Helpers.Registries.FDRoot.transform);
        shopObj.SetActive(false);

        var shopInterface = shopObj.AddComponent<ShopInterface>();
        shopInterface.ShopName = _shopName;
        shopInterface.ShopDescription = _shopDescription;

#if !MONO
        shopInterface.Listings = _listings.ToIl2CppList();
#else
        shopInterface.Listings = _listings;
#endif
        foreach (var item in _listings)
        {
            item.Shop = shopInterface;
        }

        shopInterface.DeliveryVehicle = _deliveryVehicle;
        shopInterface.ShopCode = _shopName;

        var cart = BuildCart(shopInterface);
        shopInterface.Cart = cart;

        var templateShop = FindTemplateShop();
        if (templateShop != null)
        {
            shopInterface.ListingUIPrefab = templateShop.ListingUIPrefab;
            if (shopInterface.ListingUIPrefab != null)
                shopInterface.ListingUIPrefab.gameObject.SetActive(false);
        }

        RegisterShopInterface(shopInterface);

        Logger.Debug($"Built ShopInterface: {shopInterface.ShopName}");

        return (shopInterface, cart);
    }

    private Cart BuildCart(ShopInterface shopInterface)
    {
        var cart = shopInterface.gameObject.AddComponent<Cart>();
        cart.name = $"Cart_{_shopName}";
        cart.Shop = shopInterface;
        cart.TotalText = cart.gameObject.AddComponent<TextMeshProUGUI>();
        cart.WarningText = cart.gameObject.AddComponent<TextMeshProUGUI>();
        cart.ProblemText = cart.gameObject.AddComponent<TextMeshProUGUI>();
        cart.LoadVehicleToggle = cart.gameObject.AddComponent<Toggle>();
        cart.CartArea = cart.gameObject.AddComponent<Image>();
        cart.EntryPrefab = CreateCartEntryPrefab();
        cart.CartEntryContainer = CreateCartEntryContainer(cart.transform);

        Logger.Debug($"Built Cart: {cart.name}");
        return cart;
    }

    private CartEntry CreateCartEntryPrefab()
    {
        var go = new GameObject("CartEntry");
        go.transform.SetParent(Helpers.Registries.FDRoot.transform);

        var cartEntry = go.AddComponent<CartEntry>();
        cartEntry.NameLabel = CreateChild<TextMeshProUGUI>(go.transform, "NameLabel");
        cartEntry.PriceLabel = CreateChild<TextMeshProUGUI>(go.transform, "PriceLabel");
        cartEntry.IncrementButton = CreateChild<Button>(go.transform, "IncrementButton");
        cartEntry.DecrementButton = CreateChild<Button>(go.transform, "DecrementButton");
        cartEntry.RemoveButton = CreateChild<Button>(go.transform, "RemoveButton");

        return cartEntry;
    }

    private T CreateChild<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private RectTransform CreateCartEntryContainer(Transform parent)
    {
        var container = new GameObject("CartEntryContainer");
        container.transform.SetParent(parent);
        return container.AddComponent<RectTransform>();
    }

    private ShopInterface FindTemplateShop()
    {
        if (_deliveryApp?.deliveryShops != null && DeliveryShopBuilder.OriginalDeliveryShopsCount >= 0)
        {
            var originalCount = DeliveryShopBuilder.OriginalDeliveryShopsCount;
            foreach (var ds in _deliveryApp.deliveryShops)
            {
                var index = _deliveryApp.deliveryShops.IndexOf(ds);
                if (index >= originalCount) continue;

                if (ds?.MatchingShop?.ListingUIPrefab != null)
                {
                    return ds.MatchingShop;
                }
            }
        }

        return ShopInterface.AllShops.AsEnumerable()
                   .FirstOrDefault(shop => shop != null && shop.ListingUIPrefab != null)
               ?? ShopInterface.AllShops.AsEnumerable().FirstOrDefault(shop => shop != null);
    }

    private void RegisterShopInterface(ShopInterface shopInterface)
    {
#if MONO
        ShopInterface.AllShops.RemoveAll(si => si == null);
#else
        ShopInterface.AllShops.RemoveAll((Il2CppSystem.Predicate<ShopInterface>)(si => si == null));
#endif
        ShopInterface.AllShops.Add(shopInterface);
        Logger.Debug($"Registered ShopInterface: {shopInterface.ShopName}");
    }
}