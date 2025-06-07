using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using HarmonyLib;
using MelonLoader;


#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.Vehicles;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.Vehicles;
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

    public static void Postfix(DeliveryApp __instance)
    {
        Logger.Debug("DeliveryApp Awake called");

        var app = PlayerSingleton<DeliveryApp>.Instance;

        Shops.DanShop.CreateDanShop(app);
        Shops.HerbertShop.CreateHerbertShop(app);
        Shops.OscarShop.CreateOscarShop(app);
        Shops.StanShop.CreateStanShop(app);
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