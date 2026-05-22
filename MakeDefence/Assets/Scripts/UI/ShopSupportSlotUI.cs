using UnityEngine;
using UnityEngine.UI;

public class ShopSupportSlotUI : MonoBehaviour
{
    [SerializeField] private SupportOptionData optionData;
    [SerializeField] private Image             iconImage;
    [SerializeField] private Text              nameText;
    [SerializeField] private Button            buyButton;

    private void Awake()
    {
        if (buyButton == null)
            buyButton = GetComponentInChildren<Button>();

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnEnable()
    {
        CubeSystem.OnCubeChanged    += OnCubeChanged;
        ShopSystem.OnInventoryChanged += OnInventoryChanged;
        Refresh();
    }

    private void OnDisable()
    {
        CubeSystem.OnCubeChanged    -= OnCubeChanged;
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
        bool notOwned  = hasShop &&
                         !ShopSystem.Instance.OwnedSupports.Contains(optionData);

        buyButton.interactable = hasLower && notOwned;
    }

    private void OnBuyClicked()
    {
        if (ShopSystem.Instance == null || optionData == null) return;
        ShopSystem.Instance.BuySupportOption(optionData);
    }
}
