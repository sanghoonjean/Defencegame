using UnityEngine;

public enum SkillType { SingleLaser, Missile, EMP, Railgun, Nanobot }

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
