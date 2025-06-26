using FurnitureDelivery.Helpers;
using FurnitureDelivery.Interop;
using System.Collections;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;

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

[assembly: MelonOptionalDependencies("MoreGuns", "Toileportation", "UpgradedTrashCans", "DeliveryApp++")]

namespace FurnitureDelivery;

public static class BuildInfo
{
    public const string Name = "FurnitureDelivery";
    public const string Description = "Adds a custom delivery shops for furniture items";
    public const string Author = "k073l";
    public const string Version = "1.7.1";
}

public class FurnitureDelivery : MelonMod
{
    private static MelonLogger.Instance MelonLogger { get; set; }

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
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (RegisteredMelons.Any(m => m.Info.Name.Contains("Toileportation")))
        {
            if (sceneName == "Main")
            {
                MelonCoroutines.Start(Utils.WaitForNetworkSingleton<DeliveryManager>(OnDeliveryManagerReady()));
                MelonLogger.Debug("Repatching CanOrder");
                DeliveryShopCanOrderPatch.ApplyManualPatch();
            }

            if (sceneName == "Menu")
            {
                // cleanup
                if (DeliveryManager.Instance != null)
                {
                    DeliveryManager.Instance.onDeliveryCreated.RemoveListener(
                        (UnityAction<DeliveryInstance>)ToileportationInterop.OnDeliveryCreated);
                }

                ToileportationInterop.GoldenToiletListing = null;
            }
        }

        if (sceneName == "Main")
        {
            DeliveryAppAwakePatch.AddedShops = false; // reset the flag to allow adding shops again if exited to menu
            if (RegisteredMelons.Any(m => m.Info.Name.Contains("DeliveryApp++")))
            {
                MelonLogger.Msg("DeliveryAppPlusPlus detected. Applying patches");
                DeliveryAppPlusPlusInterop.ApplyPatches();
            }
        }
    }

    private static IEnumerator OnDeliveryManagerReady()
    {
        MelonLogger.Debug($"Delivery manager ready");
        DeliveryManager.Instance.onDeliveryCreated.AddListener(
            (UnityAction<DeliveryInstance>)ToileportationInterop.OnDeliveryCreated);
        yield break;
    }
}