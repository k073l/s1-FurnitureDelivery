using FurnitureDelivery.Helpers;
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
    public static void Postfix(DeliveryShop __instance)
    {
        var app = PlayerSingleton<DeliveryApp>.Instance;
        var shops = Utils.GetInitializedShops(app, out _);

        var oscarShop = shops?.FirstOrDefault(item =>
            item.gameObject.name.StartsWith("Oscar"));

        if (oscarShop == null)
        {
            return;
        }

        if (__instance.gameObject.name != oscarShop.gameObject.name)
            return;

        MelonLogger.Msg(
            $"SetIsAvailable called on first Oscar's shop: {oscarShop.gameObject.name}, setting other one to active");

        var oscarEquipment = shops.FirstOrDefault(item =>
            item.gameObject.name.StartsWith("DeliveryShop_Oscar"));

        if (oscarEquipment == null)
        {
            MelonLogger.Warning("Oscar's equipment shop not found");
            return;
        }

        oscarEquipment.gameObject.SetActive(true);
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
            var list = Utils.GetInitializedShops(app, out _);
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
}


[HarmonyPatch(typeof(DeliveryApp), "Awake")]
public class DeliveryAppAwakePatch
{
    public static void Postfix(DeliveryApp __instance)
    {
        MelonLogger.Msg("DeliveryApp Awake called");

        var app = PlayerSingleton<DeliveryApp>.Instance;

        Shops.DanShop.CreateDanShop(app);
        Shops.HerbertShop.CreateHerbertShop(app);
        Shops.OscarShop.CreateOscarShop(app);
    }
}