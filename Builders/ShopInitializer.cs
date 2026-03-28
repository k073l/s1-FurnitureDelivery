using FurnitureDelivery.Helpers;
using FurnitureDelivery.Shops.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
#endif

namespace FurnitureDelivery.Builders;

public class ShopInitializer
{
    private readonly DeliveryShop _shopInstance;
    private readonly ShopInterface _shopInterface;

    public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-ShopInitializer");

    public ShopInitializer(DeliveryShop shopInstance, ShopInterface shopInterface)
    {
        _shopInstance = shopInstance;
        _shopInterface = shopInterface;
    }

    public void Initialize()
    {
        _shopInstance.gameObject.SetActive(true);

        try
        {
            CreateListingEntries();
            SetupButtonListeners();
            Logger.Debug("Initialize complete");
        }
        catch (System.Exception initEx)
        {
            Logger.Error($"Initialize failed: {initEx.Message}");
        }

        _shopInstance.gameObject.SetActive(false);
    }

    private void CreateListingEntries()
    {
        foreach (var listing in _shopInterface.Listings)
        {
            if (!listing.CanBeDelivered) continue;
            try
            {
                var entry = Object.Instantiate(_shopInstance.ListingEntryPrefab, _shopInstance.ListingContainer);
                entry.Initialize(listing);
                entry.onQuantityChanged.AddListener((UnityAction)(_shopInstance.RefreshCart));
                _shopInstance.OnSelect += (Action<DeliveryShop>)((shop) => entry.RefreshLocked());
                _shopInstance.listingEntries.Add(entry);
            }
            catch (System.Exception listingEx)
            {
                Logger.Warning($"Failed to create entry for {listing.name}: {listingEx.Message}");
            }
        }
    }

    private void SetupButtonListeners()
    {
        if (_shopInstance.BackButton != null)
        {
            _shopInstance.BackButton.onClick.RemoveAllListeners();
            _shopInstance.BackButton.onClick.AddListener((UnityAction)(() =>
                _shopInstance.OnSelect?.Invoke(_shopInstance)));
        }

        if (_shopInstance.OrderButton != null)
        {
            _shopInstance.OrderButton.onClick.RemoveAllListeners();
            _shopInstance.OrderButton.onClick.AddListener((UnityAction)(() =>
                _shopInstance.SubmitOrder(string.Empty)));
        }

        if (_shopInstance.DestinationDropdown != null)
        {
            _shopInstance.DestinationDropdown.onValueChanged.RemoveAllListeners();
            _shopInstance.DestinationDropdown.onValueChanged.AddListener(
                (UnityAction<int>)(_shopInstance.DestinationDropdownSelected));
        }

        if (_shopInstance.LoadingDockDropdown != null)
        {
            _shopInstance.LoadingDockDropdown.onValueChanged.RemoveAllListeners();
            _shopInstance.LoadingDockDropdown.onValueChanged.AddListener(
                (UnityAction<int>)(_shopInstance.LoadingDockDropdownSelected));
        }
    }
}