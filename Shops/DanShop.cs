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

public static class DanShop
{
    public static readonly List<string> ItemIDs = new List<string>
    {
        "coffeetable",
        "metalsquaretable",
        "woodsquaretable",
        "plastictable",
        "toilet",
        "trashcan",
        "trash_bin", // UpgradedTrashCans
        "trash_compactor", // UpgradedTrashCans
        "bed",
        "locker",
        "TV",
        "floorlamp",
        "growtent",
        "plasticpot",
        "halogengrowlight",
        "ledgrowlight",
        "suspensionrack",
        "soilpourer",
        "potsprinkler",
        "largestoragerack",
        "mediumstoragerack",
        "smallstoragerack",
    };

    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-DanShop");

    public static void CreateDanShop(DeliveryApp app)
    {
        Logger.Debug("Creating Dan's Furniture shop");
        var deliveryVehicle = VehicleManager.Instance.AllVehicles.AsEnumerable()
            .FirstOrDefault(item => item != null && item.name.Contains("Dan"));
        Logger.Debug($"Found delivery vehicle: {deliveryVehicle?.name} with guid {deliveryVehicle?.GUID}");

        var shop = new DeliveryShopBuilder(app)
            .WithShopName("Dan's Furniture")
            .WithShopDescription("General furniture")
            .WithShopColor(new Color(0.06f, 0.56f, 0.87f))
            .WithShopImage(Utils.FindSprite("Dan_Mugshot"))
            .WithDeliveryFee(300f)
            .SetAvailableByDefault(true)
            .WithDeliveryVehicle(DeliveryShopBuilder.GetOrCreateDeliveryVehicle(deliveryVehicle))
            .SetPosition(2);

        var itemDefinitions = Utils.GetAllStorableItemDefinitions();

        var wantedItems = ItemIDs
            .Select(id => itemDefinitions.FirstOrDefault(item => item.ID == id))
            .Where(item => item != null)
            .ToList();

        foreach (var item in wantedItems)
        {
            Logger.Debug($"Adding item {item.name} to Dan's shop");
            shop.AddListing(item);
        }

        var builtShop = shop.Build();

        DeliveryShopBuilder.Apply(app, builtShop);
        Logger.Msg("Dan's Furniture created");
    }
}