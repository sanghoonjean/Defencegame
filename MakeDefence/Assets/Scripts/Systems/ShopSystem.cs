using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }

    public static event Action OnInventoryChanged;

    // 보유 스킬/보조옵션 목록 (Inspector에서 초기 풀 설정)
    [SerializeField] private List<SkillData>         availableSkills;
    [SerializeField] private List<SupportOptionData> availableSupports;

    private readonly List<SkillData>         _ownedSkills   = new();
    private readonly List<SupportOptionData> _ownedSupports = new();

    public IReadOnlyList<SkillData>         OwnedSkills   => _ownedSkills;
    public IReadOnlyList<SupportOptionData> OwnedSupports => _ownedSupports;

    private void Awake() { Instance = this; }

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
        _ownedSkills.Add(skill);
        Debug.Log($"[ShopSystem] BuySkill 성공 — {skill.skillType} 구매, 보유 스킬 총 {_ownedSkills.Count}개");
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool IsAvailableSupport(SupportOptionData option) => availableSupports.Contains(option);

    public bool BuySupportOption(SupportOptionData option)
    {
        if (!availableSupports.Contains(option))    return false;
        if (_ownedSupports.Contains(option))        return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1)) return false;
        _ownedSupports.Add(option);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ReturnSkill(SkillData skill)
    {
        if (skill == null) return;
        _ownedSkills.Add(skill);
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveOwnedSkill(SkillData skill)
    {
        if (!_ownedSkills.Remove(skill)) return false;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool SwapOwnedSkills(int indexA, int indexB)
    {
        if (indexA < 0 || indexB < 0) return false;
        if (indexA >= _ownedSkills.Count || indexB >= _ownedSkills.Count) return false;
        if (indexA == indexB) return false;
        (_ownedSkills[indexA], _ownedSkills[indexB]) = (_ownedSkills[indexB], _ownedSkills[indexA]);
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
        // toIndex가 count를 초과하면 맨 뒤에 삽입
        int insertAt = Mathf.Min(toIndex > fromIndex ? toIndex - 1 : toIndex,
                                 _ownedSkills.Count);
        _ownedSkills.Insert(insertAt, skill);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ReturnSupportOption(SupportOptionData option)
    {
        if (option == null) return;
        _ownedSupports.Add(option);
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveOwnedSupportOption(SupportOptionData option)
    {
        if (!_ownedSupports.Remove(option)) return false;
        OnInventoryChanged?.Invoke();
        return true;
    }
}
