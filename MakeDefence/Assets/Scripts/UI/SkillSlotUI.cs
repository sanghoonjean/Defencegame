using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Unit_C_Panel/Skill Panel/Main Skill GameObject에 부착.
/// InventorySystem.OnTowerSelected 이벤트를 구독하여
/// 선택된 타워의 공격 스킬 아이콘과 이름을 표시한다.
/// IDropHandler로 인벤토리 슬롯 드랍을 처리한다.
/// </summary>
public class SkillSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image      iconImage;
    [SerializeField] private Text       skillNameText;
    [SerializeField] private GameObject emptyLabel;

    private void OnEnable()
    {
        InventorySystem.OnTowerSelected += Refresh;
        Refresh(InventorySystem.Instance != null ? InventorySystem.Instance.SelectedTower : null);
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= Refresh;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag?.GetComponent<InvenSlotDragHandler>();
        if (drag == null || drag.Skill == null)
        {
            Debug.Log("[SkillSlotUI] 올바르지 않은 드랍 대상");
            return;
        }

        if (InventorySystem.Instance == null || InventorySystem.Instance.SelectedTower == null)
        {
            Debug.Log("[SkillSlotUI] 타워가 선택되지 않음 — 장착 불가");
            return;
        }

        var tower    = InventorySystem.Instance.SelectedTower;
        var newSkill = drag.Skill;

        // 기존 장착 스킬 → 인벤토리로 반환
        if (tower.EquippedSkill != null)
            ShopSystem.Instance?.ReturnSkill(tower.EquippedSkill);

        // 새 스킬 인벤토리에서 제거 후 장착
        ShopSystem.Instance?.RemoveOwnedSkill(newSkill);
        InventorySystem.Instance.EquipSkill(newSkill);
    }

    private void Refresh(Tower tower)
    {
        var skill    = tower?.EquippedSkill;
        bool hasSkill = skill != null;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasSkill);
            iconImage.sprite = hasSkill ? skill.icon : null;
        }

        if (skillNameText != null)
        {
            skillNameText.gameObject.SetActive(hasSkill);
            if (hasSkill)
                skillNameText.text = string.IsNullOrEmpty(skill.displayName)
                    ? skill.skillType.ToString()
                    : skill.displayName;
        }

        if (emptyLabel != null)
            emptyLabel.SetActive(!hasSkill);
    }
}
