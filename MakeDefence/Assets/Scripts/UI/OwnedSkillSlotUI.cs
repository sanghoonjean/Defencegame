using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 패널 내 보유 스킬 슬롯에 부착.
/// Setup() 호출로 스킬 정보를 표시하고, 장착 버튼 클릭 시
/// 현재 선택된 타워에 스킬을 장착한다.
/// </summary>
public class OwnedSkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image  iconImage;
    [SerializeField] private Text   nameText;
    [SerializeField] private Button equipButton;

    private SkillData _skillData;

    private void Awake()
    {
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);
    }

    public void Setup(SkillData skill)
    {
        _skillData = skill;

        if (iconImage != null) iconImage.sprite = skill.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(skill.displayName)
                ? skill.skillType.ToString()
                : skill.displayName;

        RefreshEquipButton();
        InventorySystem.OnTowerSelected += OnTowerSelected;
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= OnTowerSelected;
    }

    private void OnTowerSelected(Tower _) => RefreshEquipButton();

    private void RefreshEquipButton()
    {
        if (equipButton != null)
            equipButton.interactable = InventorySystem.Instance != null &&
                                       InventorySystem.Instance.SelectedTower != null;
    }

    public void OnEquipClicked()
    {
        if (_skillData == null || InventorySystem.Instance == null) return;
        InventorySystem.Instance.EquipSkill(_skillData);
    }
}
