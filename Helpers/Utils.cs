using System.Collections;
using System.Diagnostics.CodeAnalysis;
using MelonLoader;
using UnityEngine;

#if MONO
using ScheduleOne;
using ScheduleOne.ItemFramework;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Delivery;

#else
using Il2CppInterop.Runtime;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Object = Il2CppSystem.Object;
#endif


namespace FurnitureDelivery.Helpers;

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

    public static bool Is<T>(object obj, out T result)
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
        var itemRegistry = Registry.Instance.ItemRegistry.ConvertToList();
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

        return itemDefinitions.ToList();
    }

    public static IEnumerator WaitForNotNull([MaybeNull] object obj, float timeout = Single.NaN, Action onTimeout = null, Action onFinish = null)
    {
        float startTime = Time.time;

        while (obj == null)
        {
            if (!float.IsNaN(timeout) && Time.time - startTime > timeout)
            {
                onTimeout?.Invoke();
                yield break;
            }

            yield return null;
        }
        onFinish?.Invoke();
    }

    public static IEnumerator WaitForNetworkSingleton<T>(IEnumerator coroutine) where T : NetworkSingleton<T>
    {
        while (!NetworkSingleton<T>.InstanceExists)
            yield return null;

        yield return coroutine;
    }
    
    public static IEnumerator WaitForPlayer(IEnumerator routine)
    {
        while (Player.Local == null || Player.Local.gameObject == null)
            yield return null;

        MelonCoroutines.Start(routine);
    }    
    
    public static T GetNotNullWithTimeout<T>(Func<T> getter, float timeout = 5f) where T : class
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalSeconds < timeout)
        {
            var result = getter();
            if (result != null)
                return result;

            Thread.Sleep(100);
        }

        Logger.Error($"Timed out waiting for {typeof(T).Name} to be not null");
        return null;
    }
}