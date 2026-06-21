using System.Collections.Generic;
using UnityEngine;

public readonly struct RiftWaveModifiers
{
    public float HpMult         { get; }
    public float DefenseMult    { get; }
    public float SpeedMult      { get; }
    public float DamageMult     { get; }
    public int   ExtraCount     { get; }
    public float RewardCubeMult { get; }

    public RiftWaveModifiers(
        float hpMult, float defenseMult, float speedMult, float damageMult,
        int extraCount, float rewardCubeMult)
    {
        HpMult         = hpMult;
        DefenseMult    = defenseMult;
        SpeedMult      = speedMult;
        DamageMult     = damageMult;
        ExtraCount     = extraCount;
        RewardCubeMult = rewardCubeMult;
    }

    public static RiftWaveModifiers Default => new(1f, 1f, 1f, 1f, 0, 1f);

    /// <summary>
    /// 차원석 옵션 리스트를 웨이브 보정값으로 변환.
    /// % 옵션은 (1 + value/100) 배율로 곱 적용, MonsterCountBoost 는 가산 마리 수.
    /// 빈 리스트는 Default 와 동일.
    /// </summary>
    public static RiftWaveModifiers FromOptions(IReadOnlyList<DimensionStoneOption> options)
    {
        if (options == null || options.Count == 0) return Default;

        float hp = 1f, def = 1f, spd = 1f, dmg = 1f, reward = 1f;
        int extra = 0;

        foreach (var opt in options)
        {
            switch (opt.Type)
            {
                case DimensionStoneOptionType.MonsterHpBoost:      hp     *= 1f + opt.Value / 100f; break;
                case DimensionStoneOptionType.MonsterDefenseBoost: def    *= 1f + opt.Value / 100f; break;
                case DimensionStoneOptionType.MonsterSpeedBoost:   spd    *= 1f + opt.Value / 100f; break;
                case DimensionStoneOptionType.EnemyDamageBoost:    dmg    *= 1f + opt.Value / 100f; break;
                case DimensionStoneOptionType.RewardCubeBoost:     reward *= 1f + opt.Value / 100f; break;
                case DimensionStoneOptionType.MonsterCountBoost:   extra  += Mathf.Max(0, Mathf.RoundToInt(opt.Value)); break;
            }
        }
        return new RiftWaveModifiers(hp, def, spd, dmg, extra, reward);
    }
}
