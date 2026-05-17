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
        CubeSystem.OnCubeChanged += OnCubeChanged;
        Refresh();
    }

    private void OnDisable()
    {
        CubeSystem.OnCubeChanged -= OnCubeChanged;
    }

    private void OnCubeChanged(CubeType type, int _)
    {
        if (type == CubeType.Lower) RefreshBuyButton();
    }

    private void Refresh()
    {
        if (optionData == null) return;

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
        bool canBuy = CubeSystem.Instance != null &&
                      CubeSystem.Instance.GetCount(CubeType.Lower) >= 1;
        buyButton.interactable = canBuy;
    }

    public void OnBuyClicked()
    {
        if (ShopSystem.Instance == null || optionData == null) return;
        ShopSystem.Instance.BuySupportOption(optionData);
        RefreshBuyButton();
    }
}
