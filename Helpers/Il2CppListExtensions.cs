namespace FurnitureDelivery.Helpers;

public static class Il2CppListExtensions
{
    public static IEnumerable<T> AsEnumerable<T>(this List<T> list)
    {
        return list ?? [];
    }

#if !MONO
    public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this IEnumerable<T> source)
    {
        var il2CppList = new Il2CppSystem.Collections.Generic.List<T>();
        foreach (var item in source)
            il2CppList.Add(item);
        return il2CppList;
    }

    public static List<T> ConvertToList<T>(this Il2CppSystem.Collections.Generic.List<T> il2CppList)
    {
        List<T> csharpList = new List<T>();
        T[] array = il2CppList.ToArray();
        csharpList.AddRange(array);
        return csharpList;
    }

    public static IEnumerable<T> AsEnumerable<T>(this Il2CppSystem.Collections.Generic.List<T> list)
    {
        return list == null ? [] : list._items.Take(list._size);
    }
#endif
}
