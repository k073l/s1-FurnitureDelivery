using HarmonyLib;
using UnityEngine.UI;

#if MONO
using ScheduleOne.UI.Shop;
#else
using Il2CppScheduleOne.UI.Shop;
#endif

namespace FurnitureDelivery.Patches;

[HarmonyPatch]
public static class ListingUIPatches
{
    [HarmonyPatch(typeof(ListingUI))]
    public static class ListingUIPatch
    {
        [HarmonyPatch(nameof(ListingUI.CanAddToCart))]
        public static bool CanAddToCart(ListingUI __instance, ref bool __result)
        {
            if (__instance.Listing == null) { __result = false; return false; }
            return true;
        }

        [HarmonyPatch(nameof(ListingUI.UpdateButtons))]
        public static bool UpdateButtons(ListingUI __instance)
        {
            if (__instance == null) return false;
            if (__instance.BuyButton == null || !__instance.BuyButton.isActiveAndEnabled) return false;
            if (__instance.DropdownButton == null || !__instance.DropdownButton.isActiveAndEnabled) return false;
            if (__instance.Listing == null) return false;
            return true;
        }
    }
}
