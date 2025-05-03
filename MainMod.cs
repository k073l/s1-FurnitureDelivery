using System.Reflection;
using MelonLoader;
using HarmonyLib;
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
        public const string Version = "1.0";
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
    }


    public class FurnitureDelivery : MelonMod
    {
        private static MelonLogger.Instance MelonLogger { get; set; }

        public override void OnInitializeMelon()
        {
            MelonLogger = LoggerInstance;
            MelonLogger.Msg("FurnitureDelivery initialized");
        }
    }

    [HarmonyPatch(typeof(DeliveryApp), "Awake")]
    public class DeliveryAppAwakePatch
    {
        public static List<string> wantedItemIDs = new List<string>
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
            "suspensionrack",
            "halogengrowlight",
            "largestoragerack",
            "mediumstoragerack",
            "smallstoragerack",
        };

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

            var wantedItems = wantedItemIDs
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
        }
    }
}