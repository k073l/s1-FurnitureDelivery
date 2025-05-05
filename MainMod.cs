using MelonLoader;

[assembly:
    MelonInfo(typeof(FurnitureDelivery.FurnitureDelivery), FurnitureDelivery.BuildInfo.Name,
        FurnitureDelivery.BuildInfo.Version,
        FurnitureDelivery.BuildInfo.Author)]
[assembly: MelonColor(1, 255, 215, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace FurnitureDelivery;

public static class BuildInfo
{
    public const string Name = "FurnitureDelivery";
    public const string Description = "Adds a custom delivery shops for furniture items";
    public const string Author = "k073l";
    public const string Version = "1.2";
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