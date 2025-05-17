using FurnitureDelivery.Helpers;
using HarmonyLib;
using MelonLoader;

namespace FurnitureDelivery.Interop;


#if MONO
using ScheduleOne.UI.Shop;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.Delivery;
#else
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.Delivery;
#endif

public static class ToileportationInterop
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-ToileportationInterop");
    public static ShopListing GoldenToiletListing;

    public static bool CanOrder(List<DeliveryShop> shops, out string reason)
    {
        reason = string.Empty;
        // skip if no mod present
        if (FurnitureDelivery.RegisteredMelons.All(m => m.Info.Name != "Toileportation")) return true;

        var inStock = GoldenToiletListing.CurrentStock;
        foreach (var shop in shops)
        {
            var entries = shop.listingEntries;
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (!entry.MatchingListing.Item.Name.Contains("Golden Toilet")) continue;
                if (entry.SelectedQuantity > inStock)
                {
                    reason = "Not enough Golden Toilets in stock";
                    return false;
                }
            }
        }

        return true;
    }

    public static void OnDeliveryCreated(DeliveryInstance delivery)
    {
        if (delivery == null) return;
        // look for the Golden Toilet
        var items = delivery.Items;
        Logger.Debug($"Delivery created with {items.Length} items");
        foreach (var item in items)
        {
            Logger.Debug($"Checking item {item.String} in quantity {item.Int}");
            if (item.String == "goldentoilet")
            {
                Logger.Debug("Found Golden Toilet in delivery");
                if (GoldenToiletListing.CurrentStock < item.Int)
                    Logger.Error("Delivering more Golden Toilets than in stock");
                GoldenToiletListing.CurrentStock -= item.Int;
                Logger.Debug($"New stock: {GoldenToiletListing.CurrentStock}");
            }
        }
    }

    [HarmonyPatch(typeof(ShopListing), "Initialize")]
    public static class ToileportationShopListingPatch
    {
        public static void Postfix(ShopListing __instance)
        {
            if (FurnitureDelivery.RegisteredMelons.All(m => m.Info.Name != "Toileportation"))
                return;
            if (__instance == null)
                return;
            if (string.IsNullOrEmpty(__instance.name))
                return;
            if (!__instance.name.Contains("Golden Toilet"))
                return;
            GoldenToiletListing = __instance;
            Logger.Msg("Captured Golden Toilet listing");
        }
    }
}