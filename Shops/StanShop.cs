using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using MelonLoader;
using UnityEngine;

#if MONO
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.Vehicles.Modification;

#else
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.Vehicles.Modification;
#endif

namespace FurnitureDelivery.Shops;

public class StanShop
{
    public static readonly Dictionary<string, float> ItemPrices = new Dictionary<string, float>
    {
        { "baseballbat", 50f },
        { "fryingpan", 100f },
        { "machete", 250f },
        { "revolver", 1000f },
        { "revolvercylinder", 10f },
        { "m1911", 2500f },
        { "m1911mag", 20f },
        { "ak47", 15000f}, // moreguns
        { "ak47mag", 1000f}, // moreguns
        { "minigun", 75000f}, // moreguns
        { "minigunmag", 10000f}, // moreguns
    };

    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-StanShop");

    public static void CreateStanShop(DeliveryApp app)
    {
        Logger.Debug("Creating Stan's shop");

        var landVehicle = new LandVehicleBuilder()
            .WithVehicleName("LandVehicle_Stan")
            .WithVehicleCode("veeper")
            .WithPlayerOwned(false)
            .WithColor(EVehicleColor.Black)
            .Build();

        var shop = new DeliveryShopBuilder(app)
            .WithShopName("Armory")
            .WithShopDescription("Weapons and ammo")
            .WithShopColor(new Color(0.8f, 0f, 0f))
            .WithShopImage(Utils.FindSprite("Fixer_Mugshot"))
            .WithDeliveryFee(800f)
            .SetAvailableByDefault(true)
            .WithDeliveryVehicle(DeliveryShopBuilder.GetOrCreateDeliveryVehicle(landVehicle))
            .SetPosition(8);

        var itemDefinitions = Utils.GetAllStorableItemDefinitions();
        var wantedItems = ItemPrices
            .Select(kvp =>
            {
                var item = itemDefinitions.FirstOrDefault(i => i.ID == kvp.Key);
                return item != null ? (item, kvp.Value) : (null, 0f);
            })
            .Where(pair => pair.item != null)
            .ToList();

        foreach (var item in wantedItems)
        {
            Logger.Debug($"Adding item {item.item.ID} to Stan's shop");

            var v = item.Value;

            var weaponFamily = item.item.ID switch
            {
                "ak47" or "ak47mag" => "ak47",
                "minigun" or "minigunmag" => "minigun",
                _ => null
            };

            if (weaponFamily != null)
            {
                if (MoreGunsInterop.TryGetPrices(weaponFamily, out var gunPrice, out var magPrice))
                {
                    if (item.item.ID == weaponFamily)
                        v = gunPrice;
                    else
                        v = magPrice;

                    Logger.Msg($"Using MoreGuns price for '{item.item.ID}': {v}");
                }
            }

            shop.AddListing(item.item, overridePrice: v);
        }

        var builtShop = shop.Build();

        DeliveryShopBuilder.Apply(app, builtShop);
        Logger.Msg("Stan's shop created");

        // same thing as with oscar's shop

        builtShop.gameObject.active = false;
    }
}