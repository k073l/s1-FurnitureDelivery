using FurnitureDelivery.Helpers;
using MelonLoader;
using UnityEngine;

#if MONO
using ScheduleOne.Vehicles;
using ScheduleOne.UI.Phone.Delivery;
#else
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.UI.Phone.Delivery;
#endif

namespace FurnitureDelivery.Shops;

public static class OscarShop
{
    public static readonly List<string> ItemIDs = new List<string>
    {
        "moisturepreservingpot",
        "airpot",
        "fullspectrumgrowlight",
        "suspensionrack",
        "packagingstation",
        "packagingstationmk2",
        "mixingstation",
        "mixingstationmk2",
        "dryingrack",
        "chemistrystation",
        "laboven",
        "cauldron",
        "brickpress",
        "locker"
    };

    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-OscarShop");

    public static void CreateOscarShop(DeliveryApp app)
    {
        Logger.Debug("Creating Oscar's Equipment shop");
        var deliveryVehicle = VehicleManager.Instance.AllVehicles.AsEnumerable()
            .FirstOrDefault(item => item != null && item.name.Contains("Oscar"));

        if (deliveryVehicle == null)
        {
            Logger.Warning("Oscar delivery vehicle not found, using default vehicle");
            deliveryVehicle = VehicleManager.Instance.AllVehicles.AsEnumerable().FirstOrDefault();
        }

        var builder = new DeliveryShopBuilder(app)
            .WithShopName("Oscar's Equipment")
            .WithShopDescription("'Specialized' equipment")
            .WithDeliveryFee(350f)
            .WithShopColor(new Color(0.87f, 0.44f, 0.05f))
            .WithShopImage(Utils.FindSprite("Oscar_Mugshot"))
            .SetAvailableByDefault(true)
            .WithDeliveryVehicle(DeliveryShopBuilder.GetOrCreateDeliveryVehicle(deliveryVehicle))
            .SetPosition(7);

        var itemDefinitions = Utils.GetAllStorableItemDefinitions();

        var wantedItems = ItemIDs
            .Select(id => itemDefinitions.FirstOrDefault(item => item.ID == id))
            .Where(item => item != null)
            .ToList();

        foreach (var item in wantedItems)
        {
            Logger.Debug($"Adding {item.ID} to Oscar's shop");
            builder.AddListing(item);
        }

        var builtShop = builder.Build();
        DeliveryShopBuilder.Apply(app, builtShop);

        Logger.Msg("Oscar's Equipment created");

        // Crucial - set oscar's shop go as disabled
        // to prevent it from being shown in the UI

        builtShop.gameObject.active = false;
    }
}