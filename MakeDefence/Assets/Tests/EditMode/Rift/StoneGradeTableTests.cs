using NUnit.Framework;

public class StoneGradeTableTests
{
    [Test]
    public void Resolve_DefaultChances_CumulativeBoundaries()
    {
        var table = new StoneGradeTable(); // 0.60 / 0.25 / 0.12 / 0.03

        Assert.AreEqual(StoneGrade.Normal, table.Resolve(0f));
        Assert.AreEqual(StoneGrade.Normal, table.Resolve(0.599f));
        Assert.AreEqual(StoneGrade.Magic,  table.Resolve(0.60f));
        Assert.AreEqual(StoneGrade.Magic,  table.Resolve(0.849f));
        Assert.AreEqual(StoneGrade.Rare,   table.Resolve(0.85f));
        Assert.AreEqual(StoneGrade.Rare,   table.Resolve(0.969f));
        Assert.AreEqual(StoneGrade.Unique, table.Resolve(0.97f));
        Assert.AreEqual(StoneGrade.Unique, table.Resolve(1f));
    }

    [Test]
    public void Resolve_ChanceSumBelowOne_FallsBackToUnique()
    {
        var table = new StoneGradeTable
        {
            normalChance = 0.5f,
            magicChance  = 0.2f,
            rareChance   = 0.1f,
            uniqueChance = 0f,   // 합 0.8 — 남는 구간은 Unique
        };
        Assert.AreEqual(StoneGrade.Unique, table.Resolve(0.81f));
        Assert.AreEqual(StoneGrade.Unique, table.Resolve(0.99f));
    }

    [Test]
    public void Resolve_FieldOverrides_PropagateToResolve()
    {
        var table = new StoneGradeTable
        {
            normalChance = 0.1f,
            magicChance  = 0.1f,
            rareChance   = 0.1f,
            uniqueChance = 0.7f,
        };
        Assert.AreEqual(StoneGrade.Normal, table.Resolve(0.05f));
        Assert.AreEqual(StoneGrade.Magic,  table.Resolve(0.15f));
        Assert.AreEqual(StoneGrade.Rare,   table.Resolve(0.25f));
        Assert.AreEqual(StoneGrade.Unique, table.Resolve(0.35f));
    }
}
