using System.Collections;
using System.Reflection;
using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.UI.Shop;
using UnityEngine;

#if MONO
using Object = System.Object;
using ScheduleOne.Vehicles;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne;
using ScheduleOne.ItemFramework;
#else
using Object = Il2CppSystem.Object;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppInterop.Runtime;
#endif


[assembly:
    MelonInfo(typeof(FurnitureDelivery.FurnitureDelivery), FurnitureDelivery.BuildInfo.Name,
        FurnitureDelivery.BuildInfo.Version,
        FurnitureDelivery.BuildInfo.Author)]
[assembly: MelonColor(1, 255, 215, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace FurnitureDelivery
{
    public static class BuildInfo
    {
        public const string Name = "FurnitureDelivery";
        public const string Description = "Adds a custom delivery shop for furniture items";
        public const string Author = "k073l";
        public const string Version = "1.1";
    }

#if !MONO
    public static class Il2CppListExtensions
    {
        public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this IEnumerable<T> source)
        {
            var il2CppList = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var item in source)
                il2CppList.Add(item);
            return il2CppList;
        }

        public static List<T> ConvertToList<T>(Il2CppSystem.Collections.Generic.List<T> il2CppList)
        {
            List<T> csharpList = new List<T>();
            T[] array = il2CppList.ToArray();
            csharpList.AddRange(array);
            return csharpList;
        }
    }
#endif

    public static class Utils
    {
        public static Sprite FindSprite(string spriteName)
        {
            try
            {
                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (sprite.name == spriteName)
                    {
                        MelonLogger.Msg($"Found sprite '{spriteName}' directly in loaded objects");
                        return sprite;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding sprite '{spriteName}': {ex.Message}");
                return null;
            }
        }

        public static List<T> GetAllComponentsInChildrenRecursive<T>(GameObject obj) where T : Component
        {
            List<T> results = new List<T>();
            if (obj == null) return results;

            T[] components = obj.GetComponents<T>();
            if (components.Length > 0)
            {
                results.AddRange(components);
            }

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                results.AddRange(GetAllComponentsInChildrenRecursive<T>(child.gameObject));
            }

            return results;
        }

        // original: https://github.com/KaBooMa/S1API/blob/stable/S1API/Internal/Utils/CrossType.cs
        static bool Is<T>(object obj, out T result)
#if !MONO
            where T : Il2CppSystem.Object
#else
            where T : class
#endif
        {
#if !MONO
            if (obj is Object il2CppObj)
            {
                var targetType = Il2CppType.Of<T>();
                var objType = il2CppObj.GetIl2CppType();

                if (targetType.IsAssignableFrom(objType))
                {
                    result = il2CppObj.TryCast<T>()!;
                    return result != null;
                }
            }
#else
                if (obj is T t)
                {
                    result = t;
                    return true;
                }
#endif

            result = null!;
            return false;
        }

        public static List<StorableItemDefinition> GetAllStorableItemDefinitions()
        {
#if !MONO
            var itemRegistry = Il2CppListExtensions.ConvertToList(Registry.Instance.ItemRegistry);
#else
            var itemRegistry = Registry.Instance.ItemRegistry.ToList();
#endif
            var itemDefinitions = new List<StorableItemDefinition>();

            foreach (var item in itemRegistry)
            {
                if (Utils.Is<StorableItemDefinition>(item.Definition, out var definition))
                {
                    itemDefinitions.Add(definition);
                }
                else
                {
                    MelonLogger.Msg(
                        $"Definition {item.Definition?.GetType().FullName} is not a StorableItemDefinition");
                }
            }

            return itemDefinitions
                .ToList();
        }

        public static List<DeliveryShop> GetInitializedShops(DeliveryApp app, out Transform contentT)
        {
            var scrollViewGO = Utils.GetAllComponentsInChildrenRecursive<Transform>(app.gameObject)
                .Select(t => t.gameObject)
                .FirstOrDefault(go => go.name == "Scroll View");

            if (scrollViewGO == null)
            {
                MelonLogger.Error("Could not find Scroll View in DeliveryApp");
                contentT = null!;
                return null;
            }

            var contentGO = Utils.GetAllComponentsInChildrenRecursive<Transform>(scrollViewGO)
                .Select(t => t.gameObject)
                .FirstOrDefault(go => go.name == "Content");

            if (contentGO == null)
            {
                MelonLogger.Error("Could not find Content in Scroll View");
                contentT = null!;
                return null;
            }

            var content = contentGO.transform;

            var shopComponents = new List<DeliveryShop>();
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                var shop = child.GetComponent<DeliveryShop>();
                if (shop != null)
                    shopComponents.Add(shop);
            }

            contentT = content;
            return shopComponents;
        }
    }


    public class FurnitureDelivery : MelonMod
    {
        private static MelonLogger.Instance MelonLogger { get; set; }
        // Flag to track if Oscar's shop has been initialized
        public static bool IsOscarShopInitialized = false;
        
        // Flag to track if Oscar's shop initialization has started
        public static bool IsOscarShopInitializing = false;
        
        // Flag to indicate that SetIsAvailable has been called on Oscar's shop
        public static bool OscarSetIsAvailableCalled = false;
        
        // Coroutine reference for Oscar shop initialization
        public static object oscarShopCoroutine = null;

        public override void OnInitializeMelon()
        {
            MelonLogger = LoggerInstance;
            MelonLogger.Msg("FurnitureDelivery initialized");
        }
    }
    
    public static class OscarShopItems
    {
        public static readonly List<string> ItemIDs = new List<string>
        {
            "packagingstation",
            "packagingstationmk2",
            "mixingstation",
            "mixingstationmk2",
            "dryingrack",
            "chemistrystation",
            "laboven",
            "cauldron",
            "brickpress"
        };
    }

    public static class DanShopItems
    {
        public static readonly List<string> ItemIDs = new List<string>
        {
            "cofeetable",
            "metalsquaretable",
            "woodsquaretable",
            "toilet",
            "trashcan",
            "bed",
            "TV",
            "floorlamp",
            "growtent",
            "plasticpot",
            "moisturepreservingpot",
            "airpot",
            "halogengrowlight",
            "ledgrowlight",
            "fullspectrumgrowlight",
            "suspensionrack",
            "soilpourer",
            "potsprinkler",
            "largestoragerack",
            "mediumstoragerack",
            "smallstoragerack",
        };
    }

    [HarmonyPatch(typeof(DeliveryShop), "SetIsAvailable")]
    public class DeliveryShopSetIsAvailablePatch
    {
        public static void Postfix(DeliveryShop __instance)
        {
            // Skip if we've already initialized Oscar's shop
            if (FurnitureDelivery.IsOscarShopInitialized)
            {
                return;
            }
            
            var app = PlayerSingleton<DeliveryApp>.Instance;
            var shops = Utils.GetInitializedShops(app, out _);

            // Find Oscar's shop to use it as a reference point
            var oscarShop = shops?.FirstOrDefault(item => 
                item.gameObject.name.StartsWith("Oscar"));
                
            if (oscarShop == null)
            {
                return;
            }

            // Only proceed if we're patching Oscar's actual shop instance
            if (__instance.gameObject.name != oscarShop.gameObject.name)
                return;
            
            MelonLogger.Msg($"SetIsAvailable called on Oscar's shop: {__instance.gameObject.name}");
            
            // Set flag that Oscar's SetIsAvailable has been called
            FurnitureDelivery.OscarSetIsAvailableCalled = true;
            
            // If we're not already initializing Oscar's shop through the coroutine,
            // create it directly here
            if (!FurnitureDelivery.IsOscarShopInitializing)
            {
                DeliveryAppAwakePatch.CreateOscarsShop(app);
            }
        }
    }


    [HarmonyPatch(typeof(DeliveryApp), "Awake")]
    public class DeliveryAppAwakePatch
    {

        public static void Postfix(DeliveryApp __instance)
        {
            MelonLogger.Msg("DeliveryApp Awake called");

            var app = PlayerSingleton<DeliveryApp>.Instance;

#if !MONO
            var deliveryVehicle = VehicleManager.Instance.AllVehicles._items[0];
#else
            var deliveryVehicle = VehicleManager.Instance.AllVehicles.FirstOrDefault();
#endif
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

            var wantedItems = DanShopItems.ItemIDs
                .Select(id => itemDefinitions.FirstOrDefault(item => item.ID == id))
                .Where(item => item != null)
                .ToList();

            foreach (var item in wantedItems)
            {
                MelonLogger.Msg($"Adding item {item.name} to shop");
                shop.AddListing(item);
            }

            var builtShop = shop.Build();

            DeliveryAppWithPosition.Finalize(app, builtShop);
            
            // Start coroutine to wait for Oscar's shop initialization
            if (FurnitureDelivery.oscarShopCoroutine == null && !FurnitureDelivery.IsOscarShopInitialized && !FurnitureDelivery.IsOscarShopInitializing)
            {
                FurnitureDelivery.IsOscarShopInitializing = true;
                FurnitureDelivery.oscarShopCoroutine = MelonCoroutines.Start(WaitForOscarShopInitialization(__instance));
            }
            
            // When all shops are initialized, refresh the app content
            app.RefreshContent();
        }
        
        private static IEnumerator WaitForOscarShopInitialization(DeliveryApp app)
        {
            MelonLogger.Msg("Starting coroutine to wait for Oscar's shop initialization");
            
            // Wait up to 30 seconds for Oscar's SetIsAvailable to be called
            float timeout = 30f;
            float elapsed = 0f;
            
            while (!FurnitureDelivery.OscarSetIsAvailableCalled && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                
                if (elapsed % 5 == 0)
                {
                    MelonLogger.Msg($"Waiting for Oscar's shop SetIsAvailable... ({elapsed}s elapsed)");
                }
            }
            
            // Check if we timed out
            if (elapsed >= timeout)
            {
                MelonLogger.Warning($"Timed out waiting for Oscar's SetIsAvailable after {timeout} seconds. Oscar's shop will not be created.");
                FurnitureDelivery.IsOscarShopInitializing = false;
                FurnitureDelivery.oscarShopCoroutine = null;
                yield break;
            }
            
            // If we didn't time out, Oscar's SetIsAvailable was called
            MelonLogger.Msg("Oscar's SetIsAvailable was called, proceeding with shop creation");
            
            // Create Oscar's shop only if it hasn't been created yet
            if (!FurnitureDelivery.IsOscarShopInitialized)
            {
                CreateOscarsShop(app);
            }
            
            FurnitureDelivery.IsOscarShopInitializing = false;
            FurnitureDelivery.oscarShopCoroutine = null;
        }
        
        public static void CreateOscarsShop(DeliveryApp app)
        {
            MelonLogger.Msg("Creating Oscar's Equipment shop");
            
#if !MONO
            var deliveryVehicle = VehicleManager.Instance.AllVehicles._items
                .FirstOrDefault(item => item != null && item.name != null && item.name.Contains("Oscar"));
#else
            var deliveryVehicle = VehicleManager.Instance.AllVehicles
                .FirstOrDefault(item => item != null && item.name != null && item.name.Contains("Oscar"));
#endif

            if (deliveryVehicle == null)
            {
                MelonLogger.Warning("Oscar delivery vehicle not found, using default vehicle");
#if !MONO
                deliveryVehicle = VehicleManager.Instance.AllVehicles._items[0];
#else
                deliveryVehicle = VehicleManager.Instance.AllVehicles.FirstOrDefault();
#endif
            }
            
            var builder = new DeliveryShopBuilder(app)
                .WithShopName("Oscar's Equipment")
                .WithShopDescription("'Specialized' equipment")
                .WithDeliveryFee(350f)
                .WithShopColor(new Color(0.87f, 0.44f, 0.05f))
                .WithShopImage(Utils.FindSprite("Oscar_Mugshot"))
                .SetAvailableByDefault(true)
                .WithDeliveryVehicle(DeliveryShopBuilder.GetOrCreateDeliveryVehicle(deliveryVehicle))
                .SetPosition(6);

            var itemDefinitions = Utils.GetAllStorableItemDefinitions();
            
            var wantedItems = OscarShopItems.ItemIDs
                .Select(id => itemDefinitions.FirstOrDefault(item => item.ID == id))
                .Where(item => item != null)
                .ToList();

            // Log how many items were found
            MelonLogger.Msg($"Found {wantedItems.Count} items out of {OscarShopItems.ItemIDs.Count} for Oscar's shop");
            
            foreach (var item in wantedItems)
            {
                MelonLogger.Msg($"Adding {item.ID} to Oscar's shop");
                builder.AddListing(item);
            }

            var builtShop = builder.Build();
            DeliveryAppWithPosition.Finalize(app, builtShop);
            
            // Set flag that Oscar's shop has been initialized
            FurnitureDelivery.IsOscarShopInitialized = true;
            
            // Refresh the app content to ensure everything is displayed
            app.RefreshContent();
            
            MelonLogger.Msg("Oscar's Equipment shop created successfully");
            
            // Log all shops in the app
            var shops = Utils.GetInitializedShops(app, out var content);
            foreach (var s in shops)
            {
                MelonLogger.Msg($"Found shop: {s.gameObject.name}");
                foreach (var i in s.listingEntries)
                {
                    MelonLogger.Msg($"Item: {i.name}");
                }
            }

            var sI = ShopInterface.AllShops;
            foreach (var s in sI)
            {
                MelonLogger.Msg($"Found shop in ShopInterface: {s.gameObject.name}");
                foreach (var i in s.Listings)
                {
                    MelonLogger.Msg($"Item: {i.Item.ID}, {i.Item.Name}");
                }
            }
        }
        
    }
}