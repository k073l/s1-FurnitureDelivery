using System.Linq;
using FurnitureDelivery.Builders;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using HarmonyLib;
using MelonLoader;

#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
#endif

namespace FurnitureDelivery.Patches;

[HarmonyPatch]
public static class DeliveryShopPatches
{
    [HarmonyPatch(typeof(DeliveryShop), nameof(DeliveryShop.RefreshCart))]
    public class RefreshCart
    {
        public static bool Prefix(DeliveryShop __instance)
        {
            if (__instance.ItemTotalLabel == null || __instance.OrderTotalLabel == null || __instance.DeliveryTimeLabel == null)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(DeliveryShop), nameof(DeliveryShop.RefreshOrderButton))]
    public class RefreshOrderButton
    {
        public static bool Prefix(DeliveryShop __instance)
        {
            if (__instance.OrderButton == null) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(DeliveryShop), nameof(DeliveryShop.RefreshDestinationUI))]
    public class RefreshDestinationUI
    {
        public static bool Prefix(DeliveryShop __instance)
        {
            if (__instance.DestinationDropdown == null || __instance.LoadingDockDropdown == null) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(DeliveryShop), nameof(DeliveryShop.RefreshLoadingDockUI))]
    public class RefreshLoadingDockUI
    {
        public static bool Prefix(DeliveryShop __instance)
        {
            if (__instance.LoadingDockDropdown == null) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(DeliveryShop), "GetDeliveryFee")]
    public class GetDeliveryFee
    {
        public static bool Prefix(DeliveryShop __instance, ref float __result)
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
    public class CanOrder
    {
        public static bool Prefix(DeliveryShop __instance, ref bool __result, ref string reason)
        {
            if (__instance == null) { __result = false; reason = "Shop is null"; return false; }

            var shopName = __instance.gameObject.name;

            if (shopName.Contains("Herbert"))
            {
                __result = ToileportationInterop.CanOrder(
                    DeliveryApp.Instance._shopElements.AsEnumerable().Select(e => e.Shop).ToList(), out reason);
                if (!__result) return false;
            }

            if (!CheckShopConflict(shopName, "Dan", ref __result, out reason)) return false;
            if (!CheckShopConflict(shopName, "Oscar", ref __result, out reason)) return false;

            return true;
        }

        private static bool CheckShopConflict(string shopName, string name, ref bool __result, out string reason)
        {
            if (!shopName.Contains(name)) { reason = ""; return true; }

            foreach (var shop in DeliveryApp.Instance.deliveryShops)
            {
                if (shop == null || !shop.gameObject.name.Contains(name)) continue;
                if (DeliveryManager.Instance.GetActiveShopDelivery(shop) != null)
                {
                    __result = false;
                    reason = $"{name} is currently delivering an order";
                    return false;
                }
            }

            reason = "";
            return true;
        }
    }
}
