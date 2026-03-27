using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using System.Collections;
using System.Reflection;
using MelonLoader;
using UnityEngine;

#if MONO
using ScheduleOne.Delivery;
#else
using Il2CppScheduleOne.Delivery;
#endif

[assembly:
    MelonInfo(typeof(FurnitureDelivery.FurnitureDelivery), FurnitureDelivery.BuildInfo.Name,
        FurnitureDelivery.BuildInfo.Version,
        FurnitureDelivery.BuildInfo.Author)]
[assembly: MelonColor(1, 255, 215, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

[assembly: MelonOptionalDependencies("MoreGuns", "Toileportation", "UpgradedTrashCans", "DeliveryApp++", "MetalStorage", "Absurdely Better Delivery")]

#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#endif

namespace FurnitureDelivery;

public static class BuildInfo
{
    public const string Name = "FurnitureDelivery";
    public const string Description = "Adds a custom delivery shops for furniture items";
    public const string Author = "k073l";
    public const string Version = "2.0.0";
}

public class FurnitureDelivery : MelonMod
{
    private static MelonLogger.Instance MelonLogger { get; set; }

    internal static Sprite StanMugshot => GetIcon(ref _stanMugshot, "FurnitureDelivery.assets.stan_mugshot.png");
    private static Sprite _stanMugshot;

    public override void OnInitializeMelon()
    {
        MelonLogger = LoggerInstance;
        MelonLogger.Msg("FurnitureDelivery initialized");

        if (RegisteredMelons.Any(m => m.Info.Name.Contains("MoreGuns")))
        {
            MelonLogger.Msg("MoreGuns detected. Adding ak47 to Armory");
        }

        if (RegisteredMelons.Any(m => m.Info.Name.Contains("Toileportation")))
        {
            MelonLogger.Msg("Toileportation detected. Adding Golden Toilet to Herbert's shop");
        }

        if (RegisteredMelons.Any(m => m.Info.Name.Contains("UpgradedTrashCans")))
        {
            MelonLogger.Msg("UpgradedTrashCans detected. Adding trash bins to Dan's shop");
        }

        if (RegisteredMelons.Any(m => m.Info.Name.Contains("MetalStorage")))
        {
            MelonLogger.Msg("MetalStorage detected. Adding metal storage racks to Dan's shop");
        }

        if (RegisteredMelons.Any(m => m.Info.Name.Contains("BigSprinklerLogic")))
        {
            MelonLogger.Msg("BigSprinklerLogic detected. Adding big sprinkler to Dan's shop");
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (RegisteredMelons.Any(m => m.Info.Name.Contains("Toileportation")))
        {
            switch (sceneName)
            {
                case "Main":
                    MelonCoroutines.Start(Utils.WaitForNetworkSingleton<DeliveryManager>(OnDeliveryManagerReady()));
                    break;
                case "Menu":
                {
                    // cleanup
                    if (DeliveryManager.Instance != null)
                    {
                        DeliveryManager.Instance.onDeliveryCreated += (Action<DeliveryInstance>)(di => ToileportationInterop.OnDeliveryCreated(di));
                    }

                    ToileportationInterop.GoldenToiletListing = null;
                    break;
                }
            }
        }

        switch (sceneName)
        {
            case "Main":
            {
                if (RegisteredMelons.Any(m => m.Info.Name.Contains("DeliveryApp++")))
                {
                    MelonLogger.Msg("DeliveryAppPlusPlus detected. Applying patches");
                    DeliveryAppPlusPlusInterop.ApplyPatches();
                }

                break;
            }
            case "Menu":
                DeliveryAppAwakePatch.AddedShops = false;
                DeliveryAppStartPatch.Initialized = false;
                break;
        }
    }

    private static IEnumerator OnDeliveryManagerReady()
    {
        MelonLogger.Debug($"Delivery manager ready");
        DeliveryManager.Instance.onDeliveryCreated += (Action<DeliveryInstance>)(di => ToileportationInterop.OnDeliveryCreated(di));
        yield break;
    }
    
    private static Sprite LoadEmbeddedPNG(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        var texture = new Texture2D(2, 2);
        if (!texture.LoadImage(data)) return null;

        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        if (sprite != null) sprite.name = resourceName;
        return sprite;
    }

    private static Sprite GetIcon(ref Sprite spriteField, string resourceName)
    {
        if (spriteField == null)
        {
            spriteField = LoadEmbeddedPNG(resourceName);
        }

        return spriteField;
    }
}