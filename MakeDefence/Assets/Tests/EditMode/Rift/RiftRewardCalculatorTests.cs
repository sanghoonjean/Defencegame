using NUnit.Framework;

public class RiftRewardCalculatorTests
{
    [Test]
    public void CalculateCubeReward_BaseTimesMult_RoundedToInt()
    {
        Assert.AreEqual(12, RiftRewardCalculator.CalculateCubeReward(10, 1.2f));
        Assert.AreEqual(13, RiftRewardCalculator.CalculateCubeReward(10, 1.26f));
        Assert.AreEqual(15, RiftRewardCalculator.CalculateCubeReward(10, 1.5f));
    }

    [Test]
    public void CalculateCubeReward_ZeroOrNegativeBase_ReturnsZero()
    {
        Assert.AreEqual(0, RiftRewardCalculator.CalculateCubeReward(0,  1.5f));
        Assert.AreEqual(0, RiftRewardCalculator.CalculateCubeReward(-5, 1.5f));
    }

    [Test]
    public void CalculateCubeReward_NonPositiveMult_ReturnsZero()
    {
        Assert.AreEqual(0, RiftRewardCalculator.CalculateCubeReward(10, 0f));
        Assert.AreEqual(0, RiftRewardCalculator.CalculateCubeReward(10, -1f));
    }

    [Test]
    public void CalculateCubeReward_DefaultMult_ReturnsBase()
    {
        Assert.AreEqual(10, RiftRewardCalculator.CalculateCubeReward(10, RiftWaveModifiers.Default.RewardCubeMult));
    }
}
