using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopSupportSlotUI : MonoBehaviour
{
    [SerializeField] private SupportOptionData optionData;
    [SerializeField] private Image             iconImage;
    [SerializeField] private Text              nameText;
    [SerializeField] private Button            buyButton;

    private void OnEnable()
    {
        if (buyButton == null)
            buyButton = GetComponentInChildren<Button>();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyClicked);
            buyButton.onClick.AddListener(OnBuyClicked);
            Debug.Log($"[ShopSupportSlotUI] OnEnable — buyButton 연결, interactable={buyButton.interactable}");
        }
        else
            Debug.LogError($"[ShopSupportSlotUI] OnEnable — buyButton 없음! ({gameObject.name})");

        CubeSystem.OnCubeChanged      += OnCubeChanged;
        ShopSystem.OnInventoryChanged += OnInventoryChanged;
        Refresh();
        Debug.Log($"[ShopSupportSlotUI] OnEnable — optionData={optionData?.optionType.ToString() ?? "null"}, interactable={buyButton?.interactable}");
    }

    private void OnDisable()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);

        CubeSystem.OnCubeChanged      -= OnCubeChanged;
        ShopSystem.OnInventoryChanged -= OnInventoryChanged;
    }

    private void OnCubeChanged(CubeType type, int _)
    {
        if (type == CubeType.Lower) RefreshBuyButton();
    }

    private void OnInventoryChanged() => RefreshBuyButton();

    private void Refresh()
    {
        if (optionData == null)
        {
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        if (iconImage != null) iconImage.sprite = optionData.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(optionData.displayName)
                ? optionData.optionType.ToString()
                : optionData.displayName;

        RefreshBuyButton();
    }

    private void RefreshBuyButton()
    {
        if (buyButton == null) return;
        if (optionData == null) { buyButton.interactable = false; return; }

        bool hasShop   = ShopSystem.Instance != null;
        bool hasLower  = CubeSystem.Instance != null &&
                         CubeSystem.Instance.GetCount(CubeType.Lower) >= 1;
        bool inCatalog = hasShop && ShopSystem.Instance.IsAvailableSupport(optionData);
        bool notOwned  = hasShop && !ShopSystem.Instance.OwnedSupports.Contains(optionData);

        Debug.Log($"[ShopSupportSlotUI] RefreshBuyButton — hasShop={hasShop}, hasLower={hasLower}, inCatalog={inCatalog}, notOwned={notOwned}");
        buyButton.interactable = hasLower && inCatalog && notOwned;
    }

    private void OnBuyClicked()
    {
        Debug.Log($"[ShopSupportSlotUI] OnBuyClicked — optionData={optionData?.optionType.ToString() ?? "null"}");
        if (ShopSystem.Instance == null || optionData == null) return;
        bool result = ShopSystem.Instance.BuySupportOption(optionData);
        Debug.Log($"[ShopSupportSlotUI] BuySupportOption 결과={result}");
    }
}
