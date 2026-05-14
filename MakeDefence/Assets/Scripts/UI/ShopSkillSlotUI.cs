using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop 패널 내 개별 스킬 슬롯에 부착.
/// Inspector에서 SkillData 에셋을 지정하면 아이콘/이름을 표시하고
/// 구매 버튼 클릭 시 하위 큐브 1개를 소모해 ShopSystem에 스킬을 추가한다.
/// </summary>
public class ShopSkillSlotUI : MonoBehaviour
{
    [SerializeField] private SkillData skillData;
    [SerializeField] private Image     iconImage;
    [SerializeField] private Text      nameText;
    [SerializeField] private Button    buyButton;

    private void OnEnable()
    {
        CubeSystem.OnCubeChanged      += OnCubeChanged;
        ShopSystem.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        CubeSystem.OnCubeChanged      -= OnCubeChanged;
        ShopSystem.OnInventoryChanged -= Refresh;
    }

    private void OnCubeChanged(CubeType type, int _)
    {
        if (type == CubeType.Lower) Refresh();
    }

    private void Refresh()
    {
        if (skillData == null) return;

        if (iconImage != null) iconImage.sprite = skillData.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(skillData.displayName)
                ? skillData.skillType.ToString()
                : skillData.displayName;

        bool canBuy = CubeSystem.Instance != null &&
                      CubeSystem.Instance.GetCount(CubeType.Lower) >= 1;
        if (buyButton != null) buyButton.interactable = canBuy;
    }

    public void OnBuyClicked()
    {
        if (ShopSystem.Instance == null || skillData == null) return;
        ShopSystem.Instance.BuySkill(skillData);
    }
}
