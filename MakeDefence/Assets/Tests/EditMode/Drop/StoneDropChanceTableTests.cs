using NUnit.Framework;

public class StoneDropChanceTableTests
{
    [Test]
    public void Resolve_DefaultChances_MatchLowerCubeSpec()
    {
        var t = new StoneDropChanceTable();
        // Lower 큐브와 동일 — Normal 8% / Magic 20% / Rare 40% / Unique 100% / LastBoss 100%
        Assert.AreEqual(0.08f, t.Resolve(0).chance);
        Assert.AreEqual(0.20f, t.Resolve(1).chance);
        Assert.AreEqual(0.40f, t.Resolve(2).chance);
        Assert.AreEqual(1.00f, t.Resolve(3).chance);
        Assert.AreEqual(1.00f, t.Resolve(4).chance);
    }

    [Test]
    public void Resolve_DefaultCounts_NormalGradesOne_LastBossTwo()
    {
        var t = new StoneDropChanceTable();
        Assert.AreEqual(1, t.Resolve(0).count);
        Assert.AreEqual(1, t.Resolve(1).count);
        Assert.AreEqual(1, t.Resolve(2).count);
        Assert.AreEqual(1, t.Resolve(3).count);
        Assert.AreEqual(2, t.Resolve(4).count);
    }

    [Test]
    public void Resolve_OutOfRange_ReturnsZero()
    {
        var t = new StoneDropChanceTable();
        Assert.AreEqual((0f, 0), t.Resolve(-1));
        Assert.AreEqual((0f, 0), t.Resolve(5));
        Assert.AreEqual((0f, 0), t.Resolve(100));
    }

    [Test]
    public void Resolve_FieldOverrides_PropagateToResolve()
    {
        var t = new StoneDropChanceTable
        {
            normalChance   = 0.5f,
            uniqueCount    = 3,
            lastBossChance = 0.75f,
        };
        Assert.AreEqual(0.5f,  t.Resolve(0).chance);
        Assert.AreEqual(3,     t.Resolve(3).count);
        Assert.AreEqual(0.75f, t.Resolve(4).chance);
    }
}
