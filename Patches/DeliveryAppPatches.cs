using System.Linq;
using FurnitureDelivery.Builders;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Shops;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.Money;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
#endif

namespace FurnitureDelivery.Patches;

[HarmonyPatch]
public static class DeliveryAppPatches
{
    public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-DeliveryApp");

    [HarmonyPatch(typeof(DeliveryApp), "SetIsAvailable")]
    public class SetIsAvailable
    {
        public static bool Prefix(DeliveryApp __instance, ShopInterface matchingShop)
        {
            Logger.Debug($"SetIsAvailable: {matchingShop?.ShopName}");
            return true;
        }

        public static void Postfix(DeliveryApp __instance, ShopInterface matchingShop)
        {
            try
            {
                if (matchingShop?.ShopName == null) return;
                if (matchingShop.gameObject == null) return;

                var app = __instance ?? DeliveryApp.Instance;
                if (app?.deliveryShops == null) return;

                if (matchingShop.ShopName == "Oscar's Store")
                {
                    var visible = matchingShop.gameObject.activeSelf;
                    foreach (var ds in app.deliveryShops)
                    {
                        if (ds == null || ds.gameObject == null) continue;
                        if (ds.gameObject.name.StartsWith("DeliveryShop_")) continue;
                        if (ds.gameObject.name.Contains("Oscar") || ds.gameObject.name.Contains("Armory"))
                            SyncButtonVisibility(app, ds, visible);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"SetIsAvailable postfix error: {ex.Message}");
            }
        }

        private static void SyncButtonVisibility(DeliveryApp app, DeliveryShop targetShop, bool visible)
        {
            if (app?._shopElements == null || targetShop == null) return;
            var element = app._shopElements?.AsEnumerable().FirstOrDefault(e => e?.Shop == targetShop);
            element?.Button?.gameObject?.SetActive(visible);
        }
    }

    [HarmonyPatch(typeof(DeliveryApp), "Awake")]
    public class Awake
    {
        public static bool AddedShops = false;

        public static void Prefix() => AddedShops = false;
    }

    [HarmonyPatch(typeof(DeliveryApp), nameof(DeliveryApp.Start))]
    public class Start
    {
        public static bool Initialized;

        public static void Postfix(DeliveryApp __instance)
        {
            if (Initialized) return;
            Initialized = true;

            Logger.Msg("DeliveryApp.Start running");
            var app = __instance;

            var countBefore = app.deliveryShops.Count;
            DeliveryShopBuilder.OriginalDeliveryShopsCount = countBefore;
            Logger.Msg(
                $"Set OriginalDeliveryShopsCount = {DeliveryShopBuilder.OriginalDeliveryShopsCount} (deliveryShops.Count = {countBefore})");

            foreach (var shop in ShopRegistry.Shops)
            {
                try
                {
                    Logger.Msg($"Creating shop: {shop.GetType().Name}");
                    shop.CreateShop(app);
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"Failed to create shop: {ex.Message}");
                }
            }

            foreach (var shop in ShopRegistry.Shops)
            {
                var shopName = shop.GetShopName();
                var customShop = app.deliveryShops.AsEnumerable().FirstOrDefault(ds =>
                    ds != null && ds.gameObject.name.Contains(shopName));

                if (customShop == null || customShop.MatchingShop == null) continue;

                if (app._shopElements.AsEnumerable().Any(e => e?.Shop == customShop))
                {
                    TryInitializeShop(customShop);
                    continue;
                }

                CreateShopButton(app, customShop);
                TryInitializeShop(customShop);
            }
        }

