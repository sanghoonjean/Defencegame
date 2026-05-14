using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit_C_Panel/Skill Panel/Main Skill GameObject에 부착.
/// InventorySystem.OnTowerSelected 이벤트를 구독하여
/// 선택된 타워의 공격 스킬 아이콘과 이름을 표시한다.
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image  iconImage;
    [SerializeField] private Text   skillNameText;
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

    private void Refresh(Tower tower)
    {
        var skill = tower?.EquippedSkill;
        bool hasSkill = skill != null;

        Debug.Log($"[SkillSlotUI] Refresh — tower={tower?.name ?? "null"} | skill={skill?.skillType.ToString() ?? "null"} | hasSkill={hasSkill} | iconImage={iconImage != null} | icon={skill?.icon?.name ?? "null"}");

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasSkill);
            iconImage.sprite = hasSkill ? skill.icon : null;
            Debug.Log($"[SkillSlotUI] iconImage.gameObject.activeSelf={iconImage.gameObject.activeSelf} | iconImage.enabled={iconImage.enabled} | sprite={iconImage.sprite?.name ?? "null"} | parent.active={iconImage.transform.parent?.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[SkillSlotUI] iconImage 참조가 null — Inspector에서 연결 확인 필요");
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
