using UnityEngine;

public enum SkillType
{
    Fireball       = 1,
    ParalysisMagic = 2,
    LightningSpear = 3,
    PoisonCloud    = 4,
    FreezingPulse  = 5,
    LightningArrow = 6,
    CausticArrow   = 7,
}

[CreateAssetMenu(fileName = "SkillData", menuName = "MakeDefence/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    public Sprite icon;

    [Header("Stats")]
    public SkillType skillType;
    public float     baseDamage;
    public float     baseCooldown;
    public float     baseRange;
    public float     aoeRadius;        // AoE/스플래시 반경 (모든 스킬 공용)
    public float     baseStunChance;   // FreezingPulse 기본 스턴 확률 (0~100)
    public float     stunDuration;     // FreezingPulse 스턴 지속시간
    public float     dotDuration;      // Nanobot 전용

    [Header("Support Restrictions")]
    public bool isDoTOnly;             // true → Added Fire Damage 미적용 (CausticArrow)

    // FreezingPulse·LightningArrow 전용 — Fireball·CausticArrow는 항상 원형 스플래시 사용
    [Header("AoE Shape (FreezingPulse / LightningArrow 전용)")]
    public AoeShape aoeShape = AoeShape.Circle;
    [Tooltip("원뿔: 반각도(도), 기본 45°")]
    public float    aoeAngle = 45f;
    [Tooltip("직사각형: 폭(월드 단위)")]
    public float    aoeWidth = 2f;

    [Header("AoE FX")]
    [Tooltip("AoE 발동 위치에 인스턴스화할 FX 프리팹. null 이면 GameUIManager 의 내부 도형 렌더링 사용")]
    public GameObject aoeFxPrefab;
}
