using UnityEngine;

// EnemyGrade 는 Core/EnemyGrade.cs (MakeDefence.Enemy.Core) 로 이동 (#388 리뷰 반영)

[CreateAssetMenu(fileName = "EnemyData", menuName = "MakeDefence/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public EnemyGrade grade;
    public float baseHp;
    public float baseDefense;
    public float baseSpeed;
    public int playerDamage;

    // LastBoss는 난이도 공식 미적용
    public bool fixedStats;

    [Header("저항 (0 = 없음, 0.5 = 50% 감소, -0.25 = 25% 증가)")]
    [Range(-1f, 0.9f)] public float fireResistance;
    [Range(-1f, 0.9f)] public float coldResistance;
    [Range(-1f, 0.9f)] public float lightningResistance;
    [Range(-1f, 0.9f)] public float poisonResistance;
}
