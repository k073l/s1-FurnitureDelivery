using System.Collections;
using System.Text;
using FurnitureDelivery;
using FurnitureDelivery.Helpers;
using MelonLoader;
using UnityEngine;
#if MONO
using ScheduleOne.Networking;
using ScheduleOne.Vehicles;
using Steamworks;
#else
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.Vehicles;
using Il2CppSteamworks;
using Guid = Il2CppSystem.Guid;
#endif


public class VehicleSync
{
    public static Dictionary<int, (string, string)> AddedVehicles = new();
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-VehicleSync");
    
    private const string VehiclesKey = "FurnitureDelivery_Vehicles";
    private const string Version = "1.0.0";

    public static bool isHost = Lobby.Instance.IsHost;
    public static bool isClient = !Lobby.Instance.IsHost && Lobby.Instance.IsInLobby;
    public static bool isSingleplayer = !Lobby.Instance.IsInLobby;

    public static void SyncVehicles()
    {
        if (isSingleplayer)
        {
            // singleplayer or steamworks not initialized
            Logger.Msg("Not in a lobby, skipping vehicle sync");
            return;
        }
        
        Logger.Debug($"Syncing {AddedVehicles.Count} vehicles to clients");

        if (isHost)
        {
            var payload = SerializeVehicles(AddedVehicles);
            Logger.Debug($"Syncing vehicles to clients: {payload}");
            // Lobby.Instance.SetLobbyData(LobbyKey, payload);
            if (!SteamMatchmaking.SetLobbyData(Lobby.Instance.LobbySteamID, VehiclesKey, payload))
            {
                Logger.Error("Failed to set lobby data for vehicle sync");
            }
        }
        else if (isClient)
        {
            // try getting the payload immediately
            Logger.Debug("Client detected, waiting for vehicle sync payload");
            if (GetPayload())
            {
                Logger.Debug("Vehicle sync payload received successfully");
                return;
            }
            MelonCoroutines.Start(WaitForPayload());
        }
    }

    public static IEnumerator WaitForPayload()
    {
        const int maxAttempts = 10;
        const float waitTime = 1f;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (GetPayload())
            {
                Logger.Debug("Vehicle sync payload received successfully");
                yield break;
            }
            Logger.Debug($"Attempt {attempt + 1}/{maxAttempts}: No payload received, waiting {waitTime} seconds...");
            yield return new WaitForSeconds(waitTime);
        }
        Logger.Error("Failed to receive vehicle sync payload after maximum attempts");
    }

    private static bool GetPayload()
    {
        var payload = SteamMatchmaking.GetLobbyData(Lobby.Instance.LobbySteamID, VehiclesKey);
        if (!string.IsNullOrEmpty(payload))
        {
            Logger.Debug($"Received vehicle sync payload: {payload}");
            AddedVehicles = DeserializeVehicles(payload);
            Logger.Msg($"Synced {AddedVehicles.Count} vehicles from payload");
            return true;
        }
        Logger.Debug("No vehicle sync payload found");
        return false;
    }

    private static string SerializeVehicles(Dictionary<int, (string, string)> vehicles)
    {
        var sb = new StringBuilder();
        sb.Append($"{VehiclesKey}@{Version};");
        foreach (var kvp in vehicles)
        {
            sb.Append($"{kvp.Key}:{kvp.Value.Item1},{kvp.Value.Item2};");
        }
        return sb.ToString();
    }

    private static Dictionary<int, (string, string)> DeserializeVehicles(string data)
    {
        var vehicles = new Dictionary<int, (string, string)>();
        if (string.IsNullOrEmpty(data))
            return vehicles;

        var entries = data.Split(';');
        // verify the first entry is the vehicles key and version
        if (entries.Length == 0 || !entries[0].StartsWith($"{VehiclesKey}@{Version}"))
        {
            Logger.Error("Invalid vehicle sync data format");
            return vehicles;
        }
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry))
                continue;

            var parts = entry.Split(':');
            if (parts.Length != 2)
                continue;

            var id = int.Parse(parts[0]);
            var vehicleParts = parts[1].Split(',');
            if (vehicleParts.Length != 2)
                continue;

            vehicles[id] = (vehicleParts[0], vehicleParts[1]);
        }
        return vehicles;
    }
    
    public static LandVehicle GetVehicleById(int id)
    {
        if (AddedVehicles.TryGetValue(id, out var vehicleData))
        {
            var vehicleName = vehicleData.Item1;
            var vehicleGuid = vehicleData.Item2;
            Logger.Debug($"Searching for vehicle with ID {id}, Name: {vehicleName}, GUID: {vehicleGuid}");
        
            var mainScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main");
            if (!mainScene.isLoaded)
            {
                Logger.Error("Main scene is not loaded.");
                return null;
            }

            var rootObjects = mainScene.GetRootGameObjects();
            LandVehicle vehicle = null;
            foreach (var root in rootObjects)
            {
                vehicle = root.GetComponentsInChildren<LandVehicle>(true)
                    .FirstOrDefault(v => v.ObjectId == id);
                if (vehicle != null)
                    break;
            }

            if (vehicle == null)
            {
                Logger.Error($"Vehicle with ObjectId {id} not found in Main scene");
                return null;
            }
        
            var guid = Guid.TryParse(vehicleGuid, out var parsedGuid) ? parsedGuid : Guid.Empty;
            Logger.Debug($"Setting the vehicle GUID to {guid}");
            vehicle.SetGUID(guid);
            Logger.Debug($"Setting the vehicle go name to {vehicleName}");
            vehicle.name = vehicleName;
            Logger.Debug($"Setting the vehicle vehicleName to {vehicleName}");
            vehicle.vehicleName = vehicleName;
            Logger.Debug($"Found vehicle: {vehicle.name} with GUID {vehicle.GUID} and ObjectId {vehicle.ObjectId}");
            return vehicle;
        }
        Logger.Error($"Vehicle with ID {id} not found in AddedVehicles dictionary");
        return null;
    }


    public static LandVehicle GetVehicleByName(string name)
    {
        Logger.Debug($"Searching for vehicle by name: {name}");
        var id = AddedVehicles.FirstOrDefault(kvp => kvp.Value.Item1.Contains(name)).Key;
        return id != 0 ? GetVehicleById(id) : null;
    }
}