using UnityEngine;

public enum SkillType
{
    PreciseArrow   = 0,
    Fireball       = 1,
    ParalysisMagic = 2,
    LightningSpear = 3,
    PoisonCloud    = 4,
    FreezingPulse  = 5,
}

[CreateAssetMenu(fileName = "SkillData", menuName = "MakeDefence/Skill Data")]
public class SkillData : ScriptableObject
{
    public SkillType skillType;
    public float     baseDamage;
    public float     baseCooldown;
    public float     baseRange;
    public float     aoeRadius;        // Fireball 전용
    public float     baseStunChance;   // FreezingPulse 기본 스턴 확률 (0~100)
    public float     stunDuration;     // FreezingPulse 스턴 지속시간
    public float     dotDuration;      // Nanobot 전용
}
