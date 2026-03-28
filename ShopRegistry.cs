using FurnitureDelivery.Shops;

namespace FurnitureDelivery;

public static class ShopRegistry
{
    private static readonly List<ICustomShop> _shops =
    [
        new DanShop(),
        new HerbertShop(),
        new OscarShop(),
        new StanShop()
    ];

    public static IReadOnlyList<ICustomShop> Shops => _shops.AsReadOnly();

    public static string GetShopName(this ICustomShop shop) => shop.ShopName;
}
