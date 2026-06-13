using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }

    public static event Action OnInventoryChanged;

    [SerializeField] private List<SkillData>         availableSkills;
    [SerializeField] private List<SupportOptionData> availableSupports;

    private readonly List<SkillData>         _ownedSkills   = new();
    private readonly List<SupportOptionData> _ownedSupports = new();
    private readonly List<DisplayEntry>      _displayOrder  = new();

    public IReadOnlyList<SkillData>         OwnedSkills        => _ownedSkills;
    public IReadOnlyList<SupportOptionData> OwnedSupports      => _ownedSupports;
    public IReadOnlyList<DisplayEntry>      OwnedDisplayOrder  => _displayOrder;

    public readonly struct DisplayEntry
    {
        public readonly InventoryItemKind Kind;
        public readonly int               DataIndex;
        public DisplayEntry(InventoryItemKind kind, int dataIndex) { Kind = kind; DataIndex = dataIndex; }
    }

    public readonly struct DisplayItem
    {
        public readonly InventoryItemKind Kind;
        public readonly SkillData         Skill;
        public readonly SupportOptionData Support;
        public DisplayItem(SkillData skill)         { Kind = InventoryItemKind.Skill;   Skill = skill;   Support = null; }
        public DisplayItem(SupportOptionData supp)  { Kind = InventoryItemKind.Support; Skill = null;    Support = supp; }
        public Sprite Icon        => Kind == InventoryItemKind.Skill ? Skill?.icon        : Support?.icon;
        public string DisplayName => Kind == InventoryItemKind.Skill ? Skill?.displayName : Support?.displayName;
    }

    private void Awake() { Instance = this; }

    public DisplayItem GetDisplayItem(int displayIdx)
    {
        if (displayIdx < 0 || displayIdx >= _displayOrder.Count) return default;
        var entry = _displayOrder[displayIdx];
        if (entry.Kind == InventoryItemKind.Skill)
        {
            if (entry.DataIndex < 0 || entry.DataIndex >= _ownedSkills.Count) return default;
            return new DisplayItem(_ownedSkills[entry.DataIndex]);
        }
        if (entry.DataIndex < 0 || entry.DataIndex >= _ownedSupports.Count) return default;
        return new DisplayItem(_ownedSupports[entry.DataIndex]);
    }

    public bool BuySkill(SkillData skill)
    {
        Debug.Log($"[ShopSystem] BuySkill 시도 — skill={skill?.skillType.ToString() ?? "null"}, availableSkills={availableSkills.Count}개");
        if (!availableSkills.Contains(skill))
        {
            Debug.LogWarning($"[ShopSystem] BuySkill 실패 — availableSkills에 {skill?.skillType} 없음");
            return false;
        }
        int lowerCount = CubeSystem.Instance != null ? CubeSystem.Instance.GetCount(CubeType.Lower) : -1;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1))
        {
            Debug.LogWarning($"[ShopSystem] BuySkill 실패 — Lower 큐브 부족 (현재 {lowerCount}개)");
            return false;
        }
        AddSkillInternal(skill);
        Debug.Log($"[ShopSystem] BuySkill 성공 — {skill.skillType} 구매, 보유 스킬 총 {_ownedSkills.Count}개");
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool IsAvailableSupport(SupportOptionData option) => availableSupports.Contains(option);

    public bool BuySupportOption(SupportOptionData option)
    {
        if (!availableSupports.Contains(option))    return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1)) return false;
        AddSupportInternal(option);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ReturnSkill(SkillData skill)
    {
        if (skill == null) return;
        AddSkillInternal(skill);
        OnInventoryChanged?.Invoke();
    }

    public void ReturnSupportOption(SupportOptionData option)
    {
        if (option == null) return;
        AddSupportInternal(option);
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveByDisplayIndex(int displayIdx)
    {
        if (displayIdx < 0 || displayIdx >= _displayOrder.Count) return false;
        var entry = _displayOrder[displayIdx];
        if (entry.Kind == InventoryItemKind.Skill)
        {
            if (entry.DataIndex < 0 || entry.DataIndex >= _ownedSkills.Count) return false;
            _ownedSkills.RemoveAt(entry.DataIndex);
            ShiftDataIndexAfterRemove(InventoryItemKind.Skill, entry.DataIndex);
        }
        else
        {
            if (entry.DataIndex < 0 || entry.DataIndex >= _ownedSupports.Count) return false;
            _ownedSupports.RemoveAt(entry.DataIndex);
            ShiftDataIndexAfterRemove(InventoryItemKind.Support, entry.DataIndex);
        }
        _displayOrder.RemoveAt(displayIdx);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool SwapDisplayOrder(int indexA, int indexB)
    {
        if (indexA < 0 || indexB < 0) return false;
        if (indexA >= _displayOrder.Count || indexB >= _displayOrder.Count) return false;
        if (indexA == indexB) return false;
        (_displayOrder[indexA], _displayOrder[indexB]) = (_displayOrder[indexB], _displayOrder[indexA]);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool MoveDisplayOrder(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _displayOrder.Count) return false;
        if (toIndex < 0) return false;
        if (fromIndex == toIndex) return false;
        var entry = _displayOrder[fromIndex];
        _displayOrder.RemoveAt(fromIndex);
        int insertAt = Mathf.Min(toIndex > fromIndex ? toIndex - 1 : toIndex, _displayOrder.Count);
        _displayOrder.Insert(insertAt, entry);
        OnInventoryChanged?.Invoke();
        return true;
    }

    // ---- 하위호환 (deprecated) ----
    // 자산 참조 기반 제거: 첫 매치만 제거. 중복 보유 시 잘못된 사본 제거 가능.
    // 새 코드는 RemoveByDisplayIndex(int) 사용 권장.

    public bool RemoveOwnedSkill(SkillData skill)
    {
        if (skill == null) return false;
        int displayIdx = FindFirstDisplayIndex(InventoryItemKind.Skill, skill);
        return displayIdx >= 0 && RemoveByDisplayIndex(displayIdx);
    }

    public bool RemoveOwnedSupportOption(SupportOptionData option)
    {
        if (option == null) return false;
        int displayIdx = FindFirstDisplayIndex(InventoryItemKind.Support, option);
        return displayIdx >= 0 && RemoveByDisplayIndex(displayIdx);
    }

    public bool SwapOwnedSkills(int indexA, int indexB)
    {
        int displayA = FindDisplayIndexBySkillDataIndex(indexA);
        int displayB = FindDisplayIndexBySkillDataIndex(indexB);
        if (displayA < 0 || displayB < 0) return false;
        return SwapDisplayOrder(displayA, displayB);
    }

    public bool MoveOwnedSkill(int fromIndex, int toIndex)
    {
        int displayFrom = FindDisplayIndexBySkillDataIndex(fromIndex);
        if (displayFrom < 0) return false;
        // toIndex가 스킬 List 인덱스라는 옛 시맨틱은 통합 displayOrder 에서 모호하므로
        // 클램프된 displayOrder 인덱스로 해석. 정확한 자유 재배치는 MoveDisplayOrder 사용 권장.
        int displayTo = Mathf.Clamp(toIndex, 0, _displayOrder.Count);
        return MoveDisplayOrder(displayFrom, displayTo);
    }

    // ---- 내부 ----

    private void AddSkillInternal(SkillData skill)
    {
        _ownedSkills.Add(skill);
        _displayOrder.Add(new DisplayEntry(InventoryItemKind.Skill, _ownedSkills.Count - 1));
    }

    private void AddSupportInternal(SupportOptionData option)
    {
        _ownedSupports.Add(option);
        _displayOrder.Add(new DisplayEntry(InventoryItemKind.Support, _ownedSupports.Count - 1));
    }

    private void ShiftDataIndexAfterRemove(InventoryItemKind kind, int removedDataIndex)
    {
        for (int i = 0; i < _displayOrder.Count; i++)
        {
            var e = _displayOrder[i];
            if (e.Kind == kind && e.DataIndex > removedDataIndex)
                _displayOrder[i] = new DisplayEntry(e.Kind, e.DataIndex - 1);
        }
    }

    private int FindFirstDisplayIndex(InventoryItemKind kind, ScriptableObject asset)
    {
        for (int i = 0; i < _displayOrder.Count; i++)
        {
            var e = _displayOrder[i];
            if (e.Kind != kind) continue;
            if (kind == InventoryItemKind.Skill && _ownedSkills[e.DataIndex] == asset) return i;
            if (kind == InventoryItemKind.Support && _ownedSupports[e.DataIndex] == asset) return i;
        }
        return -1;
    }

    private int FindDisplayIndexBySkillDataIndex(int dataIndex)
    {
        for (int i = 0; i < _displayOrder.Count; i++)
        {
            var e = _displayOrder[i];
            if (e.Kind == InventoryItemKind.Skill && e.DataIndex == dataIndex) return i;
        }
        return -1;
    }
}
