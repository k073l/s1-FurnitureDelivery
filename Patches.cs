using System.Collections;
using System.Linq;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
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
using ScheduleOne.ItemFramework;

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
using Il2CppScheduleOne.ItemFramework;
#endif

namespace FurnitureDelivery;

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
    public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-ShopInterfacePatch");

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