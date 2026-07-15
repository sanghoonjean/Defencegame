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
        Debug.Log($"[ShopSupportSlotUI] Awake — {gameObject.name}, buyButton={buyButton != null}");
        if (buyButton == null)
        {
            buyButton = GetComponentInChildren<Button>();
            Debug.Log($"[ShopSupportSlotUI] GetComponentInChildren<Button> 결과={buyButton?.name ?? "null"}");
        }

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
        else
            Debug.LogError($"[ShopSupportSlotUI] buyButton을 찾지 못함 ({gameObject.name})");

        // 상점 서포트 호버 툴팁 (#402)
        var tooltip = gameObject.GetComponent<ItemTooltipTrigger>()
                   ?? gameObject.AddComponent<ItemTooltipTrigger>();
        tooltip.TextSource = () => optionData != null ? ItemTooltipTrigger.BuildSupportText(optionData) : null;
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

        buyButton.interactable = hasLower && inCatalog;
    }

    private void OnBuyClicked()
    {
        Debug.Log($"[ShopSupportSlotUI] OnBuyClicked — optionData={optionData?.optionType.ToString() ?? "null"}");
        if (ShopSystem.Instance == null || optionData == null) return;
        bool result = ShopSystem.Instance.BuySupportOption(optionData);
        Debug.Log($"[ShopSupportSlotUI] BuySupportOption 결과={result}");
    }
}
