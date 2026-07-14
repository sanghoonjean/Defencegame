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
    public void Resolve_WeightsAreNormalized_SumNotRequiredToBeOne()
    {
        // 합 0.8 — 비율(5:2:1)로 정규화되므로 Unique(가중치 0) 는 절대 나오지 않음
        var table = new StoneGradeTable
        {
            normalChance = 0.5f,
            magicChance  = 0.2f,
            rareChance   = 0.1f,
            uniqueChance = 0f,
        };
        Assert.AreEqual(StoneGrade.Normal, table.Resolve(0.624f)); // < 0.5/0.8
        Assert.AreEqual(StoneGrade.Magic,  table.Resolve(0.626f));
        Assert.AreEqual(StoneGrade.Rare,   table.Resolve(0.99f));
        Assert.AreEqual(StoneGrade.Rare,   table.Resolve(1f), "roll 1.0 경계 — 양수 가중치의 마지막 등급");

        // 모든 가중치를 2배 해도 분포 동일
        var doubled = new StoneGradeTable
        {
            normalChance = 1.2f,
            magicChance  = 0.5f,
            rareChance   = 0.24f,
            uniqueChance = 0.06f,
        };
        Assert.AreEqual(StoneGrade.Normal, doubled.Resolve(0.599f));
        Assert.AreEqual(StoneGrade.Magic,  doubled.Resolve(0.60f));
        Assert.AreEqual(StoneGrade.Unique, doubled.Resolve(0.97f));
    }

    [Test]
    public void Resolve_UniqueChanceIsHonored()
    {
        // Codex 리뷰 (P2) — uniqueChance 필드가 실제 드랍률을 제어해야 한다
        var table = new StoneGradeTable
        {
            normalChance = 0.1f,
            magicChance  = 0.1f,
            rareChance   = 0.1f,
            uniqueChance = 0.01f,  // 합 0.31 — Unique 는 약 3.2% 구간만 차지
        };
        Assert.AreEqual(StoneGrade.Rare,   table.Resolve(0.95f));   // < 0.30/0.31
        Assert.AreEqual(StoneGrade.Unique, table.Resolve(0.97f));   // >= 0.30/0.31
    }

    [Test]
    public void Resolve_AllWeightsZero_ReturnsNormal()
    {
        var table = new StoneGradeTable
        {
            normalChance = 0f,
            magicChance  = 0f,
            rareChance   = 0f,
            uniqueChance = 0f,
        };
        Assert.AreEqual(StoneGrade.Normal, table.Resolve(0.5f));
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
