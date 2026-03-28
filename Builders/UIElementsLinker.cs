using System.Linq;
using FurnitureDelivery.Helpers;
using FurnitureDelivery.Shops.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if MONO
using TMPro;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.ItemFramework;
#else
using Il2CppTMPro;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.ItemFramework;
#endif

namespace FurnitureDelivery.Builders;

public class UIElementsLinker
{
    private readonly DeliveryShop _shopInstance;
    private readonly ShopInterface _shopInterface;
    private readonly Color _shopColor;
    private readonly string _shopName;
    private readonly string _shopDescription;
    private readonly Sprite _shopImage;
    private readonly DeliveryShop _deliveryShopTemplate;

    public static MelonLogger.Instance Logger => new MelonLogger.Instance($"{BuildInfo.Name}-UIElementsLinker");

    public UIElementsLinker(
        DeliveryShop shopInstance,
        ShopInterface shopInterface,
        Color shopColor,
        string shopName,
        string shopDescription,
        Sprite shopImage,
        DeliveryShop deliveryShopTemplate)
    {
        _shopInstance = shopInstance;
        _shopInterface = shopInterface;
        _shopColor = shopColor;
        _shopName = shopName;
        _shopDescription = shopDescription;
        _shopImage = shopImage;
        _deliveryShopTemplate = deliveryShopTemplate;
    }

    public void LinkAll()
    {
        _shopInstance.ShopColor = _shopColor;
        _shopInstance.gameObject.name = $"DeliveryShop_{_shopName}";

        LinkHeader();
        LinkLabels();
        LinkButtons();
        LinkDropdowns();
        LinkListingContainer();

        _shopInstance.ListingEntryPrefab = _deliveryShopTemplate.ListingEntryPrefab;
        Logger.Debug(
            $"Set ListingEntryPrefab: {(_shopInstance.ListingEntryPrefab != null ? _shopInstance.ListingEntryPrefab.name : "NULL")}");

        _shopInstance.MatchingShopInterfaceName = _shopName;
        _shopInstance.MatchingShop = _shopInterface;
        Helpers.Registries.RegisterDeliveryFee(_shopInstance, GetDeliveryFee());
        _shopInstance.AvailableByDefault = GetAvailableByDefault();
    }

    protected virtual float GetDeliveryFee() => 100f;
    protected virtual bool GetAvailableByDefault() => true;

    private void LinkHeader()
    {
        var headerTransform = _shopInstance.transform.Find(UIPathConstants.Header)
                              ?? _shopInstance.transform.Find(UIPathConstants.HeaderFallback);

        if (headerTransform == null) return;

        var headerBg = headerTransform.GetComponent<Image>();
        if (headerBg != null)
            headerBg.color = _shopColor;

        var iconTransform = headerTransform.Find(UIPathConstants.IconImage);
        if (iconTransform != null && _shopImage != null)
        {
            var iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
                iconImage.sprite = _shopImage;
        }

        var titleTransform = headerTransform.Find(UIPathConstants.Title);
        if (titleTransform != null)
        {
            var titleText = titleTransform.GetComponent<Text>();
            if (titleText != null)
                titleText.text = _shopName;
        }

        var descTransform = headerTransform.Find(UIPathConstants.Description);
        if (descTransform != null)
        {
            var descText = descTransform.GetComponent<Text>();
            if (descText != null)
                descText.text = _shopDescription;
        }
    }

    private void LinkLabels()
    {
        var itemTotalTransform = _shopInstance.transform.Find(UIPathConstants.ItemTotal)
                                 ?? _shopInstance.transform.Find(UIPathConstants.ItemTotalLabelFallback);
        if (itemTotalTransform != null)
            _shopInstance.ItemTotalLabel = itemTotalTransform.GetComponent<Text>();

        var orderTotalTransform = _shopInstance.transform.Find(UIPathConstants.OrderTotal)
                                  ?? _shopInstance.transform.Find(UIPathConstants.OrderTotalLabelFallback);
        if (orderTotalTransform != null)
            _shopInstance.OrderTotalLabel = orderTotalTransform.GetComponent<Text>();

        var deliveryFeeTransform = _shopInstance.transform.Find(UIPathConstants.DeliveryFee)
                                   ?? _shopInstance.transform.Find(UIPathConstants.DeliveryFeeLabelFallback);
        if (deliveryFeeTransform != null)
            _shopInstance.DeliveryFeeLabel = deliveryFeeTransform.GetComponent<Text>();

        var deliveryTimeTransform = _shopInstance.transform.Find(UIPathConstants.DeliveryTime)
                                    ?? _shopInstance.transform.Find(UIPathConstants.DeliveryTimeLabelFallback);
        if (deliveryTimeTransform != null)
            _shopInstance.DeliveryTimeLabel = deliveryTimeTransform.GetComponent<Text>();
    }

    private void LinkButtons()
    {
        var backButtonTransform = _shopInstance.transform.Find(UIPathConstants.BackButton)
                                  ?? _shopInstance.transform.Find(UIPathConstants.BackButtonFallback);
        if (backButtonTransform != null)
            _shopInstance.BackButton = backButtonTransform.GetComponent<Button>();

        var orderButtonTransform = _shopInstance.transform.Find(UIPathConstants.Confirm)
                                   ?? _shopInstance.transform.Find(UIPathConstants.OrderButtonFallback);
        if (orderButtonTransform != null)
            _shopInstance.OrderButton = orderButtonTransform.GetComponent<Button>();
    }

    private void LinkDropdowns()
    {
        var destDropdownTransform = _shopInstance.transform.Find(UIPathConstants.DestinationDropdown)
                                    ?? _shopInstance.transform.Find(UIPathConstants.DestinationDropdownFallback);
        if (destDropdownTransform != null)
            _shopInstance.DestinationDropdown = destDropdownTransform.GetComponent<Dropdown>();

        var dockDropdownTransform = _shopInstance.transform.Find(UIPathConstants.LoadingDockDropdown)
                                    ?? _shopInstance.transform.Find(UIPathConstants.LoadingDockDropdownFallback);
        if (dockDropdownTransform != null)
            _shopInstance.LoadingDockDropdown = dockDropdownTransform.GetComponent<Dropdown>();
    }

    private void LinkListingContainer()
    {
        var listingContainerTransform = _shopInstance.transform.Find(UIPathConstants.Listings)
                                        ?? _shopInstance.transform.Find(UIPathConstants.ListingContainer)
                                        ?? _shopInstance.transform.Find(UIPathConstants.Entries);

        if (listingContainerTransform != null)
        {
            if (Helpers.Utils.Is<RectTransform>(listingContainerTransform, out var rectTransform))
                _shopInstance.ListingContainer = rectTransform;
            Logger.Debug($"Found ListingContainer: {_shopInstance.ListingContainer?.name}");
        }

        if (_shopInstance.ListingContainer != null)
        {
            for (var i = 0; i < _shopInstance.ListingContainer.transform.childCount; i++)
            {
                var listingChild = _shopInstance.ListingContainer.transform.GetChild(i);
                if (listingChild != null)
                {
                    Object.Destroy(listingChild.gameObject);
                }
            }

            Logger.Debug("Cleared children from ListingContainer");
        }
    }
}