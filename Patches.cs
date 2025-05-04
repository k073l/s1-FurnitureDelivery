using FurnitureDelivery.Helpers;
using HarmonyLib;
using MelonLoader;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.Delivery;
#else
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

[HarmonyPatch(typeof(DeliveryApp), "Awake")]
public class DeliveryAppAwakePatch
{
    public static void Postfix(DeliveryApp __instance)
    {
        MelonLogger.Msg("DeliveryApp Awake called");

        var app = PlayerSingleton<DeliveryApp>.Instance;

        Shops.DanShop.CreateDanShop(app);
        Shops.OscarShop.CreateOscarShop(app);
    }
}