using System;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public static event Action<Tower> OnTowerPlaced;

    // 기본 스탯 (tech-debt: 수치 미확정 — Inspector에서 조정)
    [SerializeField] private float baseAttackDamage   = 20f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseAttackRange    = 5f;

    public Vector2Int TileCoord { get; private set; }

    // 스킬 슬롯
    public SkillData EquippedSkill { get; private set; }

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
        RefreshStats();
    }

    public void Place(Vector2Int coord)
    {
        TileCoord = coord;
        ItemSystem.Instance?.RegisterTower(this);
        OnTowerPlaced?.Invoke(this);
    }

    public bool IsGhost { get; private set; }

    public void InitAsGhost()
    {
        IsGhost = true;
        enabled = false;
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        if (_animator != null) _animator.enabled = false;
    }

    private void OnDestroy()
    {
        if (IsGhost) return;
        ItemSystem.Instance?.UnregisterTower(this);
        MapTileSystem.Instance?.RemoveTower(TileCoord);
    }

    public void EquipSkill(SkillData skill)
    {
        EquippedSkill = skill;
        RefreshStats();
    }

    public void UnequipSkill()
    {
        EquippedSkill = null;
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

        AttackDamage   = baseAttackDamage   * (1f + dmgPct   / 100f);
        AttackCooldown = baseAttackSpeed * (1f - spdPct   / 100f);
        AttackRange    = baseAttackRange    * (1f + rangePct / 100f);

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

        _attackAnimSpeed = baseAttackSpeed / Mathf.Max(0.01f, AttackCooldown);
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
        bool attacking = target != null;
        _animator?.SetBool(AttackBool, attacking);
        if (_animator != null)
            _animator.speed = attacking ? _attackAnimSpeed : 1f;

        if (target == null)
        {
            _attackTimer = 0f;
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
