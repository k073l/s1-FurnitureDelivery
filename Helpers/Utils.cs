using MelonLoader;
using UnityEngine;

#if MONO
using ScheduleOne;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI.Phone.Delivery;

#else
using Il2CppInterop.Runtime;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Object = Il2CppSystem.Object;
#endif


namespace FurnitureDelivery.Helpers;


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
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-Utils");

    public static Sprite FindSprite(string spriteName)
    {
        try
        {
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite.name == spriteName)
                {
                    Logger.Debug($"Found sprite '{spriteName}' directly in loaded objects");
                    return sprite;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error finding sprite '{spriteName}': {ex.Message}");
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
                Logger.Warning(
                    $"Definition {item.Definition?.GetType().FullName} is not a StorableItemDefinition");
            }
        }

        return itemDefinitions
            .ToList();
    }
}