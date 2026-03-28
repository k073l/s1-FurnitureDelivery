using System.Collections;
using System.Linq;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using FurnitureDelivery.Shops;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;


#if MONO
using Guid = System.Guid;
using TMPro;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.Vehicles;
using ScheduleOne.Weather;

#else
using Guid = Il2CppSystem.Guid;
using Il2CppTMPro;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Weather;
#endif

namespace FurnitureDelivery;

[HarmonyPatch(typeof(DeliveryApp), "SetIsAvailable")]
public class DeliveryShopSetIsAvailablePatch
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-SetIsAvailable");

    public static void Postfix(DeliveryShop __instance)
    {
        if (__instance?.gameObject.name == null) return;
        var app = DeliveryApp.Instance;
        var shops = app?.deliveryShops;

        var oscarShop = shops?.AsEnumerable().FirstOrDefault(item =>
            item != null && item.gameObject.name.StartsWith("Oscar"));

        if (oscarShop == null)
            return;

        if (oscarShop?.MatchingShop == null) return;
        var oscarAvailable = oscarShop.MatchingShop.gameObject.activeSelf;

        if (__instance.gameObject.name != oscarShop.gameObject.name)
            return;

        Logger.Msg($"First Oscar's shop: {oscarShop.gameObject.name} set to active, setting other one to active");

        var oscarEquipment = shops.AsEnumerable().FirstOrDefault(item =>
            item != null && item.gameObject.name.StartsWith("DeliveryShop_Oscar"));

        if (oscarEquipment == null)
        {
            Logger.Warning("Oscar's equipment shop not found");
            return;
        }

        var oscarElement = app?._shopElements?.AsEnumerable().FirstOrDefault(item => item?.Shop == oscarEquipment);
        oscarElement?.Button?.gameObject?.SetActive(oscarAvailable);

        // now stan
        var stanShop = shops.AsEnumerable().FirstOrDefault(item =>
            item != null && item.gameObject.name.StartsWith("DeliveryShop_Armory"));
        if (stanShop == null)
        {
            Logger.Warning("Stan's shop not found");
            return;
        }

        var stanElement = app?._shopElements?.AsEnumerable().FirstOrDefault(item => item?.Shop == stanShop);
        stanElement?.Button?.gameObject?.SetActive(oscarAvailable);
    }
}

[HarmonyPatch(typeof(DeliveryApp), "Awake")]
public class DeliveryAppAwakePatch
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-AppAwake");
    public static bool AddedShops = false;

    public static List<ICustomShop> shops =
    [
        new DanShop(),
        new HerbertShop(),
        new OscarShop(),
        new StanShop()
    ];

    public static void Prefix(DeliveryApp __instance)
    {
        Logger.Debug("DeliveryApp.Awake Prefix START");
        Logger.Debug($"AddedShops: {AddedShops}");
        Logger.Debug($"AllShops count: {ShopInterface.AllShops.Count}");

        if (AddedShops)
        {
            Logger.Debug("Already added shops, skipping");
            return;
        }

        Logger.Debug("DeliveryApp.Awake Prefix END");
    }
}

[HarmonyPatch(typeof(ShopInterface), "Awake")]
public static class ShopInterfaceAwakePatch
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-ShopIntAwake");

    [HarmonyPrefix]
    public static void Prefix(ShopInterface __instance)
    {
        Logger.Debug($"ShopInterface.Awake START: {__instance.ShopName}");
        Logger.Debug(
            $"  ListingUIPrefab: {(__instance.ListingUIPrefab != null ? __instance.ListingUIPrefab.name : "NULL")}");
        Logger.Debug(
            $"  ListingContainer: {(__instance.ListingContainer != null ? __instance.ListingContainer.name : "NULL")}");
        Logger.Debug($"  AllShops count: {ShopInterface.AllShops.Count}");
    }

    [HarmonyFinalizer]
    public static void Finalizer(ShopInterface __instance)
    {
        Logger.Debug($"ShopInterface.Awake END: {__instance.ShopName}");
    }
}

