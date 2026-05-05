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
        if (!availableSkills.Contains(skill))       return false;
        if (_ownedSkills.Contains(skill))           return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1)) return false;
        _ownedSkills.Add(skill);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool BuySupportOption(SupportOptionData option)
    {
        if (!availableSupports.Contains(option))    return false;
        if (_ownedSupports.Contains(option))        return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1)) return false;
        _ownedSupports.Add(option);
        OnInventoryChanged?.Invoke();
        return true;
    }
}
