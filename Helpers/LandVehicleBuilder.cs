#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Vehicles.Modification;
using ScheduleOne.Vehicles;
#else
using Il2Cpp;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Vehicles.Modification;
using Il2CppScheduleOne.Vehicles;
#endif
using UnityEngine;

namespace FurnitureDelivery.Helpers;

public class LandVehicleBuilder
{
    private string _vehicleName = "CustomVehicle";
    private string _vehicleCode = "shitbox";
    private float _vehiclePrice = 1000f;
    private float _topSpeed = 60;
    private bool _isPlayerOwned = true;
    private EVehicleColor _color = EVehicleColor.Custom;
#if MONO
    public static List<LandVehicle> VehiclePrefabs = NetworkSingleton<VehicleManager>.Instance.VehiclePrefabs;
#else
    public static List<LandVehicle> VehiclePrefabs =
        NetworkSingleton<VehicleManager>.Instance.VehiclePrefabs.ConvertToList();
#endif

    public LandVehicleBuilder()
    {
    }

    public LandVehicleBuilder WithVehicleName(string vehicleName)
    {
        _vehicleName = vehicleName;
        return this;
    }

    public LandVehicleBuilder WithVehicleCode(string vehicleCode)
    {
        _vehicleCode = vehicleCode;
        return this;
    }

    public LandVehicleBuilder WithVehiclePrice(float vehiclePrice)
    {
        _vehiclePrice = vehiclePrice;
        return this;
    }

    public LandVehicleBuilder WithTopSpeed(float topSpeed)
    {
        _topSpeed = topSpeed;
        return this;
    }

    public LandVehicleBuilder WithPlayerOwned(bool isPlayerOwned)
    {
        _isPlayerOwned = isPlayerOwned;
        return this;
    }

    public LandVehicleBuilder WithColor(EVehicleColor color)
    {
        _color = color;
        return this;
    }

    public LandVehicle Build()
    {
        Vector3 position = new Vector3();
        Quaternion rotation = Quaternion.identity;

        var component =
            NetworkSingleton<VehicleManager>.Instance.SpawnAndReturnVehicle(_vehicleCode, position, rotation, false);
        component.SetGUID(GUIDManager.GenerateUniqueGUID());
        component.name = _vehicleName;
        component.gameObject.name = _vehicleName;
        component.vehicleName = _vehicleName;
        component.vehiclePrice = _vehiclePrice;
        component.TopSpeed = _topSpeed;
        component.ApplyColor(_color);
        component.SetOwnedColor(null, _color);
        component.SetIsPlayerOwned(null, _isPlayerOwned);
        if (_isPlayerOwned)
            NetworkSingleton<VehicleManager>.Instance.PlayerOwnedVehicles.Add(component);

        NetworkSingleton<VehicleManager>.Instance.AllVehicles.Add(component);

        return component;
    }
}