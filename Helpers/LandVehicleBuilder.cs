#if MONO
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.Vehicles.Modification;
using ScheduleOne.Vehicles;
#else
using Il2Cpp;
using Il2CppFishNet;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Vehicles.Modification;
using Il2CppScheduleOne.Vehicles;
using Guid = Il2CppSystem.Guid;
#endif
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
        var position = new Vector3(130f, 50f, -250f);
        // var position = new Vector3(0, 10, 0);
        var rotation = Quaternion.identity;

        if (!InstanceFinder.IsServer)
        {
            MelonLogger.Error("LandVehicleBuilder can only be used on the server.");
            return null;
        }

        var prefab = VehicleManager.Instance.GetVehiclePrefab(_vehicleCode);
        if (prefab == null)
        {
            MelonLogger.Error($"Vehicle prefab with code '{_vehicleCode}' not found.");
            return null;
        }

        var go = Object.Instantiate<GameObject>(prefab.gameObject);
        var component = go.GetComponent<LandVehicle>();
        component.transform.position = position;
        component.transform.rotation = rotation;
        var guid = GUIDManager.GenerateUniqueGUID();
        component.SetGUID(guid);
        component.name = _vehicleName;
        component.gameObject.name = _vehicleName;
        component.vehicleName = _vehicleName;
        component.vehiclePrice = _vehiclePrice;
        component.TopSpeed = _topSpeed;
        component.SetIsPlayerOwned(null, _isPlayerOwned);
        if (_isPlayerOwned)
            VehicleManager.Instance.PlayerOwnedVehicles.Add(component);
        component.ApplyColor(_color);
        component.SetOwnedColor(null, _color);
        
        VehicleManager.Instance.AllVehicles.Add(component);
        VehicleManager.Instance.NetworkObject.Spawn(component.gameObject, null, default(Scene));

        VehicleSync.AddedVehicles.Add(component.ObjectId, (_vehicleName, guid.ToString()));
        VehicleSync.SyncVehicles();
        
        return component;
    }
}