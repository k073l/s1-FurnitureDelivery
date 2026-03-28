using HarmonyLib;

#if MONO
using ScheduleOne.Vehicles;
using ScheduleOne.Weather;
#else
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Weather;
#endif

namespace FurnitureDelivery.Patches;

[HarmonyPatch]
public static class WheelPatches
{
    [HarmonyPatch(typeof(Wheel))]
    public static class WheelPatch
    {
        [HarmonyPatch("OnWeatherChange")]
        public static bool Prefix(Wheel __instance, WeatherConditions newConditions)
        {
            if (__instance?.vehicle == null) return false;
            if (newConditions?.Rainy == null) return false;
            return true;
        }
    }
}
