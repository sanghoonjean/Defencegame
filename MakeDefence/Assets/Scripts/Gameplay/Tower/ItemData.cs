using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemOptionType
{
    AttackPower, AttackSpeed, AttackRange, StunChance,
    CritChance, CritDamage, ArmorPenetration,
    SkillCooldownReduce, CubeDropRate
}

public class ItemOption
{
    public ItemOptionType Type  { get; }
    public float           Value { get; }

    public ItemOption(ItemOptionType type, float value)
    {
        Type  = type;
        Value = value;
    }
}

public class ItemData
{
    public const int MaxOptions = 6;

    private readonly List<ItemOption> _options = new();
    public IReadOnlyList<ItemOption> Options => _options;

    // 옵션 수치 범위 (tech-debt: 수치 미확정)
    private static readonly Dictionary<ItemOptionType, (float min, float max)> Ranges = new()
    {
        { ItemOptionType.AttackPower,          (5f,  30f) },
        { ItemOptionType.AttackSpeed,          (5f,  25f) },
        { ItemOptionType.AttackRange,          (5f,  20f) },
        { ItemOptionType.StunChance,           (3f,  15f) },
        { ItemOptionType.CritChance,           (5f,  25f) },
        { ItemOptionType.CritDamage,           (10f, 50f) },
        { ItemOptionType.ArmorPenetration,     (5f,  30f) },
        { ItemOptionType.SkillCooldownReduce,  (5f,  20f) },
        { ItemOptionType.CubeDropRate,         (2f,  10f) },
    };

    public static ItemData CreateRandom()
    {
        var item = new ItemData();
        item.AddRandomOption();
        return item;
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

    public bool UpgradeRandomOption()
    {
        if (_options.Count <= 1) return false;
        int idx = Random.Range(0, _options.Count);
        var old = _options[idx];
        var (_, max) = Ranges[old.Type];
        float upgraded = Mathf.Min(old.Value * 1.5f, max);
        _options[idx] = new ItemOption(old.Type, upgraded);
        return true;
    }

    public ItemData Clone()
    {
        var copy = new ItemData();
        foreach (var opt in _options)
            copy._options.Add(new ItemOption(opt.Type, opt.Value));
        return copy;
    }

    private List<ItemOptionType> GetAvailableTypes()
    {
        var used = new HashSet<ItemOptionType>(_options.Select(o => o.Type));
        var result = new List<ItemOptionType>();
        foreach (ItemOptionType t in System.Enum.GetValues(typeof(ItemOptionType)))
            if (!used.Contains(t)) result.Add(t);
        return result;
    }

    private static ItemOption RollOption(ItemOptionType type)
    {
        var (min, max) = Ranges[type];
        return new ItemOption(type, Mathf.Round(Random.Range(min, max)));
    }
}
