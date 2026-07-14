/// <summary>
/// 차원석 등급 확률 테이블 (#394). 획득 시점에 roll 하여 등급을 결정한다.
/// 확률 합이 1 미만이면 남는 구간은 Unique 로 fallback 된다.
/// </summary>
[System.Serializable]
public class StoneGradeTable
{
    // tech-debt: 확률 미확정 — Inspector 튜닝 가능 (잠정값)
    public float normalChance = 0.60f;
    public float magicChance  = 0.25f;
    public float rareChance   = 0.12f;
    public float uniqueChance = 0.03f;

    /// <summary>
    /// roll01 (0~1) 을 누적 구간에 매핑. 결정적 함수 — EditMode 테스트 대상.
    /// </summary>
    public StoneGrade Resolve(float roll01)
    {
        float cumulative = normalChance;
        if (roll01 < cumulative) return StoneGrade.Normal;
        cumulative += magicChance;
        if (roll01 < cumulative) return StoneGrade.Magic;
        cumulative += rareChance;
        if (roll01 < cumulative) return StoneGrade.Rare;
        return StoneGrade.Unique;
    }

    public StoneGrade Roll() => Resolve(UnityEngine.Random.value);
}
