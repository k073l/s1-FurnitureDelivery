using MelonLoader;
using UnityEngine;

#if MONO
using ScheduleOne.UI.Phone.Delivery;
#else
using Il2CppScheduleOne.UI.Phone.Delivery;
#endif

namespace FurnitureDelivery;

public static class DeliveryAppWithPosition
{
    public static void Finalize(DeliveryApp app, DeliveryShop shop)
    {
        int insertPosition = -1;
        if (ShopPositionRegistry.ShopPositions.TryGetValue(shop.gameObject.name, out int position))
        {
            insertPosition = position;
            MelonLogger.Msg($"Found position {insertPosition} for shop {shop.gameObject.name}");
        }
            
        if (insertPosition < 0)
        {
            insertPosition = app.deliveryShops.Count + insertPosition + 1;
            if (insertPosition < 0) insertPosition = 0;
        }
            
        if (insertPosition > app.deliveryShops.Count)
            insertPosition = app.deliveryShops.Count;
            
        // Insert the shop at the specified position
        if (insertPosition < app.deliveryShops.Count)
        {
            MelonLogger.Msg($"Inserting shop at position {insertPosition}");
            app.deliveryShops.Insert(insertPosition, shop);
        }
        else
        {
            MelonLogger.Msg($"Adding shop to end at position {insertPosition}");
            app.deliveryShops.Add(shop);
        }
            
        MelonLogger.Msg($"Added new delivery shop: {shop.name}, {shop.gameObject.name}");
            
        // fix hierarchy in UI
        FixShopHierarchy(app);
    }

    private static void FixShopHierarchy(DeliveryApp app)
    {
        var shopComponents = Utils.GetInitializedShops(app, out var content);

        MelonLogger.Msg($"Found {shopComponents.Count} shop components in UI hierarchy");

        for (int i = 0; i < app.deliveryShops.Count; i++)
        {
            #if !MONO
            var shop = app.deliveryShops._items[i];
            #else
            var shop = app.deliveryShops[i];
            #endif
            if (shop.gameObject.name == "Space" || shop.gameObject.name.Contains("Spacer"))
                continue;

            shop.transform.SetParent(content, false);
            shop.transform.SetSiblingIndex(i);
            MelonLogger.Msg($"Set {shop.gameObject.name} to sibling index {i}");
        }

        MelonLogger.Msg("Final UI hierarchy order:");
        for (int i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            MelonLogger.Msg($"[{i}] {child.gameObject.name}");
        }
    }
}