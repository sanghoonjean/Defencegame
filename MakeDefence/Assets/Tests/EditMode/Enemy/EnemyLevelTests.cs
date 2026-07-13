using NUnit.Framework;
using UnityEngine;

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

    // Enemy.Initialize 의 레벨 기반 스탯 배율: HP/방어 1 + level * 0.05, 이속 1 + level * 0.02
    private static Enemy CreateEnemy(out GameObject go)
    {
        go = new GameObject("EnemyLevelTests");
        return go.AddComponent<Enemy>();
    }

    private static EnemyData CreateData(EnemyGrade grade, bool fixedStats)
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        data.grade = grade;
        data.baseHp = 100f;
        data.baseDefense = 10f;
        data.baseSpeed = 1f;
        data.playerDamage = 1;
        data.fixedStats = fixedStats;
        return data;
    }

    [Test]
    public void Initialize_RareGradeBonus_AppliesToStats()
    {
        var data = CreateData(EnemyGrade.Rare, fixedStats: false);
        var enemy = CreateEnemy(out var go);
        try
        {
            enemy.Initialize(data, 5, new Vector2[0], 0);
            Assert.AreEqual(7, enemy.Level);                 // 5 + Rare(2)
            Assert.AreEqual(135f, enemy.MaxHp);              // Floor(100 * 1.35)
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void Initialize_Normal_MatchesLegacyStageFormula()
    {
        var data = CreateData(EnemyGrade.Normal, fixedStats: false);
        var enemy = CreateEnemy(out var go);
        try
        {
            enemy.Initialize(data, 5, new Vector2[0], 0);
            Assert.AreEqual(5, enemy.Level);                 // level == stage → 밸런스 불변
            Assert.AreEqual(125f, enemy.MaxHp);              // Floor(100 * 1.25) — 기존 stage 공식과 동일
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void Initialize_FixedStats_LevelIsReferenceOnly()
    {
        var data = CreateData(EnemyGrade.LastBoss, fixedStats: true);
        var enemy = CreateEnemy(out var go);
        try
        {
            enemy.Initialize(data, 12, new Vector2[0], 0);
            Assert.AreEqual(12, enemy.Level);                // 참조용 — 공식 미적용
            Assert.AreEqual(100f, enemy.MaxHp);              // baseHp 그대로
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(data);
        }
    }
}
