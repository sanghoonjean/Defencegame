/// <summary>
/// 몬스터 레벨 산정 공식의 단일 소스 (#388).
/// 레벨 = 스테이지 + 등급 보너스. 레벨이 스탯 스케일링의 기준이 된다 (UI 미표시).
/// </summary>
public static class EnemyLevel
{
    public static int GradeBonus(EnemyGrade grade)
    {
        switch (grade)
        {
            case EnemyGrade.Magic:  return 1;
            case EnemyGrade.Rare:   return 2;
            case EnemyGrade.Unique: return 3;
            default:                return 0; // Normal, LastBoss
        }
    }

    public static int Calculate(int stage, EnemyGrade grade)
        => stage + GradeBonus(grade);
}
