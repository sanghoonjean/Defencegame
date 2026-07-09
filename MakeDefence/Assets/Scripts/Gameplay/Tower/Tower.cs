using System;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public static event Action<Tower> OnTowerPlaced;

    /// 이 타워 인스턴스가 삭제될 때 발생. 스폰 버튼이 자신의 배치 상태를 초기화하는 데 사용.
    public event Action OnRemoved;

    [SerializeField] private JobClass jobClass = JobClass.None;
    public JobClass Job => jobClass;

    // 기본 스탯 (tech-debt: 수치 미확정 — Inspector에서 조정)
    [SerializeField] private float baseAttackDamage   = 20f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseAttackRange    = 5f;

    // 마나 (maxMana = 0 이면 마나 시스템 비활성화)
    [SerializeField] private float maxMana       = 100f;
    [SerializeField] private float manaRegenRate = 20f;  // 초당 회복량

    public float MaxMana        => maxMana;
    public float CurrentMana    { get; private set; }
    public bool  HasManaSystem  => maxMana > 0f;
    // 직업 보너스로 추가되는 마나 재생량 (초당). RefreshStats에서 계산, Update에서 합산.
    public float ManaRegenBonus { get; private set; }

    public event Action<float, float> OnManaChanged; // (current, max)

    // 최초 설치 시 무료로 지급되는 기본 스킬 (유닛 타입별로 프리팹에서 지정)
    [SerializeField] private SkillData defaultSkill;

    public Vector2Int TileCoord { get; private set; }

    // 스킬 슬롯
    public SkillData EquippedSkill { get; private set; }

    /// EquippedSkill이 최초 설치 시 무료로 지급된 기본 스킬인지 여부.
    /// 보유 목록 반환/판매 보상 지급 시 이 값을 확인해 무료 스킬 복제/큐브 무한 생성을 방지한다.
    public bool IsDefaultSkillEquipped { get; private set; }

    // 보조 옵션 슬롯 (최대 5개, 상위 큐브로 해금)
    private static readonly int[] SupportSlotCost = { 5, 10, 15, 20, 25 };
    private readonly SupportOptionData[] _supportSlots = new SupportOptionData[5];
    private int _unlockedSupportSlots = 0;
    public int UnlockedSupportSlots => _unlockedSupportSlots;
    public IReadOnlyList<SupportOptionData> SupportOptions => _supportSlots;

    // 최종 계산 스탯
    public float AttackDamage   { get; private set; }
    public float AttackCooldown { get; private set; }
    public float AttackRange    { get; private set; }
    public float StunChance     { get; private set; }
    public float CritChance     { get; private set; }
    public float CritDamage     { get; private set; }
    public float ArmorPen       { get; private set; }
    public float SkillCDReduce  { get; private set; }
    public float CubeDropRate   { get; private set; }
    public float IgniteChance      { get; private set; }
    public float AddedFireRatio   { get; private set; }
    public float DotDamageRatio   { get; private set; }
    public float DotDuration      { get; private set; }
    public int   ChainCount       { get; private set; }
    public int   PierceCount      { get; private set; }

    // Brutality Support — Physical 증폭 (More 곱연산) + 원소/카오스 차단 플래그
    public float BrutalityMultiplier { get; private set; } = 1f;
    public bool  IsBrutalityActive   { get; private set; }

    /// Physical 데미지에 Brutality More 배율을 적용. 비활성 시 원본 반환.
    public float ScalePhysical(float dmg)
        => IsBrutalityActive ? dmg * BrutalityMultiplier : dmg;

    private float    _attackTimer;
    private float    _attackAnimSpeed = 1f;
    private Animator _animator;
    private bool     _hasDirectionParams;

    private static readonly int AttackBool      = Animator.StringToHash("IsAttacking");
    private static readonly int DirectionXParam = Animator.StringToHash("DirectionX");
    private static readonly int DirectionYParam = Animator.StringToHash("DirectionY");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator != null)
            _hasDirectionParams = HasAnimatorParam("DirectionX") && HasAnimatorParam("DirectionY");
        CurrentMana = maxMana;
        RefreshStats();
    }

    public void Place(Vector2Int coord)
    {
        TileCoord = coord;
        ItemSystem.Instance?.RegisterTower(this);
        if (EquippedSkill == null && defaultSkill != null)
            EquipSkill(defaultSkill, isDefault: true);
        OnTowerPlaced?.Invoke(this);
    }

    /// 이미 배치된 타워를 다른 좌표로 옮길 때 사용. Place()와 달리 ItemSystem 재등록이나
    /// OnTowerPlaced 재발화 없이 좌표/월드 위치만 갱신한다.
    public void MoveTo(Vector2Int coord)
    {
        TileCoord = coord;
        transform.position = new Vector3(coord.x + 0.5f, coord.y + 0.5f, -1f);
    }

    public bool IsGhost { get; private set; }

    public void InitAsGhost()
    {
        IsGhost = true;
        SetGhostVisual(true);
    }

    /// 배치 대기 중(신규 ghost 또는 재배치 픽업) 시각/충돌 비활성화를 토글한다.
    public void SetGhostVisual(bool active)
    {
        enabled = !active;
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = !active;
    }

    private void OnDestroy()
    {
        if (IsGhost) return;
        ItemSystem.Instance?.UnregisterTower(this);
        MapTileSystem.Instance?.RemoveTower(TileCoord);
        OnRemoved?.Invoke();
    }

    public void EquipSkill(SkillData skill, bool isDefault = false)
    {
        EquippedSkill = skill;
        IsDefaultSkillEquipped = isDefault;
        RefreshStats();
    }

    public void UnequipSkill()
    {
        EquippedSkill = null;
        IsDefaultSkillEquipped = false;
        RefreshStats();
    }

    public int GetNextSupportSlotCost()
    {
        if (_unlockedSupportSlots >= 5) return -1;
        return SupportSlotCost[_unlockedSupportSlots];
    }

    public bool UnlockSupportSlot()
    {
        if (_unlockedSupportSlots >= 5) return false;
        int cost = SupportSlotCost[_unlockedSupportSlots];
        if (!CubeSystem.Instance.TryConsume(CubeType.Upper, cost)) return false;
        _unlockedSupportSlots++;
        return true;
    }

    public bool SetSupportOption(int slot, SupportOptionData option)
    {
        if (slot >= _unlockedSupportSlots) return false;
        _supportSlots[slot] = option;
        RefreshStats();
        return true;
    }

    public void RefreshStats()
    {
        float dmgPct   = 0f, spdPct = 0f, rangePct = 0f;
        StunChance    = 0f;
        CritChance    = 0f;
        CritDamage    = 0f;
        ArmorPen      = 0f;
        SkillCDReduce = 0f;
        CubeDropRate  = 0f;
        IgniteChance   = 0f;
        AddedFireRatio = 0f;
        DotDamageRatio = 0f;
        DotDuration    = 0f;
        ChainCount     = 0;
        PierceCount    = 0;
        BrutalityMultiplier = 1f;
        IsBrutalityActive   = false;
        ManaRegenBonus      = 0f;

        // 아이템 옵션 합산
        if (ItemSystem.Instance != null)
        {
            int slotCount = ItemSystem.Instance.GetUnlockedSlotCount(this);
            for (int i = 0; i < slotCount; i++)
            {
                var item = ItemSystem.Instance.GetItem(this, i);
                if (item == null) continue;
                foreach (var opt in item.Options)
                    AccumulateOption(opt, ref dmgPct, ref spdPct, ref rangePct);
            }
        }

        // 보조 옵션 합산
        for (int i = 0; i < _unlockedSupportSlots; i++)
        {
            var opt = _supportSlots[i];
            if (opt == null) continue;
            AccumulateSupportOption(opt);
        }

        // dmgPct/CritDamage/SkillCDReduce 등 직업 보너스 합산 (속도·사거리 %는 별도 변수로 분리)
        float jobSpdPct = 0f, jobRangePct = 0f;
        ApplyJobClassBonus(ref dmgPct, ref jobSpdPct, ref jobRangePct);

        AttackDamage   = baseAttackDamage * (1f + dmgPct / 100f);
        AttackCooldown = baseAttackSpeed  * (1f - spdPct / 100f);
        AttackRange    = baseAttackRange  * (1f + rangePct / 100f);

        AttackCooldown = Mathf.Max(0.1f, AttackCooldown);
        AttackRange    = Mathf.Max(0.5f, AttackRange);

        // Brutality Support — 원소/카오스 보조 옵션 효과 무효화
        // Physical More 증폭은 SkillDispatcher 에서 phys 합산값 (tower base + skill base) 에 적용
        if (IsBrutalityActive)
        {
            AddedFireRatio = 0f;
            IgniteChance   = 0f;
            DotDamageRatio = 0f;
            DotDuration    = 0f;
        }

        if (EquippedSkill != null)
        {
            float cdMult = 1f - Mathf.Clamp01(SkillCDReduce / 100f);
            AttackCooldown = EquippedSkill.baseCooldown * cdMult;
            AttackRange    = EquippedSkill.baseRange;
        }

        // 직업 속도·사거리 보너스는 스킬 오버라이드 이후에 적용 (스킬 기본값에 곱산)
        AttackCooldown *= (1f - jobSpdPct   / 100f);
        AttackRange    *= (1f + jobRangePct / 100f);

        AttackCooldown = Mathf.Max(0.1f, AttackCooldown);
        AttackRange    = Mathf.Max(0.5f, AttackRange);

        _attackAnimSpeed = baseAttackSpeed / Mathf.Max(0.01f, AttackCooldown);
    }

    // spdPct·rangePct 는 호출 측에서 jobSpdPct/jobRangePct 로 분리해 스킬 오버라이드 후 적용
    private void ApplyJobClassBonus(ref float dmgPct, ref float spdPct, ref float rangePct)
    {
        switch (jobClass)
        {
            case JobClass.Warrior:
                dmgPct     += 20f;
                CritDamage += 30f;
                break;
            case JobClass.Mage:
                SkillCDReduce += 20f;
                ManaRegenBonus = manaRegenRate * 0.1f;
                break;
            case JobClass.Archer:
                spdPct   += 20f;
                rangePct += 20f;
                break;
        }
    }

    private void AccumulateSupportOption(SupportOptionData opt)
    {
        switch (opt.optionType)
        {
            case SupportOptionType.IncendiaryRound: AddedFireRatio += Mathf.Clamp01(opt.value); break;
            case SupportOptionType.EnergyDrain:
                DotDamageRatio += Mathf.Clamp01(opt.value);
                DotDuration     = 3f;
                break;
            case SupportOptionType.ChainCircuit:
                ChainCount += Mathf.Max(1, Mathf.RoundToInt(opt.value * 10));
                break;
            case SupportOptionType.PiercingRound:
                PierceCount += Mathf.Max(1, Mathf.RoundToInt(opt.value * 5));
                break;
            case SupportOptionType.BrutalitySupport:
                BrutalityMultiplier *= 1f + Mathf.Clamp01(opt.value);
                IsBrutalityActive   = true;
                break;
        }
    }

    private void AccumulateOption(ItemOption opt,
        ref float dmgPct, ref float spdPct, ref float rangePct)
    {
        switch (opt.Type)
        {
            case ItemOptionType.AttackPower:         dmgPct        += opt.Value; break;
            case ItemOptionType.AttackSpeed:         spdPct        += opt.Value; break;
            case ItemOptionType.AttackRange:         rangePct      += opt.Value; break;
            case ItemOptionType.StunChance:          StunChance    += opt.Value; break;
            case ItemOptionType.CritChance:          CritChance    += opt.Value; break;
            case ItemOptionType.CritDamage:          CritDamage    += opt.Value; break;
            case ItemOptionType.ArmorPenetration:    ArmorPen      += opt.Value; break;
            case ItemOptionType.SkillCooldownReduce: SkillCDReduce += opt.Value; break;
            case ItemOptionType.CubeDropRate:        CubeDropRate  += opt.Value; break;
            case ItemOptionType.IgniteChance:        IgniteChance  += opt.Value; break;
        }
    }

    private void Update()
    {
        if (HasManaSystem)
        {
            float prev = CurrentMana;
            CurrentMana = Mathf.Min(maxMana, CurrentMana + (manaRegenRate + ManaRegenBonus) * Time.deltaTime);
            if (!Mathf.Approximately(prev, CurrentMana))
                OnManaChanged?.Invoke(CurrentMana, maxMana);
        }

        if (EquippedSkill == null)
        {
            if (_animator != null)
            {
                _animator.SetBool(AttackBool, false);
                _animator.speed = 1f;
            }
            return;
        }

        _attackTimer += Time.deltaTime;

        var target = FindTarget();
        bool hasEnoughMana = !HasManaSystem || CurrentMana >= EquippedSkill.manaCost;
        bool attacking = target != null && hasEnoughMana;
        _animator?.SetBool(AttackBool, attacking);
        if (_animator != null)
            _animator.speed = attacking ? _attackAnimSpeed : 1f;

        if (target == null || !hasEnoughMana)
        {
            if (target != null) _attackTimer = AttackCooldown; // 마나 충전되면 바로 공격
            else _attackTimer = 0f;
            return;
        }

        if (_attackTimer < AttackCooldown) return;

        Attack(target);
        _attackTimer = 0f;
    }

    private Enemy FindTarget()
    {
        Enemy closest = null;
        float minDist = AttackRange;

        foreach (var e in Enemy.ActiveEnemies)
        {
            if (e == null) continue;
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist) { minDist = dist; closest = e; }
        }
        return closest;
    }

    private void Attack(Enemy target)
    {
        Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;

        if (_hasDirectionParams)
        {
            _animator.SetFloat(DirectionXParam, dir.x);
            _animator.SetFloat(DirectionYParam, dir.y);
        }

        if (HasManaSystem && EquippedSkill != null && EquippedSkill.manaCost > 0f)
        {
            CurrentMana -= EquippedSkill.manaCost;
            OnManaChanged?.Invoke(CurrentMana, maxMana);
        }

        // Brutality 가 비호환 스킬을 차단한 경우 큐브 드롭도 막음 (실제 공격이 일어나지 않음)
        if (SkillDispatcher.Execute(this, target))
            TryDropCube();
    }

    private bool HasAnimatorParam(string paramName)
    {
        foreach (var p in _animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    private void TryDropCube()
    {
        if (CubeDropRate <= 0f) return;
        if (UnityEngine.Random.value < Mathf.Clamp01(CubeDropRate / 100f))
            CubeSystem.Instance.Add(CubeType.Lower, 1);
    }
}
