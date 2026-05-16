using UnityEngine;
using UnityEngine.UI;

public class OwnedSkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image  iconImage;
    [SerializeField] private Text   nameText;
    [SerializeField] private Button equipButton;

    private SkillData _skillData;

    public void Setup(SkillData skill)
    {
        _skillData = skill;

        if (iconImage != null) iconImage.sprite = skill.icon;
        if (nameText  != null)
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
            equipButton.interactable = InventorySystem.Instance != null
                                       && InventorySystem.Instance.SelectedTower != null;
    }

    // Inspector의 equipButton OnClick에 연결
    public void OnEquipClicked()
    {
        if (_skillData == null || InventorySystem.Instance == null) return;
        InventorySystem.Instance.EquipSkill(_skillData);
    }
}
