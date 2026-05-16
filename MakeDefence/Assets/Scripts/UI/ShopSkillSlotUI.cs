using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop 패널 내 개별 스킬 슬롯에 부착.
/// Inspector에서 SkillData 에셋을 지정하면 아이콘/이름을 표시하고,
/// 큐브 잔량에 따라 구매 버튼을 활성/비활성화한다.
/// </summary>
public class ShopSkillSlotUI : MonoBehaviour
{
    [SerializeField] private SkillData skillData;
    [SerializeField] private Image     iconImage;
    [SerializeField] private Text      nameText;
    [SerializeField] private Button    buyButton;

    private void Awake()
    {
        if (buyButton == null)
            buyButton = GetComponentInChildren<Button>();

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
        else
            Debug.LogError($"[ShopSkillSlotUI] buyButton을 찾지 못함 — 자식 Button 컴포넌트를 확인하세요 ({gameObject.name})");
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
        if (skillData == null)
        {
            Debug.LogWarning($"[ShopSkillSlotUI] skillData가 null — Inspector에서 SkillData 에셋을 연결하세요 ({gameObject.name})");
            return;
        }

        if (iconImage != null) iconImage.sprite = skillData.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(skillData.displayName)
                ? skillData.skillType.ToString()
                : skillData.displayName;

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
        Debug.Log($"[ShopSkillSlotUI] OnBuyClicked — skillData={skillData?.skillType.ToString() ?? "null"}, ShopSystem={ShopSystem.Instance != null}");
        if (ShopSystem.Instance == null || skillData == null) return;
        bool result = ShopSystem.Instance.BuySkill(skillData);
        Debug.Log($"[ShopSkillSlotUI] BuySkill 결과: {result}");
        RefreshBuyButton();
    }
}
