using System.Collections;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using HarmonyLib;
using MelonLoader;
using UnityEngine;


#if MONO
using Guid = System.Guid;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.Vehicles;
using ScheduleOne.Weather;
#else
using Guid = Il2CppSystem.Guid;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Weather;
#endif

namespace FurnitureDelivery;

[HarmonyPatch(typeof(DeliveryShop), "SetIsAvailable")]
public class DeliveryShopSetIsAvailablePatch
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-SetIsAvailable");

    public static void Postfix(DeliveryShop __instance)
    {
        var app = PlayerSingleton<DeliveryApp>.Instance;
        var shops = DeliveryShopBuilder.GetInitializedShops(app, out _);

        var oscarShop = shops?.FirstOrDefault(item =>
            item.gameObject.name.StartsWith("Oscar"));

        if (oscarShop == null)
            return;

        if (__instance.gameObject.name != oscarShop.gameObject.name)
            return;

        Logger.Msg($"First Oscar's shop: {oscarShop.gameObject.name} set to active, setting other one to active");

        var oscarEquipment = shops.FirstOrDefault(item =>
            item.gameObject.name.StartsWith("DeliveryShop_Oscar"));

        if (oscarEquipment == null)
        {
            Logger.Warning("Oscar's equipment shop not found");
            return;
        }

        oscarEquipment.gameObject.SetActive(true);

        // now stan
        var stanShop = shops.FirstOrDefault(item =>
            item.gameObject.name.StartsWith("DeliveryShop_Armory"));
        if (stanShop == null)
        {
            Logger.Warning("Stan's shop not found");
            return;
        }

        stanShop.gameObject.SetActive(true);
    }
}

class InitializedShopsCache
{
    public static Dictionary<string, DeliveryShop> shops = new();

    public static DeliveryShop GetShop(string name)
    {
        if (!shops.ContainsKey(name))
        {
            var app = PlayerSingleton<DeliveryApp>.Instance;
            if (app == null)
            {
                Melon<FurnitureDelivery>.Logger.Debug("DeliveryApp is null, cannot get shops");
                return null;
            }
            var list = DeliveryShopBuilder.GetInitializedShops(app, out _);
            foreach (var shop in list)
                shops.TryAdd(shop.gameObject.name, shop);
        }

        return shops.TryGetValue(name, out var result) ? result : null;
    }

    public static List<DeliveryShop> GetShops(string name)
    {
        // Ensure initialized
        if (!shops.Any())
            GetShop(name);

        return shops
            .Where(kvp => kvp.Key.Contains(name))
            .Select(kvp => kvp.Value)
            .ToList();
    }
}

[HarmonyPatch(typeof(DeliveryShop), "CanOrder")]
public class DeliveryShopCanOrderPatch
{
    public static bool HandleAddedShops(DeliveryShop shop, out string reason, ref bool result)
    {
        var shopName = shop.gameObject.name;

        // If Herbert's, pass to ToileportationInterop
        if (shopName.Contains("Herbert"))
        {
            result = ToileportationInterop.CanOrder(InitializedShopsCache.GetShops("Herbert"), out reason);
            if (!result)
            {
                result = false;
                return false;
            }
        }

        if (shopName.Contains("Dan"))
        {
            var danShops = InitializedShopsCache.GetShops("Dan");

            foreach (var danShop in danShops)
            {
                var active = NetworkSingleton<DeliveryManager>.Instance.GetActiveShopDelivery(danShop) != null;
                if (active)
                {
                    result = false;
                    reason = "Dan is currently delivering an order";
                    return false;
                }
            }
        }

        if (shopName.Contains("Oscar"))
        {
            var oscarShops = InitializedShopsCache.GetShops("Oscar");

            foreach (var oscarShop in oscarShops)
            {
                var active = NetworkSingleton<DeliveryManager>.Instance.GetActiveShopDelivery(oscarShop) != null;
                if (active)
                {
                    result = false;
                    reason = "Oscar is currently delivering an order";
                    return false;
                }
            }
        }

        reason = string.Empty;
        return true;
    }

    public static bool Prefix(DeliveryShop __instance, out string reason, ref bool __result)
    {
        return
            HandleAddedShops(__instance, out reason,
                ref __result); // run checks for added shops, if none fail, proceed with base game checks
    }

    public static void ApplyManualPatch()
    {
        var harmony = Melon<FurnitureDelivery>.Instance.HarmonyInstance;

        if (!harmony.GetPatchedMethods().Contains(AccessTools.Method(typeof(DeliveryShop), "CanOrder")))
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(DeliveryShop), "CanOrder"),
                prefix: new HarmonyMethod(typeof(DeliveryShopCanOrderPatch), "Prefix")
            );
        }
    }
}

[HarmonyPatch(typeof(DeliveryApp), "Awake")]
public class DeliveryAppAwakePatch
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-AppAwake");
    public static bool AddedShops = false;

    public static void Postfix(DeliveryApp __instance)
    {
        Logger.Debug("DeliveryApp Awake called");
        if (AddedShops) return;
        var app = PlayerSingleton<DeliveryApp>.Instance;
        Shops.DanShop.CreateDanShop(app);
        Shops.HerbertShop.CreateHerbertShop(app);
        Shops.OscarShop.CreateOscarShop(app);
        Shops.StanShop.CreateStanShop(app);
        AddedShops = true;
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
            var shops = InitializedShopsCache.GetShops(name);
            if (shops == null)
            {
                Logger.Debug($"InitializedShopsCache returned null for {name}");
                return true;
            }

            var manager = NetworkSingleton<DeliveryManager>.Instance;
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
        if (__instance.BuyButton.isActiveAndEnabled == false) return false;
        if (__instance.DropdownButton == null) return false;
        if (__instance.DropdownButton.isActiveAndEnabled == false) return false;
        if (__instance.Listing == null) return false;
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
    private static bool ExitIfNull(ShopInterface __instance)
    {
        if (__instance?.ListingScrollRect == null ||
            __instance.StoreNameLabel == null ||
            __instance.ListingContainer == null ||
            __instance.AmountSelector == null ||
            __instance.ListingUIPrefab == null ||
            __instance.listingPanel == null ||
            __instance.listingPanel == null
            )
        {
            ShopInterface.AllShops.Add(__instance);
            return false;
        }
        return true;
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
            __instance.Container = __instance.GetComponent<RectTransform>() ?? __instance.gameObject.AddComponent<RectTransform>();
    }
}