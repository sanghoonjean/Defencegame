using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DimensionStoneOption
{
    public DimensionStoneOptionType Type  { get; }
    public float                    Value { get; }

    public DimensionStoneOption(DimensionStoneOptionType type, float value)
    {
        Type  = type;
        Value = value;
    }
}

public class DimensionStone
{
    public const int MaxOptions = 6;

    private readonly List<DimensionStoneOption> _options = new();
    public IReadOnlyList<DimensionStoneOption> Options => _options;

    // 옵션 수치 범위 (tech-debt: 수치 미확정 — 잠정값)
    // HP/Defense/Speed/Reward/EnemyDamage 는 % 단위, Count 는 마리 수
    private static readonly Dictionary<DimensionStoneOptionType, (float min, float max)> Ranges = new()
    {
        { DimensionStoneOptionType.MonsterHpBoost,      (5f, 30f) },
        { DimensionStoneOptionType.MonsterDefenseBoost, (5f, 30f) },
        { DimensionStoneOptionType.MonsterSpeedBoost,   (5f, 30f) },
        { DimensionStoneOptionType.MonsterCountBoost,   (1f, 10f) },
        { DimensionStoneOptionType.RewardCubeBoost,     (5f, 30f) },
        { DimensionStoneOptionType.EnemyDamageBoost,    (5f, 25f) },
    };

    public static DimensionStone CreateRandom()
    {
        var stone = new DimensionStone();
        stone.AddRandomOption();
        return stone;
    }

    public void Reroll()
    {
        int count = _options.Count;
        _options.Clear();
        for (int i = 0; i < count; i++)
            AddRandomOption();
    }

    public bool AddRandomOption()
    {
        if (_options.Count >= MaxOptions) return false;
        var available = GetAvailableTypes();
        if (available.Count == 0) return false;
        var type = available[Random.Range(0, available.Count)];
        _options.Add(RollOption(type));
        return true;
    }

    public bool RemoveRandomOption()
    {
        if (_options.Count <= 1) return false;
        _options.RemoveAt(Random.Range(0, _options.Count));
        return true;
    }

    /// <summary>
    /// 옵션 중 아직 max 에 도달하지 않은 항목 인덱스만 모은다.
    /// CanUpgrade / UpgradeRandomOption 공통 헬퍼.
    /// </summary>
    private List<int> GetUpgradeableIndices()
    {
        var result = new List<int>();
        for (int i = 0; i < _options.Count; i++)
        {
            var (_, max) = Ranges[_options[i].Type];
            if (_options[i].Value < max) result.Add(i);
        }
        return result;
    }

    /// <summary>업그레이드 가능한 옵션이 하나라도 있으면 true.</summary>
    public bool CanUpgrade() => GetUpgradeableIndices().Count > 0;

    /// <summary>
    /// 업그레이드 가능한 옵션 중 하나를 골라 1.5배 (max clamp).
    /// 전부 max 도달이거나 옵션이 없으면 false — 큐브 환불 트리거.
    /// </summary>
    public bool UpgradeRandomOption()
    {
        var upgradeable = GetUpgradeableIndices();
        if (upgradeable.Count == 0) return false;
        int idx = upgradeable[Random.Range(0, upgradeable.Count)];
        var old = _options[idx];
        var (_, max) = Ranges[old.Type];
        float upgraded = Mathf.Min(old.Value * 1.5f, max);
        _options[idx] = new DimensionStoneOption(old.Type, upgraded);
        return true;
    }

    public DimensionStone Clone()
    {
        var copy = new DimensionStone();
        foreach (var opt in _options)
            copy._options.Add(new DimensionStoneOption(opt.Type, opt.Value));
        return copy;
    }

    private List<DimensionStoneOptionType> GetAvailableTypes()
    {
        var used = new HashSet<DimensionStoneOptionType>(_options.Select(o => o.Type));
        var result = new List<DimensionStoneOptionType>();
        foreach (DimensionStoneOptionType t in System.Enum.GetValues(typeof(DimensionStoneOptionType)))
            if (!used.Contains(t)) result.Add(t);
        return result;
    }

    private static DimensionStoneOption RollOption(DimensionStoneOptionType type)
    {
        var (min, max) = Ranges[type];
        return new DimensionStoneOption(type, Mathf.Round(Random.Range(min, max)));
    }
}
