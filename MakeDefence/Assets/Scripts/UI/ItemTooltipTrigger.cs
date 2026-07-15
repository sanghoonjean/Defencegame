using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 슬롯 호버 시 아이템 상세 툴팁을 표시한다.
/// 기본은 같은 GameObject 의 InvenSlotDragHandler 에서 스킬/서포트/차원석 데이터를 읽고,
/// 데이터 출처가 다른 슬롯(장착/상점, #402)은 TextSource 델리게이트로 텍스트를 위임한다.
/// 각 슬롯 UI 가 런타임 부착 — 씬 수정 불필요.
/// </summary>
public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
{
    /// <summary>지정 시 InvenSlotDragHandler 대신 이 델리게이트가 툴팁 텍스트를 공급한다.
    /// 호버 시점에 호출되므로 장착 상태 변화를 따로 반영할 필요가 없다. null/빈 문자열 반환 = 미표시.</summary>
    public System.Func<string> TextSource { get; set; }

    private InvenSlotDragHandler _slot;

    private void Awake() => _slot = GetComponent<InvenSlotDragHandler>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        string content = TextSource != null ? TextSource() : BuildFromSlot();
        if (string.IsNullOrEmpty(content)) return;
        ItemTooltipUI.Show((RectTransform)transform, content);
    }

    private string BuildFromSlot()
    {
        if (_slot == null || !_slot.HasItem) return null;
        return BuildText(_slot);
    }

    public void OnPointerExit(PointerEventData eventData) => ItemTooltipUI.Hide();

    // 드래그 고스트가 뜨는 동안 툴팁이 겹치지 않도록 숨김
    public void OnBeginDrag(PointerEventData eventData) => ItemTooltipUI.Hide();

    private void OnDisable() => ItemTooltipUI.Hide();

    private static string BuildText(InvenSlotDragHandler slot)
    {
        if (slot.Skill   != null) return BuildSkillText(slot.Skill);
        if (slot.Support != null) return BuildSupportText(slot.Support);
        if (slot.Stone   != null) return BuildStoneText(slot.Stone);
        return null;
    }

    public static string BuildSkillText(SkillData skill)
    {
        var sb = new StringBuilder();
        sb.Append("<b>").Append(skill.displayName).Append("</b>");
        sb.Append("\nDamage ").Append(skill.baseDamage.ToString("0.#"));
        sb.Append("\nCooldown ").Append(skill.baseCooldown.ToString("0.#")).Append('s');
        sb.Append("\nRange ").Append(skill.baseRange.ToString("0.#"));
        if (skill.manaCost > 0f)
            sb.Append("\nMana ").Append(skill.manaCost.ToString("0.#"));
        return sb.ToString();
    }

    public static string BuildSupportText(SupportOptionData support)
    {
        var sb = new StringBuilder();
        sb.Append("<b>").Append(support.displayName).Append("</b>");
        if (!string.IsNullOrEmpty(support.description))
            sb.Append('\n').Append(support.description);
        if (support.value > 0f)
            sb.Append("\nValue +").Append(Mathf.RoundToInt(support.value * 100f)).Append('%');
        return sb.ToString();
    }

    public static string BuildStoneText(DimensionStone stone)
    {
        var sb = new StringBuilder();
        string name = ShopSystem.Instance != null ? ShopSystem.Instance.StoneDisplayName : "Dimension Stone";
        sb.Append("<b>").Append(name).Append("</b>");
        sb.Append("\nGrade <color=").Append(GradeColor(stone.Grade)).Append('>')
          .Append(stone.Grade).Append("</color>");
        foreach (var opt in stone.Options)
        {
            sb.Append('\n').Append(OptionLabel(opt.Type)).Append(" +").Append(opt.Value.ToString("0.#"));
            if (opt.Type != DimensionStoneOptionType.MonsterCountBoost)
                sb.Append('%'); // Count 는 마리 수, 나머지는 % (DimensionStone.Ranges 주석 참조)
        }
        return sb.ToString();
    }

    private static string GradeColor(StoneGrade grade) => grade switch
    {
        StoneGrade.Magic  => "#6FA8FF",
        StoneGrade.Rare   => "#FFD75B",
        StoneGrade.Unique => "#FF8C4B",
        _                 => "#FFFFFF",
    };

    private static string OptionLabel(DimensionStoneOptionType type) => type switch
    {
        DimensionStoneOptionType.MonsterHpBoost      => "Monster HP",
        DimensionStoneOptionType.MonsterDefenseBoost => "Monster Defense",
        DimensionStoneOptionType.MonsterSpeedBoost   => "Monster Speed",
        DimensionStoneOptionType.MonsterCountBoost   => "Monster Count",
        DimensionStoneOptionType.RewardCubeBoost     => "Cube Reward",
        DimensionStoneOptionType.EnemyDamageBoost    => "Enemy Damage",
        _                                            => type.ToString(),
    };
}
