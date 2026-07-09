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
    MoltenStrike   = 8,
}

public enum SkillDamageNature
{
    Physical,   // Brutality Support 호환
    Fire,
    Cold,
    Lightning,
    Chaos,      // Poison / DoT 계열
}

[CreateAssetMenu(fileName = "SkillData", menuName = "MakeDefence/Skill Data")]
public class SkillData : ScriptableObject, IInventoryItem
{
    [Header("Display")]
    public string displayName;
    public Sprite icon;

    string            IInventoryItem.DisplayName => displayName;
    Sprite            IInventoryItem.Icon        => icon;
    InventoryItemKind IInventoryItem.Kind        => InventoryItemKind.Skill;

    [Header("Stats")]
    public SkillType skillType;
    public float     baseDamage;
    public float     baseCooldown;
    public float     baseRange;
    public float     aoeRadius;        // AoE/스플래시 반경 (모든 스킬 공용)
    public float     baseStunChance;   // FreezingPulse 기본 스턴 확률 (0~100)
    public float     stunDuration;     // FreezingPulse 스턴 지속시간
    public float     dotDuration;      // Nanobot 전용

    [Header("Mana")]
    public float manaCost = 0f;

    [Header("Support Restrictions")]
    public bool isDoTOnly;             // true → Added Fire Damage 미적용 (CausticArrow)

    [Header("Job Class Restriction")]
    [Tooltip("None이면 모든 직업 장착 가능. 지정 시 해당 직업만 장착 가능.")]
    public JobClass requiredClass = JobClass.None;

    [Header("Damage Classification")]
    [Tooltip("스킬의 베이스 데미지 분류. Brutality Support 는 Physical 만 허용.")]
    public SkillDamageNature damageNature = SkillDamageNature.Physical;

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

    [Header("Molten Strike 전용")]
    public int   projectileCount        = 4;
    public float explosionRadius        = 9f;
    public float projectileRadius       = 2f;
    [Tooltip("물리 피해의 X 비율을 화염으로 전환 (0~1)")]
    public float physToFireRatio        = 0.6f;
    [Tooltip("투사체 명중/상태이상 피해 less 비율 (0~1) — 0.6 → ×0.4")]
    public float projectileLessHitRatio = 0.6f;
}
