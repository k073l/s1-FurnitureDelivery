
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using HarmonyLib;
using MelonLoader;


#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.Delivery;

#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Delivery;
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
    public static bool Prefix(DeliveryShop __instance, out string reason, ref bool __result)
    {
        var shopName = __instance.gameObject.name;
        
        // If Herbert's, pass to ToileportationInterop
        if (shopName.Contains("Herbert"))
        {
            __result = ToileportationInterop.CanOrder(InitializedShopsCache.GetShops("Herbert"), out reason);
            if (!__result)
            {
                __result = false;
                return false;
            }
        }

        // If it's not Dan or Oscar, allow default behavior
        if (!shopName.Contains("Dan") && !shopName.Contains("Oscar"))
        {
            reason = string.Empty;
            return true;
        }

        if (shopName.Contains("Dan"))
        {
            var danShops = InitializedShopsCache.GetShops("Dan");

            foreach (var shop in danShops)
            {
                bool active = NetworkSingleton<DeliveryManager>.Instance.GetActiveShopDelivery(shop) != null;
                if (active)
                {
                    __result = false;
                    reason = "Dan is currently delivering an order";
                    return false;
                }
            }
        }

        if (shopName.Contains("Oscar"))
        {
            var oscarShops = InitializedShopsCache.GetShops("Oscar");

            foreach (var shop in oscarShops)
            {
                bool active = NetworkSingleton<DeliveryManager>.Instance.GetActiveShopDelivery(shop) != null;
                if (active)
                {
                    __result = false;
                    reason = "Oscar is currently delivering an order";
                    return false;
                }
            }
        }

        reason = string.Empty;
        return true;
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