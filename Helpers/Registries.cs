using System.Linq;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;
using ScheduleOne.UI.Phone.Delivery;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.UI.Phone.Delivery;
#endif

namespace FurnitureDelivery.Helpers;

public static class Registries
{
    private static GameObject _fdRoot;
    public static GameObject FDRoot => _fdRoot ??= new GameObject("FurnitureDeliveryRoot");

    public static Dictionary<LandVehicle, DeliveryVehicle> DeliveryVehicleRegistry { get; } = new();

    public static Dictionary<DeliveryShop, float> DeliveryFeeRegistry { get; } = new();

    public static Dictionary<DeliveryShop, Sprite> ShopImageRegistry { get; } = new();

    public static Dictionary<string, int> ShopPositionRegistry { get; } = new();

    public static int OriginalDeliveryShopsCount { get; set; } = -1;

    public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-Registries");

    public static void RegisterAvailableVehicles()
    {
        var vehicleManager = VehicleManager.Instance;
        if (vehicleManager == null || vehicleManager.AllVehicles == null)
        {
            Logger.Error("VehicleManager or AllVehicles is null.");
            return;
        }

        foreach (var vehicle in vehicleManager.AllVehicles.AsEnumerable())
        {
            if (vehicle == null) continue;
            if (!DeliveryVehicleRegistry.ContainsKey(vehicle))
            {
                DeliveryVehicleRegistry[vehicle] = null;
            }
        }

        Logger.Debug($"Registered {DeliveryVehicleRegistry.Count} vehicles for lazy wrapping.");
    }

    public static DeliveryVehicle GetOrCreateDeliveryVehicle(LandVehicle vehicle)
    {
        if (vehicle == null) return null;

        if (vehicle?.gameObject?.GetComponent<DeliveryVehicle>() != null)
            return vehicle.gameObject.GetComponent<DeliveryVehicle>();

        if (DeliveryVehicleRegistry.TryGetValue(vehicle, out var cached) && cached != null)
        {
            Logger.Debug("Using cached DeliveryVehicle for " + vehicle.name);
            if (cached.Vehicle != vehicle)
            {
                Logger.Warning($"Cached DeliveryVehicle's Vehicle does not match for {vehicle.name}. Updating reference.");
                cached.Vehicle = vehicle;
            }
            return cached;
        }

        var vehicleObject = vehicle.gameObject;
        vehicleObject.transform.SetParent(FDRoot.transform);
        var deliveryVehicle = vehicleObject.AddComponent<DeliveryVehicle>();
        deliveryVehicle.Vehicle = vehicle;
        deliveryVehicle.GUID = vehicle.GUID.ToString();
        Logger.Debug($"Created new DeliveryVehicle for {vehicle.name} with GUID {deliveryVehicle.GUID}");

        DeliveryVehicleRegistry[vehicle] = deliveryVehicle;
        return deliveryVehicle;
    }

    public static void RegisterShopPosition(string shopName, int position)
    {
        ShopPositionRegistry[shopName] = position;
    }

    public static int GetShopPosition(string shopName)
    {
        return ShopPositionRegistry.TryGetValue(shopName, out var pos) ? pos : -1;
    }

    public static void RegisterShopImage(DeliveryShop shop, Sprite image)
    {
        ShopImageRegistry.TryAdd(shop, image);
    }

    public static Sprite GetShopImage(DeliveryShop shop)
    {
        return ShopImageRegistry.TryGetValue(shop, out var sprite) ? sprite : null;
    }

    public static void RegisterDeliveryFee(DeliveryShop shop, float fee)
    {
        DeliveryFeeRegistry.TryAdd(shop, fee);
    }

    public static void Clear()
    {
        DeliveryVehicleRegistry.Clear();
        DeliveryFeeRegistry.Clear();
        ShopImageRegistry.Clear();
        ShopPositionRegistry.Clear();
        OriginalDeliveryShopsCount = -1;
        if (_fdRoot != null)
        {
            Object.Destroy(_fdRoot);
            _fdRoot = null;
        }
        Logger.Debug("Registries cleared");
    }
}
