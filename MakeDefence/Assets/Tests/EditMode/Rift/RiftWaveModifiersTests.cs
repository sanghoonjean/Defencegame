using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RiftWaveModifiersTests
{
    [Test]
    public void Default_HasNeutralMultipliers()
    {
        var d = RiftWaveModifiers.Default;
        Assert.AreEqual(1f, d.HpMult);
        Assert.AreEqual(1f, d.DefenseMult);
        Assert.AreEqual(1f, d.SpeedMult);
        Assert.AreEqual(1f, d.DamageMult);
        Assert.AreEqual(0,  d.ExtraCount);
        Assert.AreEqual(1f, d.RewardCubeMult);
    }

    [Test]
    public void FromOptions_NullOrEmpty_ReturnsDefault()
    {
        var fromNull  = RiftWaveModifiers.FromOptions(null);
        var fromEmpty = RiftWaveModifiers.FromOptions(new List<DimensionStoneOption>());

        Assert.AreEqual(1f, fromNull.HpMult);
        Assert.AreEqual(0,  fromNull.ExtraCount);
        Assert.AreEqual(1f, fromEmpty.HpMult);
        Assert.AreEqual(0,  fromEmpty.ExtraCount);
    }

    [Test]
    public void FromOptions_PercentBoosts_ApplyMultiplicatively()
    {
        var options = new List<DimensionStoneOption>
        {
            new(DimensionStoneOptionType.MonsterHpBoost,      30f),
            new(DimensionStoneOptionType.MonsterDefenseBoost, 20f),
            new(DimensionStoneOptionType.MonsterSpeedBoost,   10f),
            new(DimensionStoneOptionType.EnemyDamageBoost,    25f),
            new(DimensionStoneOptionType.RewardCubeBoost,     20f),
        };
        var m = RiftWaveModifiers.FromOptions(options);

        Assert.IsTrue(Mathf.Approximately(1.30f, m.HpMult));
        Assert.IsTrue(Mathf.Approximately(1.20f, m.DefenseMult));
        Assert.IsTrue(Mathf.Approximately(1.10f, m.SpeedMult));
        Assert.IsTrue(Mathf.Approximately(1.25f, m.DamageMult));
        Assert.IsTrue(Mathf.Approximately(1.20f, m.RewardCubeMult));
        Assert.AreEqual(0, m.ExtraCount);
    }

    [Test]
    public void FromOptions_CountBoost_AccumulatesAdditively()
    {
        var options = new List<DimensionStoneOption>
        {
            new(DimensionStoneOptionType.MonsterCountBoost, 5f),
        };
        var m = RiftWaveModifiers.FromOptions(options);

        Assert.AreEqual(5, m.ExtraCount);
        Assert.AreEqual(1f, m.HpMult);
    }

    [Test]
    public void FromOptions_CountBoost_NegativeIsClampedToZero()
    {
        var options = new List<DimensionStoneOption>
        {
            new(DimensionStoneOptionType.MonsterCountBoost, -5f),
        };
        var m = RiftWaveModifiers.FromOptions(options);
        Assert.AreEqual(0, m.ExtraCount);
    }
}