[HarmonyPatch(typeof(DeliveryApp), "Start")]
public class DeliveryAppStartPatch
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-AppStart");
    public static bool Initialized = false;

    private static readonly Dictionary<string, string> ShopNameToPattern = new()
    {
        { "DanShop", "DeliveryShop_Dan's Furniture" },
        { "HerbertShop", "DeliveryShop_Herbert" },
        { "OscarShop", "DeliveryShop_Oscar" },
        { "StanShop", "DeliveryShop_Armory" }
    };

    public static void Postfix(DeliveryApp __instance)
    {
        if (Initialized) return;
        Initialized = true;

        var app = __instance;

        foreach (var shop in DeliveryAppAwakePatch.shops)
        {
            try
            {
                shop.CreateShop(app);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to create shop: {ex.Message}");
            }
        }

        foreach (var shopBuilder in DeliveryAppAwakePatch.shops)
        {
            if (!ShopNameToPattern.TryGetValue(shopBuilder.GetType().Name, out var shopName))
                continue;

            var customShop = app.deliveryShops.AsEnumerable().FirstOrDefault(ds =>
                ds != null &&
                ds.gameObject != null &&
                ds.gameObject.name.Contains(shopName));

            if (customShop == null || customShop.MatchingShop == null)
                continue;

            if (app._shopElements.AsEnumerable().Any(e => e?.Shop == customShop))
            {
                InitializeDeliveryShop(customShop);
                continue;
            }

            // Find template button from existing shop elements
            GameObject templateButton = null;
            Transform buttonParent = null;

            if (app._shopElements != null && app._shopElements.Count > 0)
            {
                var firstElement = app._shopElements[0];
                if (firstElement?.Button?.gameObject != null)
                {
                    templateButton = firstElement.Button.gameObject;
                    buttonParent = templateButton.transform.parent;
                }
            }

            if (templateButton != null && buttonParent != null)
            {
                // Clone template button
                var buttonObj = UnityEngine.Object.Instantiate(templateButton, buttonParent);
                buttonObj.name = $"{customShop.MatchingShop.ShopName}Button";
                buttonObj.SetActive(true);

                // Set sibling index to match position in deliveryShops list
                var shopIndex = app.deliveryShops.IndexOf(customShop);
                if (shopIndex >= 0)
                    buttonObj.transform.SetSiblingIndex(shopIndex);

                // Update button text
                var texts = buttonObj.GetComponentsInChildren<Text>();
                foreach (var text in texts)
                {
                    switch (text.gameObject.name)
                    {
                        case "Title":
                            text.text = customShop.MatchingShop.ShopName;
                            break;
                        case "Description":
                            text.text = customShop.MatchingShop.ShopDescription;
                            break;
                    }
                }

                var images = buttonObj.GetComponentsInChildren<Image>();
                foreach (var img in images)
                    if (img.gameObject.name == "Image")
                        img.sprite = ShopImageRegistry.Images.GetValueSafe(customShop);

                var bg = buttonObj.GetComponent<Image>();
                if (bg != null)
                    bg.color = customShop.ShopColor;

                var btnComp = buttonObj.GetComponent<Button>();
                if (btnComp != null)
                {
                    btnComp.onClick.RemoveAllListeners();
                    btnComp.onClick.AddListener((UnityAction)(() => app.OpenShop(customShop)));
                    customShop.OnSelect += (Action<DeliveryShop>)(app.CloseShop);
                }

                // Add to shop elements at correct position
                var insertIndex = shopIndex >= 0 && shopIndex <= app._shopElements.Count
                    ? shopIndex
                    : app._shopElements.Count;
                app._shopElements.Insert(insertIndex, new DeliveryApp.DeliveryShopElement
                {
                    Shop = customShop,
                    Button = btnComp
                });
            }

            InitializeDeliveryShop(customShop);
        }
    }

    private static void InitializeDeliveryShop(DeliveryShop shop)
    {
        if (shop == null) return;

        // Skip if already properly initialized (Build() should have called Initialize)
        if (shop.MatchingShop != null && shop.ListingContainer != null)
        {
            Logger.Debug($"  Shop already initialized: {shop.gameObject.name}");
            return;
        }

        try
        {
            // Log state before initialization
            Logger.Debug(
                $"  Before Init - Name: {shop.gameObject.name}, MatchingShop: {shop.MatchingShop?.ShopName ?? "NULL"}, MatchingShopInterfaceName: {shop.MatchingShopInterfaceName}");

            shop.Initialize();

            // Log state after initialization
            Logger.Msg($"  Initialized: {shop.gameObject.name}, MatchingShop: {shop.MatchingShop?.ShopName ?? "NULL"}");
        }
        catch (System.Exception initEx)
        {
            Logger.Error($"  Initialize failed: {initEx.Message}");
            Logger.Error($"  Stack: {initEx.StackTrace}");
        }
    }
}

