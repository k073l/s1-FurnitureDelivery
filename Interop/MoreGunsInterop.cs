using MelonLoader;

namespace FurnitureDelivery.Interop;

public static class MoreGunsInterop
{
    public static MelonLogger.Instance Logger = new MelonLogger.Instance($"{BuildInfo.Name}-MoreGunsInterop");
    public static bool TryGetPrices(string weaponID, out float gunPrice, out float magPrice)
    {
        gunPrice = 0f;
        magPrice = 0f;

        string categoryName = $"MoreGuns-{weaponID} Settings";

        var category = MelonPreferences.GetCategory(categoryName);
        if (category == null)
        {
            Logger.Warning($"Could not find category: {categoryName}");
            return false;
        }

        var gunPriceEntry = category.GetEntry<float>($"{weaponID} Price");
        var magPriceEntry = category.GetEntry<float>($"{weaponID} Magazine Price");

        if (gunPriceEntry == null || magPriceEntry == null)
        {
            Logger.Warning($"Missing price entries for '{weaponID}'");
            return false;
        }

        gunPrice = gunPriceEntry.Value;
        magPrice = magPriceEntry.Value;
        return true;
    }
}
