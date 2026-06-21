/// <summary>
/// 차원석 드랍 확률·수량 테이블. 등급 인덱스는 EnemyGrade enum 의 정수값
/// (0=Normal, 1=Magic, 2=Rare, 3=Unique, 4=LastBoss) 과 1:1.
///
/// Lower 큐브와 동일한 확률 (DroppedCubeSystem 기준):
///   Normal 8% / Magic 20% / Rare 40% / Unique 100% / LastBoss 100%
/// </summary>
[System.Serializable]
public class StoneDropChanceTable
{
    public float normalChance   = 0.08f;
    public float magicChance    = 0.20f;
    public float rareChance     = 0.40f;
    public float uniqueChance   = 1.00f;
    public float lastBossChance = 1.00f;

    public int normalCount   = 1;
    public int magicCount    = 1;
    public int rareCount     = 1;
    public int uniqueCount   = 1;
    public int lastBossCount = 2;

    /// <summary>
    /// gradeIndex 는 EnemyGrade 의 (int) 값. Out-of-range 는 (0, 0) 반환.
    /// </summary>
    public (float chance, int count) Resolve(int gradeIndex) => gradeIndex switch
    {
        0 => (normalChance,   normalCount),
        1 => (magicChance,    magicCount),
        2 => (rareChance,     rareCount),
        3 => (uniqueChance,   uniqueCount),
        4 => (lastBossChance, lastBossCount),
        _ => (0f, 0),
    };
}
