using NUnit.Framework;

// Enemy(MonoBehaviour)/EnemyData 는 Assembly-CSharp 소속이라 asmdef 테스트 어셈블리에서
// 참조 불가 — 순수 로직(MakeDefence.Enemy.Core)만 테스트하고, Enemy.Initialize 연동은
// 에디터 execute_code 로 검증한다 (PR #390 검증 로그 참조).
public class EnemyLevelTests
{
    [Test]
    public void GradeBonus_PerGrade()
    {
        Assert.AreEqual(0, EnemyLevel.GradeBonus(EnemyGrade.Normal));
        Assert.AreEqual(1, EnemyLevel.GradeBonus(EnemyGrade.Magic));
        Assert.AreEqual(2, EnemyLevel.GradeBonus(EnemyGrade.Rare));
        Assert.AreEqual(3, EnemyLevel.GradeBonus(EnemyGrade.Unique));
        Assert.AreEqual(0, EnemyLevel.GradeBonus(EnemyGrade.LastBoss));
    }

    [Test]
    public void Calculate_StagePlusGradeBonus()
    {
        Assert.AreEqual(1,  EnemyLevel.Calculate(1, EnemyGrade.Normal));
        Assert.AreEqual(2,  EnemyLevel.Calculate(1, EnemyGrade.Magic));
        Assert.AreEqual(7,  EnemyLevel.Calculate(5, EnemyGrade.Rare));
        Assert.AreEqual(13, EnemyLevel.Calculate(10, EnemyGrade.Unique));
        Assert.AreEqual(12, EnemyLevel.Calculate(12, EnemyGrade.LastBoss));
    }

    [Test]
    public void Multipliers_LevelFormula()
    {
        // HP/방어 1 + level * 0.05, 이속 1 + level * 0.02
        Assert.AreEqual(1.25f, EnemyLevel.HpMultiplier(5),      1e-5f);
        Assert.AreEqual(1.35f, EnemyLevel.HpMultiplier(7),      1e-5f);
        Assert.AreEqual(1.25f, EnemyLevel.DefenseMultiplier(5), 1e-5f);
        Assert.AreEqual(1.35f, EnemyLevel.DefenseMultiplier(7), 1e-5f);
        Assert.AreEqual(1.10f, EnemyLevel.SpeedMultiplier(5),   1e-5f);
        Assert.AreEqual(1.14f, EnemyLevel.SpeedMultiplier(7),   1e-5f);
    }

    [Test]
    public void Multipliers_NormalGrade_MatchesLegacyStageFormula()
    {
        // Normal 은 level == stage 이므로 기존 stage 공식과 동일해야 한다 (밸런스 불변)
        for (int stage = 1; stage <= 16; stage++)
        {
            int level = EnemyLevel.Calculate(stage, EnemyGrade.Normal);
            Assert.AreEqual(stage, level);
            Assert.AreEqual(1f + stage * 0.05f, EnemyLevel.HpMultiplier(level),    1e-5f);
            Assert.AreEqual(1f + stage * 0.02f, EnemyLevel.SpeedMultiplier(level), 1e-5f);
        }
    }
}
