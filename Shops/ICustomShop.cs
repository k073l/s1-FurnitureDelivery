#if MONO
using ScheduleOne.UI.Phone.Delivery;
#else
using Il2CppScheduleOne.UI.Phone.Delivery;
#endif

namespace FurnitureDelivery.Shops;

public interface ICustomShop
{
    public List<string> ItemIDs { get; }
    public DeliveryShop CreateShop(DeliveryApp app);
}