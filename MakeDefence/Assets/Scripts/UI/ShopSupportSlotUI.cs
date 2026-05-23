using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopSupportSlotUI : MonoBehaviour
{
    [SerializeField] private SupportOptionData optionData;
    [SerializeField] private Image             iconImage;
    [SerializeField] private Text              nameText;
    [SerializeField] private Button            buyButton;

    private bool _started;

    private void Awake()
    {
        if (buyButton == null)
        {
            Debug.LogError($"[ShopSupportSlotUI] buyButton이 연결되지 않음 — Inspector에서 Buy Button 필드를 설정하세요 ({gameObject.name})");
            return;
        }
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void Start()
    {
        _started = true;
        Refresh();
    }

    private void OnEnable()
    {
        CubeSystem.OnCubeChanged      += OnCubeChanged;
        ShopSystem.OnInventoryChanged += OnInventoryChanged;
        if (_started) Refresh();
    }

    private void OnDisable()
    {
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

        buyButton.interactable = hasLower && inCatalog && notOwned;
    }

    private void OnBuyClicked()
    {
        if (ShopSystem.Instance == null || optionData == null) return;
        ShopSystem.Instance.BuySupportOption(optionData);
    }
}
