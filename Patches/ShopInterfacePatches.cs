using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

#if MONO
using ScheduleOne.UI.Shop;
#else
using Il2CppScheduleOne.UI.Shop;
#endif

namespace FurnitureDelivery.Patches;

[HarmonyPatch]
public static class ShopInterfacePatches
{

    [HarmonyPatch(typeof(ShopInterface), nameof(ShopInterface.RefreshShownItems))]
    public static class RefreshShownItemsPatch
    {
        public static bool Prefix(ShopInterface __instance) =>
            __instance?.listingUI != null && __instance.DetailPanel != null;
    }

    [HarmonyPatch(typeof(ShopInterface), nameof(ShopInterface.Start))]
    public static class ShopInterfaceStartPatch
    {
        public static void Prefix(ShopInterface __instance)
        {
            if (__instance.Canvas == null)
                __instance.Canvas = __instance.GetComponent<Canvas>() ?? __instance.gameObject.AddComponent<Canvas>();
            if (__instance.Container == null)
                __instance.Container = __instance.GetComponent<RectTransform>() ?? __instance.gameObject.AddComponent<RectTransform>();
        }
    }
}
