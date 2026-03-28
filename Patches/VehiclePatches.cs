using HarmonyLib;
using MelonLoader;

#if MONO
using Guid = System.Guid;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.Vehicles;
#else
using Guid = Il2CppSystem.Guid;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.Vehicles;
#endif

namespace FurnitureDelivery.Patches;

[HarmonyPatch]
public static class VehiclePatches
{
    [HarmonyPatch(typeof(VehicleCamera))]
    public static class VehicleCameraPatch
    {
        [HarmonyPatch("LateUpdate")]
        public static bool LateUpdate(VehicleCamera __instance) =>
            __instance.vehicle != null && __instance.cameraOrigin != null && PlayerSingleton<PlayerCamera>.Instance != null;

        [HarmonyPatch("Update")]
        public static bool Update(VehicleCamera __instance) =>
            __instance.vehicle != null && __instance.cameraOrigin != null && PlayerSingleton<PlayerCamera>.Instance != null;
    }

    [HarmonyPatch(typeof(DeliveryVehicle), "Deactivate")]
    public static class DeliveryVehicleDeactivatePatch
    {
        public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-VehicleDeactivate");

        public static bool Prefix(DeliveryVehicle __instance)
        {
            if (__instance == null || __instance.Vehicle == null) return true;
            if (__instance.ActiveDelivery?.Status == EDeliveryStatus.Completed) return true;

            var vehicleName = __instance.Vehicle?.name;
            if (string.IsNullOrEmpty(vehicleName)) return true;

            var shopName = vehicleName.Contains("Dan") ? "Dan" :
                             vehicleName.Contains("Oscar") ? "Oscar" : null;

            if (shopName == null) return true;

            var shops = DeliveryApp.Instance?.deliveryShops;
            var manager = DeliveryManager.Instance;
            if (shops == null || manager == null) return true;

            foreach (var shop in shops)
            {
                if (shop != null && manager.GetActiveShopDelivery(shop) != null)
                {
                    Logger.Warning($"{shopName} has active delivery, not deactivating");
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(DeliveryVehicle), nameof(DeliveryVehicle.Awake))]
    public static class DeliveryVehicleAwakePatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(DeliveryVehicle __instance)
        {
            if (Guid.TryParse(__instance.GUID, out _)) return true;
            if (__instance.GetComponent<LandVehicle>() == null) return false;
            __instance.Vehicle = __instance.GetComponent<LandVehicle>();
            __instance.Deactivate();
            return false;
        }
    }
}
