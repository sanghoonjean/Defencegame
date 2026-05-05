using UnityEngine;

public enum EnemyGrade { Normal, Magic, Rare, Unique, LastBoss }

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
}
