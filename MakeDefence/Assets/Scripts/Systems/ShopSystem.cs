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

    // 옛 시맨틱 보존: 인덱스는 _ownedSkills 의 인덱스. _ownedSkills 를 실제로 재정렬하고
    // _displayOrder 의 DataIndex 참조도 새 위치를 따르도록 보정.
    public bool SwapOwnedSkills(int indexA, int indexB)
    {
        if (indexA < 0 || indexB < 0) return false;
        if (indexA >= _ownedSkills.Count || indexB >= _ownedSkills.Count) return false;
        if (indexA == indexB) return false;

        (_ownedSkills[indexA], _ownedSkills[indexB]) = (_ownedSkills[indexB], _ownedSkills[indexA]);

        for (int i = 0; i < _displayOrder.Count; i++)
        {
            var e = _displayOrder[i];
            if (e.Kind != InventoryItemKind.Skill) continue;
            if (e.DataIndex == indexA)      _displayOrder[i] = new DisplayEntry(e.Kind, indexB);
            else if (e.DataIndex == indexB) _displayOrder[i] = new DisplayEntry(e.Kind, indexA);
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool MoveOwnedSkill(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _ownedSkills.Count) return false;
        if (toIndex < 0) return false;
        if (fromIndex == toIndex) return false;

        var skill = _ownedSkills[fromIndex];
        _ownedSkills.RemoveAt(fromIndex);
        int insertAt = Mathf.Min(toIndex > fromIndex ? toIndex - 1 : toIndex, _ownedSkills.Count);
        _ownedSkills.Insert(insertAt, skill);

        // DisplayEntry.DataIndex 재매핑:
        //  - 옛 fromIndex 항목 → insertAt
        //  - 옛 idx > fromIndex → -1 (remove shift), 그 후 idx >= insertAt → +1 (insert shift)
        //  - 옛 idx < fromIndex → 변화 없음, 그 후 idx >= insertAt → +1
        for (int i = 0; i < _displayOrder.Count; i++)
        {
            var e = _displayOrder[i];
            if (e.Kind != InventoryItemKind.Skill) continue;
            int newIdx;
            if (e.DataIndex == fromIndex)
            {
                newIdx = insertAt;
            }
            else
            {
                int afterRemove = e.DataIndex > fromIndex ? e.DataIndex - 1 : e.DataIndex;
                newIdx          = afterRemove >= insertAt ? afterRemove + 1 : afterRemove;
            }
            if (newIdx != e.DataIndex)
                _displayOrder[i] = new DisplayEntry(e.Kind, newIdx);
        }
        OnInventoryChanged?.Invoke();
        return true;
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

}
