using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop 패널 내 개별 스킬 슬롯에 부착.
/// Inspector에서 SkillData 에셋을 지정하면 아이콘/이름을 표시한다.
/// </summary>
public class ShopSkillSlotUI : MonoBehaviour
{
    [SerializeField] private SkillData skillData;
    [SerializeField] private Image     iconImage;
    [SerializeField] private Text      nameText;

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (skillData == null) return;

        if (iconImage != null) iconImage.sprite = skillData.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(skillData.displayName)
                ? skillData.skillType.ToString()
                : skillData.displayName;
    }
}
