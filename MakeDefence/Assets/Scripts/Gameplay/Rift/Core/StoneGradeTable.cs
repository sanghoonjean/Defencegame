/// <summary>
/// 차원석 등급 확률 테이블 (#394). 획득 시점에 roll 하여 등급을 결정한다.
/// 각 값은 가중치로 해석된다 — 합이 1 이 아니어도 비율대로 정규화되므로
/// Inspector 에서 어느 필드를 조정해도 그대로 드랍률에 반영된다.
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
    /// roll01 (0~1) 을 가중치 비율의 누적 구간에 매핑. 결정적 함수 — EditMode 테스트 대상.
    /// roll01 == 1 같은 경계값은 양수 가중치를 가진 마지막 등급으로 처리하고,
    /// 모든 가중치가 0 이하면 Normal 을 반환한다.
    /// </summary>
    public StoneGrade Resolve(float roll01)
    {
        // 인덱스는 StoneGrade enum 순서 (0=Normal … 3=Unique) 와 1:1
        float[] weights =
        {
            UnityEngine.Mathf.Max(0f, normalChance),
            UnityEngine.Mathf.Max(0f, magicChance),
            UnityEngine.Mathf.Max(0f, rareChance),
            UnityEngine.Mathf.Max(0f, uniqueChance),
        };

        float total = weights[0] + weights[1] + weights[2] + weights[3];
        if (total <= 0f) return StoneGrade.Normal;

        float scaled = roll01 * total;
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f) continue;
            cumulative += weights[i];
            if (scaled < cumulative) return (StoneGrade)i;
        }

        // scaled == total 경계 — 양수 가중치의 마지막 등급
        for (int i = weights.Length - 1; i >= 0; i--)
            if (weights[i] > 0f) return (StoneGrade)i;
        return StoneGrade.Normal;
    }

    public StoneGrade Roll() => Resolve(UnityEngine.Random.value);
}