[HarmonyPatch(typeof(VehicleCamera))]
public static class VehicleCameraPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("LateUpdate")]
    public static bool SafeLateUpdatePrefix(VehicleCamera __instance)
    {
        return __instance.vehicle != null &&
               __instance.cameraOrigin != null &&
               PlayerSingleton<PlayerCamera>.Instance != null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    public static bool SafeUpdatePrefix(VehicleCamera __instance)
    {
        return __instance.vehicle != null &&
               __instance.cameraOrigin != null &&
               PlayerSingleton<PlayerCamera>.Instance != null;
    }
}

[HarmonyPatch(typeof(DeliveryVehicle), "Deactivate")]
public static class DeliveryVehicleDeactivatePatch
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-VehicleDeactivate");

    public static bool Prefix(DeliveryVehicle __instance)
    {
        if (__instance == null) return true;
        if (__instance.Vehicle == null) return true;
        if (__instance.ActiveDelivery?.Status == EDeliveryStatus.Completed) return true;

        var vehicleName = __instance.Vehicle?.name;
        if (string.IsNullOrEmpty(vehicleName)) return true;

        string name = null;
        if (vehicleName.Contains("Dan"))
            name = "Dan";
        else if (vehicleName.Contains("Oscar"))
            name = "Oscar";

        if (!string.IsNullOrEmpty(name))
        {
            var shops = DeliveryApp.Instance?.deliveryShops;
            if (shops == null)
            {
                return true;
            }

            var manager = DeliveryManager.Instance;
            if (manager == null)
            {
                Logger.Debug("NetworkSingleton<DeliveryManager>.Instance is null");
                return true;
            }

            foreach (var shop in shops)
            {
                var active = manager.GetActiveShopDelivery(shop) != null;
                if (active)
                {
                    Logger.Warning($"{name} is currently delivering an order, not deactivating the vehicle");
                    return false;
                }
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(ListingUI))]
public static class ListingUICanAddToCartPatch
{
    [HarmonyPatch(nameof(ListingUI.CanAddToCart))]
    [HarmonyPrefix]
    public static bool PrefixCanAddToCart(ListingUI __instance, ref bool __result)
    {
        if (__instance.Listing == null)
        {
            __result = false;
            return false;
        }

        return true;
    }

    [HarmonyPatch(nameof(ListingUI.UpdateButtons))]
    [HarmonyPrefix]
    public static bool PrefixUpdateButtons(ListingUI __instance)
    {
        if (__instance == null) return false;
        if (__instance.BuyButton == null) return false;
        if (!__instance.BuyButton.isActiveAndEnabled) return false;
        if (__instance.DropdownButton == null) return false;
        if (!__instance.DropdownButton.isActiveAndEnabled) return false;
        if (__instance.Listing == null) return false;
        return true;
    }
}

[HarmonyPatch(typeof(DeliveryShop), nameof(DeliveryShop.RefreshCart))]
public static class DeliveryShopRefreshCartPatch
{
    [HarmonyPrefix]
    public static bool PrefixRefreshCart(DeliveryShop __instance)
    {
        Melon<FurnitureDelivery>.Logger.Debug($"[REFRESH CART] Called on {__instance.gameObject.name}");

        try
        {
            if (__instance.ItemTotalLabel == null)
            {
                Melon<FurnitureDelivery>.Logger.Warning(
                    $"RefreshCart: ItemTotalLabel is null on {__instance.gameObject.name}");
                return false;
            }

            if (__instance.OrderTotalLabel == null)
            {
                Melon<FurnitureDelivery>.Logger.Warning(
                    $"RefreshCart: OrderTotalLabel is null on {__instance.gameObject.name}");
                return false;
            }

            if (__instance.DeliveryTimeLabel == null)
            {
                Melon<FurnitureDelivery>.Logger.Warning(
                    $"RefreshCart: DeliveryTimeLabel is null on {__instance.gameObject.name}");
                return false;
            }

            Melon<FurnitureDelivery>.Logger.Debug(
                $"[REFRESH CART] Labels OK - ItemTotal: '{__instance.ItemTotalLabel.text}'");
        }
        catch (System.Exception ex)
        {
            Melon<FurnitureDelivery>.Logger.Error($"RefreshCart prefix check failed: {ex.Message}");
        }

        return true;
    }
}

[HarmonyPatch(typeof(Wheel))]
internal class WheelPatch
{
    [HarmonyPatch(nameof(Wheel.OnWeatherChange))]
    [HarmonyPrefix]
    private static bool ExitIfNull(Wheel __instance, WeatherConditions newConditions)
    {
        if (__instance?.vehicle == null) return false;
        if (newConditions?.Rainy == null) return false;
        return true;
    }
}

[HarmonyPatch(typeof(DeliveryVehicle))]
internal static class DeliveryVehicleAwakePatch
{
    [HarmonyPatch(nameof(DeliveryVehicle.Awake))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool ExitIfNull(DeliveryVehicle __instance)
    {
        // skip guid setting if invalid
        if (Guid.TryParse(__instance.GUID, out var _)) return true;
        if (__instance.GetComponent<LandVehicle>() == null) return false;
        __instance.Vehicle = __instance.GetComponent<LandVehicle>();
        __instance.Deactivate();
        return false;
    }
}

[HarmonyPatch(typeof(ShopInterface))]
internal static class ShopInterfacePatch
{
    [HarmonyPatch(nameof(ShopInterface.Awake))]
    [HarmonyPrefix]
    private static void EnsureInAllShops(ShopInterface __instance)
    {
        if (__instance == null) return;

        // Ensure this ShopInterface is in AllShops list
        try
        {
            if (!ShopInterface.AllShops.Contains(__instance))
            {
                ShopInterface.AllShops.Add(__instance);
                Melon<FurnitureDelivery>.Logger.Debug($"Added ShopInterface to AllShops: {__instance.ShopName}");
            }
        }
        catch (System.Exception ex)
        {
            Melon<FurnitureDelivery>.Logger.Error($"Failed to add ShopInterface to AllShops: {ex.Message}");
        }
    }

    [HarmonyPatch(nameof(ShopInterface.RefreshShownItems))]
    [HarmonyPrefix]
    private static bool ExitIfUINull(ShopInterface __instance)
    {
        if (__instance?.listingUI == null || __instance.DetailPanel == null) return false;
        return true;
    }

    [HarmonyPatch(nameof(ShopInterface.Start))]
    [HarmonyPrefix]
    private static void AddMissingMembers(ShopInterface __instance)
    {
        if (__instance.Canvas == null)
            __instance.Canvas = __instance.GetComponent<Canvas>() ?? __instance.gameObject.AddComponent<Canvas>();
        if (__instance.Container == null)
            __instance.Container = __instance.GetComponent<RectTransform>() ??
                                   __instance.gameObject.AddComponent<RectTransform>();
    }
}

[HarmonyPatch(typeof(DeliveryShop))]
internal class DeliveryShopGetDeliveryFeePatch
{
    [HarmonyPatch("GetDeliveryFee")]
    [HarmonyPrefix]
    private static bool PrefixGetDeliveryFee(DeliveryShop __instance, ref float __result)
    {
        if (DeliveryShopBuilder.DeliveryFeeRegistry.TryGetValue(__instance, out var customFee))
        {
            __result = customFee;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(DeliveryShop), nameof(DeliveryShop.CanOrder))]
internal class DeliveryShopConflictCheckPatch
{
    [HarmonyPrefix]
    internal static bool PrefixCanOrder(DeliveryShop __instance, ref bool __result, ref string reason)
    {
        if (__instance == null)
        {
            __result = false;
            reason = "Shop is null";
            return false;
        }

        var shopName = __instance.gameObject.name;

        if (shopName.Contains("Herbert"))
        {
            __result = ToileportationInterop.CanOrder(
                DeliveryApp.Instance._shopElements.AsEnumerable().Select(se => se.Shop).ToList(), out reason);
            if (!__result)
            {
                __result = false;
                return false;
            }
        }

        if (!CheckForName(shopName, "Dan", ref __result, out reason))
            return false;

        if (!CheckForName(shopName, "Oscar", ref __result, out reason))
            return false;

        return true;
    }

    private static bool CheckForName(string shopName, string name, ref bool __result, out string reason)
    {
        if (shopName.Contains(name))
        {
            foreach (var shop in DeliveryApp.Instance.deliveryShops)
            {
                if (shop == null) continue;
                if (!shop.gameObject.name.Contains(name)) continue;
                var active = DeliveryManager.Instance.GetActiveShopDelivery(shop) != null;
                if (!active) continue;
                __result = false;
                reason = $"{name} is currently delivering an order";
                return false;
            }
        }

        reason = "";
        return true;
    }
}