using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using MelonLoader;
using UnityEngine;

#if MONO
using FishNet;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.Vehicles.Modification;
using ScheduleOne.Vehicles;
#else
using Il2CppFishNet;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.Vehicles.Modification;
using Il2CppScheduleOne.Vehicles;
#endif

namespace FurnitureDelivery.Shops;

public class HerbertShop
{
    public static readonly List<string> ItemIDs = new List<string>
    {
        "woodensign",
        "metalsign",
        "wallmountedshelf",
        "antiquewalllamp",
        "modernwalllamp",
        "wallclock",
        "grandfatherclock",
        "safe",
        "jukebox",
        "goldenskateboard",
        "filingcabinet",
        "smalltrashcan",
        "dumpster",
    };

    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-HerbertShop");

    public static void CreateHerbertShop(DeliveryApp app)
    {
        Logger.Debug("Creating Herbert's shop");

        LandVehicle landVehicle = null;
        if (!InstanceFinder.IsServer)
        {
            Logger.Debug("Syncing vehicles");
            VehicleSync.SyncVehicles();
            Logger.Debug("Not on server, trying to find Herbert's land vehicle");
            landVehicle = Utils.GetNotNullWithTimeout<LandVehicle>(() => VehicleSync.GetVehicleByName("LandVehicle_Herbert"));
        }
        else
        {
            Logger.Debug("On server, creating Herbert's land vehicle");
            landVehicle = new LandVehicleBuilder()
                .WithVehicleName("LandVehicle_Herbert")
                .WithVehicleCode("veeper")
                .WithPlayerOwned(false)
                .WithColor(EVehicleColor.DarkBlue)
                .Build();
        }
        
        Logger.Debug($"Found land vehicle: {landVehicle?.name} with guid {landVehicle?.GUID}");
        
        var shop = new DeliveryShopBuilder(app)
            .WithShopName("Herbert's Curiosities")
            .WithShopDescription("Boutique's picks and exotic items")
            .WithShopColor(new Color(0.2f, 0f, 1f))
            .WithShopImage(Utils.FindSprite("Herbert_Mugshot"))
            .WithDeliveryFee(500f)
            .SetAvailableByDefault(true)
            .WithDeliveryVehicle(DeliveryShopBuilder.GetOrCreateDeliveryVehicle(landVehicle))
            .SetPosition(5);

        var itemDefinitions = Utils.GetAllStorableItemDefinitions();
        var wantedItems = ItemIDs
            .Select(id => itemDefinitions.FirstOrDefault(item => item.ID == id))
            .Where(item => item != null)
            .ToList();

        foreach (var item in wantedItems)
        {
            Logger.Debug($"Adding item {item.name} to Herbert's shop");
            shop.AddListing(item);
        }
        
        // Toileportation Interop
        if (FurnitureDelivery.RegisteredMelons.Any(m => m.Info.Name == "Toileportation"))
        {
            var toilet = ToileportationInterop.GoldenToiletListing;
            if (toilet == null)
            {
                // wait for the item to be created
                Logger.Warning("Golden toilet listing not found, waiting for it to be created");
                MelonCoroutines.Start(Utils.WaitForNotNull(toilet));
            }
            toilet.CanBeDelivered = true;
            Logger.Msg("Adding golden toilet to Herbert's shop");
            shop.AddListing(toilet);
            Logger.Debug($"{toilet.Shop}, {toilet.CurrentStock}, {toilet.Item.BasePurchasePrice}, {toilet.Item.ID}");
        }

        var builtShop = shop.Build();

        DeliveryShopBuilder.Apply(app, builtShop);
        Logger.Msg("Herbert's Curiosities created");
    }
}