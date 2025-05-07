using FurnitureDelivery.Helpers;

#if MONO
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.Vehicles.Modification;
#else
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.Vehicles.Modification;
#endif
using MelonLoader;

using UnityEngine;

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

        var landVehicle = new LandVehicleBuilder()
            .WithVehicleName("LandVehicle_Herbert")
            .WithVehicleCode("veeper")
            .WithPlayerOwned(false)
            .WithColor(EVehicleColor.DarkBlue)
            .Build();
        
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
        
        var builtShop = shop.Build();
        
        DeliveryShopBuilder.Apply(app, builtShop);
        Logger.Msg("Herbert's Curiosities created");
    }
}