        private static void CreateShopButton(DeliveryApp app, DeliveryShop customShop)
        {
            if (app._shopElements == null || app._shopElements.Count == 0) return;

            var firstElement = app._shopElements[0];
            if (firstElement?.Button?.gameObject == null) return;

            var templateButton = firstElement.Button.gameObject;
            var buttonParent = templateButton.transform.parent;

            var buttonObj = Object.Instantiate(templateButton, buttonParent);
            buttonObj.name = $"{customShop.MatchingShop.ShopName}Button";
            buttonObj.SetActive(true);

            var shopIndex = app.deliveryShops.IndexOf(customShop);
            if (shopIndex >= 0)
                buttonObj.transform.SetSiblingIndex(shopIndex);

            foreach (var text in buttonObj.GetComponentsInChildren<Text>())
            {
                text.text = text.gameObject.name switch
                {
                    "Title" => customShop.MatchingShop.ShopName,
                    "Description" => customShop.MatchingShop.ShopDescription,
                    _ => text.text
                };
            }

            foreach (var img in buttonObj.GetComponentsInChildren<Image>())
                if (img.gameObject.name == "Image")
                    img.sprite = Registries.GetShopImage(customShop);

            if (Utils.Is<Image>(buttonObj.GetComponent<Image>(), out var bg))
                bg.color = customShop.ShopColor;

            var btnComp = buttonObj.GetComponent<Button>();
            if (btnComp != null)
            {
                btnComp.onClick.RemoveAllListeners();
                btnComp.onClick.AddListener((UnityAction)(() => app.OpenShop(customShop)));
                customShop.OnSelect += (Action<DeliveryShop>)(app.CloseShop);
            }

            var insertIndex = shopIndex >= 0 && shopIndex <= app._shopElements.Count
                ? shopIndex
                : app._shopElements.Count;
            app._shopElements.Insert(insertIndex,
                new DeliveryApp.DeliveryShopElement { Shop = customShop, Button = btnComp });
        }

        private static void TryInitializeShop(DeliveryShop shop)
        {
            if (shop == null) return;
            if (shop.MatchingShop != null && shop.ListingContainer != null && shop.listingEntries.Count > 0)
            {
                Logger.Debug($"Shop already initialized: {shop.name}");
                return;
            }

            Logger.Debug(
                $"Setup shop - MatchingShop: {shop.MatchingShop?.ShopName}, ListingContainer: {shop.ListingContainer?.name}, Entries: {shop.listingEntries.Count}");

            try
            {
                if (shop.MatchingShop == null)
                {
#if MONO
                    shop.MatchingShop =
 ShopInterface.AllShops.Find((x) => x.ShopName == shop.MatchingShopInterfaceName);
#else
                    shop.MatchingShop = ShopInterface.AllShops.Find(
                        (Il2CppSystem.Predicate<ShopInterface>)((ShopInterface x) =>
                            x.ShopName == shop.MatchingShopInterfaceName));
#endif
                }

                if (shop.ListingContainer == null)
                {
                    Logger.Error($"ListingContainer is null for {shop.name}!");
                    return;
                }

                if (shop.listingEntries.Count == 0)
                {
                    Logger.Warning($"No listing entries for {shop.name}, creating them now...");
                    foreach (var listing in shop.MatchingShop?.Listings ?? new List<ShopListing>()
#if !MONO
                                 .ToIl2CppList()
#endif
                            )
                    {
                        if (listing.CanBeDelivered && shop.ListingEntryPrefab != null)
                        {
                            var entry = UnityEngine.Object.Instantiate(shop.ListingEntryPrefab, shop.ListingContainer);
                            entry.Initialize(listing);
                            entry.onQuantityChanged.AddListener((UnityAction)(shop.RefreshCart));
                            shop.listingEntries.Add(entry);
                        }
                    }
                }

                shop.BackButton?.onClick.RemoveAllListeners();
                shop.BackButton?.onClick.AddListener((UnityAction)(() => shop.OnSelect?.Invoke(shop)));

                shop.OrderButton?.onClick.RemoveAllListeners();
                shop.OrderButton?.onClick.AddListener((UnityAction)(() => shop.SubmitOrder(string.Empty)));

                shop.DestinationDropdown?.onValueChanged.RemoveAllListeners();
                shop.DestinationDropdown?.onValueChanged.AddListener(
                    (UnityAction<int>)(shop.DestinationDropdownSelected));

                shop.LoadingDockDropdown?.onValueChanged.RemoveAllListeners();
                shop.LoadingDockDropdown?.onValueChanged.AddListener(
                    (UnityAction<int>)(shop.LoadingDockDropdownSelected));

                shop.gameObject.SetActive(false);

                Logger.Debug($"Setup succeeded for: {shop.name}");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Setup failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(DeliveryApp), nameof(DeliveryApp.SetOpen))]
    public class SetOpen
    {
        public static void Postfix(DeliveryApp __instance, bool open)
        {
            if (__instance == null || !open) return;
            foreach (var shop in __instance.deliveryShops)
            {
                if (shop?.DeliveryFeeLabel == null) continue;
                if (!DeliveryShopBuilder.DeliveryFeeRegistry.TryGetValue(shop, out var fee)) continue;
                shop.DeliveryFeeLabel.text = MoneyManager.FormatAmount(fee);
            }
        }
    }
}