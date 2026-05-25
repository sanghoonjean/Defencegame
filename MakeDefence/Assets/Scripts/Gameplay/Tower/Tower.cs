using System;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public static event Action<Tower> OnTowerPlaced;

    // 기본 스탯 (tech-debt: 수치 미확정 — Inspector에서 조정)
    [SerializeField] private float baseAttackDamage   = 20f;
    [SerializeField] private float baseAttackCooldown = 1f;
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
    public float AddedFireRatio { get; private set; }

    private float    _attackTimer;
    private Animator _animator;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        RefreshStats();
    }

    public void Place(Vector2Int coord)
    {
        TileCoord = coord;
        ItemSystem.Instance?.RegisterTower(this);
        OnTowerPlaced?.Invoke(this);
    }

    private void OnDestroy()
    {
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
        AddedFireRatio = 0f;

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
        AttackCooldown = baseAttackCooldown * (1f - spdPct   / 100f);
        AttackRange    = baseAttackRange    * (1f + rangePct / 100f);

        AttackCooldown = Mathf.Max(0.1f, AttackCooldown);
        AttackRange    = Mathf.Max(0.5f, AttackRange);

        if (EquippedSkill != null)
        {
            float cdMult = 1f - Mathf.Clamp01(SkillCDReduce / 100f);
            AttackCooldown = EquippedSkill.baseCooldown * cdMult;
            AttackRange    = EquippedSkill.baseRange;
        }

        if (_animator != null)
        {
            float baseCd = EquippedSkill != null ? EquippedSkill.baseCooldown : baseAttackCooldown;
            _animator.speed = baseCd / Mathf.Max(0.01f, AttackCooldown);
        }
    }

    private void AccumulateSupportOption(SupportOptionData opt)
    {
        switch (opt.optionType)
        {
            case SupportOptionType.IncendiaryRound: AddedFireRatio += opt.value; break;
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
        }
    }

    private void Update()
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer < AttackCooldown) return;

        var target = FindTarget();
        if (target == null)
        {
            Debug.Log($"[Tower] FindTarget null — AttackRange={AttackRange}, ActiveEnemies={Enemy.ActiveEnemies.Count}, Skill={EquippedSkill?.name ?? "없음"}");
            _attackTimer = 0f;
            return;
        }

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
        _animator?.SetTrigger(AttackTrigger);
        SkillDispatcher.Execute(this, target);
        TryDropCube();
    }

    private void TryDropCube()
    {
        if (CubeDropRate <= 0f) return;
        if (UnityEngine.Random.value < Mathf.Clamp01(CubeDropRate / 100f))
            CubeSystem.Instance.Add(CubeType.Lower, 1);
    }
}
