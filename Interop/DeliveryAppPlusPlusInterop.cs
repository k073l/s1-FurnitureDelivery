using System.Reflection;
using HarmonyLib;
using MelonLoader;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
#else
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
#endif

namespace FurnitureDelivery.Interop;

[HarmonyPatch(typeof(Cart))]
static class CartPatches
{
	[HarmonyPatch("UpdateTotal")]
	[HarmonyPrefix]
	private static bool UpdateTotal(Cart __instance)
	{
		return __instance.TotalText != null;
	}
	
	[HarmonyPatch("UpdateProblem")]
	[HarmonyPrefix]
	private static bool UpdateProblem(Cart __instance)
	{
		return __instance.ProblemText != null;
	}
	
	[HarmonyPatch("UpdateViewCartText")]
	[HarmonyPrefix]
	private static bool UpdateViewCartText(Cart __instance)
	{
		return __instance.ViewCartText != null;
	}
}

public class DeliveryAppPlusPlusInterop
{
	private static MelonLogger.Instance _logger = new MelonLogger.Instance($"{BuildInfo.Name}-DeliveryAppPlusPlusInterop");
    public static void ApplyPatches()
    {
        var harmony = Melon<FurnitureDelivery>.Instance.HarmonyInstance;
        
        var targetAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "DeliveryAppPlusPlus");

        if (targetAssembly == null)
        {
            _logger.Error("Target assembly not found!");
            return;
        }
        
        var deliveryInfoType = targetAssembly.GetType("FavouriteDeliveriesPatch.DeliveryInfo");
        if (deliveryInfoType == null)
        {
            _logger.Error("DeliveryInfo type not found!");
            return;
        }
        
        var targetType = targetAssembly.GetType("FavouriteDeliveriesPatch.DeliveryUtils");
        if (targetType == null)
        {
            _logger.Error("Target class not found!");
            return;
        }
        
        var method = AccessTools.Method(targetType, "OnReorder", [deliveryInfoType]);
        if (method == null)
        {
            _logger.Error("OnReorder method not found!");
            return;
        }

        harmony.Patch(method, prefix: new HarmonyMethod(typeof(DeliveryAppPlusPlusInterop).GetMethod(nameof(OnReorder_Prefix), BindingFlags.Static | BindingFlags.NonPublic)));
    }

#if MONO
    private static bool OnReorder_Prefix(object __0)
	{
		try
		{
			var deliveryInfoType = __0.GetType();
#else
    private static bool OnReorder_Prefix(Il2CppObjectBase __0) // __0 is DeliveryInfo
    {
	    try
	    {
		    var deliveryInfoType = __0.GetType();
#endif
		    // try to get 'instance' field
		    var instanceField = deliveryInfoType.GetField("instance", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		    if (instanceField == null)
		    {
			    MelonLogger.Warning("[Added by FurnitureDelivery] Could not find 'instance' field on DeliveryInfo.");
			    return false;
		    }

		    var instanceObj = instanceField.GetValue(__0);
		    if (instanceObj == null)
		    {
			    MelonLogger.Warning("[Added by FurnitureDelivery] 'instance' field was null.");
			    return false;
		    }

		    // get StoreName property on instance
		    var storeNameProp = instanceObj.GetType().GetProperty("StoreName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		    var storeName = storeNameProp?.GetValue(instanceObj) as string;
		    if (storeName == null)
		    {
			    MelonLogger.Warning("[Added by FurnitureDelivery] 'StoreName' property was null.");
			    return false;
		    }

		    var shop = PlayerSingleton<DeliveryApp>.Instance.GetShop(storeName);
		    _ = shop.CanOrder(out var reason);
		    // check if we can order from this shop
		    if (!string.IsNullOrEmpty(reason))
		    {
			    MelonLogger.Msg($"[Added by FurnitureDelivery] Tried to order, but got {reason}");
			    return false; // stop original method
		    }
			    
		    return true; // all good, continue with original method
	    }
	    catch (Exception ex)
	    {
		    MelonLogger.Error($"[Added by FurnitureDelivery] Failed to reflect DeliveryInfo: {ex}");
	    }

	    return false; // weird error, stop original method
    }

}