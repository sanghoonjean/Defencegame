using UnityEngine;

public enum SkillType
{
    PreciseArrow   = 0,
    Fireball       = 1,
    ParalysisMagic = 2,
    LightningSpear = 3,
    PoisonCloud    = 4,
}

[CreateAssetMenu(fileName = "SkillData", menuName = "MakeDefence/Skill Data")]
public class SkillData : ScriptableObject
{
    public SkillType skillType;
    public float     baseDamage;
    public float     baseCooldown;
    public float     baseRange;
    public float     aoeRadius;     // Missile / Nanobot 전용
    public float     stunDuration;  // EMP 전용
    public float     dotDuration;   // Nanobot 전용
}
