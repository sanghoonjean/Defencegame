using UnityEngine;

public static class SkillDispatcher
{
    // 반환값: 실제로 공격이 수행됐는지. false 면 호출자가 사후 처리 (큐브 드롭 등) 도 skip.
    public static bool Execute(Tower tower, Enemy target)
    {
        var skill = tower.EquippedSkill;
        if (skill == null)
        {
            DirectAttack(tower, target);
            return true;
        }

        // Brutality Support — Physical 외 모든 데미지 타입 스킬 발사 차단
        if (tower.IsBrutalityActive && skill.damageNature != SkillDamageNature.Physical)
            return false;

        switch (skill.skillType)
        {
            case SkillType.Fireball:
                LaunchFireball(tower, target);
                break;
            case SkillType.FreezingPulse:
                ExecuteFreezingPulse(tower, target);
                break;
            case SkillType.LightningArrow:
                LaunchLightningArrow(tower, target);
                break;
            case SkillType.CausticArrow:
                LaunchCausticArrow(tower, target);
                break;
            case SkillType.MoltenStrike:
                ExecuteMoltenStrike(tower, target);
                break;
            default:
                DirectAttack(tower, target, applyFire: !skill.isDoTOnly);
                break;
        }
        return true;
    }

    private static void DirectAttack(Tower tower, Enemy target, bool applyFire = true)
    {
        float dmg    = tower.ScalePhysical(tower.AttackDamage);
        bool  isCrit = Random.value < Mathf.Clamp01(tower.CritChance / 100f);
        if (isCrit) dmg *= 1f + tower.CritDamage / 100f;

        target.TakeDamage(dmg, tower.ArmorPen / 100f, isCrit);
        if (applyFire) ApplyFireDamage(tower, target, tower.AttackDamage, isCrit);

        if (target.CurrentHp > 0f && tower.StunChance > 0f &&
            Random.value < Mathf.Clamp01(tower.StunChance / 100f))
            target.ApplyStun(0.5f);
    }

    private static void ExecuteFreezingPulse(Tower tower, Enemy target)
    {
        var   skill      = tower.EquippedSkill;
        float baseDmg    = tower.AttackDamage + skill.baseDamage;
        bool  isCrit     = Random.value < Mathf.Clamp01(tower.CritChance / 100f);
        float dmg        = isCrit ? baseDmg * (1f + tower.CritDamage / 100f) : baseDmg;
        float freeze     = skill.stunDuration > 0f ? skill.stunDuration : 0.5f;
        float stunChance = skill.baseStunChance + tower.StunChance;
        float   radius   = skill.aoeRadius > 0f ? Mathf.Min(skill.aoeRadius, tower.AttackRange) : tower.AttackRange;
        Vector2 origin   = tower.transform.position;
        Vector2 forward  = ((Vector2)target.transform.position - origin).normalized;
        float   dotTick  = tower.AttackDamage * tower.DotDamageRatio;

        foreach (var e in Enemy.ActiveEnemies.ToArray())
        {
            if (e == null) continue;
            if (!AoeUtils.IsInAoe(e.transform.position, origin, forward,
                    skill.aoeShape, radius, skill.aoeWidth, skill.aoeAngle)) continue;

            e.TakeDamage(dmg, tower.ArmorPen / 100f, isCrit, DamageType.Cold);
            if (e.CurrentHp > 0f && stunChance > 0f &&
                Random.value < Mathf.Clamp01(stunChance / 100f))
                e.ApplyStun(freeze);
            ApplyFireDamage(tower, e, baseDmg, isCrit);
            if (dotTick > 0f && tower.DotDuration > 0f && e.CurrentHp > 0f)
                e.ApplyDot(dotTick, tower.DotDuration);
        }

        AoeUtils.ShowAoeHit(origin, forward, skill.aoeShape, radius, skill.aoeWidth, skill.aoeAngle,
            skill.aoeFxPrefab);
    }

    private static void LaunchLightningArrow(Tower tower, Enemy target)
    {
        var proj = ObjectPoolSystem.Instance.GetProjectile<LightningArrowProjectile>();
        if (proj == null) { DirectAttack(tower, target); return; }

        var   skill = tower.EquippedSkill;
        float dmg   = tower.AttackDamage + skill.baseDamage;

        proj.AoeRadius      = skill.aoeRadius;
        proj.ShockDuration  = skill.stunDuration > 0f ? skill.stunDuration : 0.5f;
        proj.CritChance     = tower.CritChance;
        proj.CritDamage     = tower.CritDamage;
        proj.StunChance     = 0f;
        proj.AddedFireRatio = tower.AddedFireRatio;
        proj.FireCritDamage = tower.CritDamage;
        proj.FireBaseDamage = dmg;
        proj.DotTickDamage  = tower.AttackDamage * tower.DotDamageRatio;
        proj.DotDuration    = tower.DotDuration;
        proj.IgniteChance   = tower.IgniteChance;
        proj.ChainCount     = tower.ChainCount;
        proj.PierceCount    = tower.PierceCount;
        proj.AoeShape       = skill.aoeShape;
        proj.AoeAngle       = skill.aoeAngle;
        proj.AoeWidth       = skill.aoeWidth;
        proj.Launch(tower.transform.position, target, dmg, tower.ArmorPen / 100f);
    }

    private static void LaunchCausticArrow(Tower tower, Enemy target)
    {
        var proj = ObjectPoolSystem.Instance.GetProjectile<CausticArrowProjectile>();
        if (proj == null) { DirectAttack(tower, target, applyFire: false); return; }

        var skill = tower.EquippedSkill;

        proj.AoeRadius      = skill.aoeRadius;
        proj.PoisonDuration = skill.dotDuration > 0f ? skill.dotDuration : 3f;
        proj.TickDamage     = skill.baseDamage;
        proj.TickInterval   = 0.5f;
        proj.StunChance     = 0f;
        proj.SplashRadius   = skill.aoeRadius;
        proj.DotTickDamage  = tower.AttackDamage * tower.DotDamageRatio;
        proj.DotDuration    = tower.DotDuration;
        proj.IgniteChance   = tower.IgniteChance;
        proj.ChainCount     = tower.ChainCount;
        proj.PierceCount    = tower.PierceCount;
        // AddedFireRatio 미설정 — DoT 스킬은 불꽃 데미지 제외
        proj.Launch(tower.transform.position, target, skill.baseDamage, tower.ArmorPen / 100f);
    }

    private static void LaunchFireball(Tower tower, Enemy target)
    {
        var proj = ObjectPoolSystem.Instance.GetProjectile<FireballProjectile>();
        if (proj == null) { DirectAttack(tower, target); return; }

        var   skill   = tower.EquippedSkill;
        float baseDmg = tower.AttackDamage + skill.baseDamage;
        bool  isCrit  = Random.value < Mathf.Clamp01(tower.CritChance / 100f);
        float dmg     = isCrit ? baseDmg * (1f + tower.CritDamage / 100f) : baseDmg;

        proj.AoeRadius          = skill.aoeRadius;
        proj.StunChance         = tower.StunChance;
        proj.SplashRadius       = skill.aoeRadius;
        proj.SplashStunDuration = skill.stunDuration > 0f ? skill.stunDuration : 0.5f;
        proj.IsCrit             = isCrit;
        proj.SplashDamageType   = DamageType.Fire;
        proj.AddedFireRatio     = tower.AddedFireRatio;
        proj.FireCritDamage     = tower.CritDamage;
        proj.FireBaseDamage     = baseDmg;
        proj.DotTickDamage      = tower.AttackDamage * tower.DotDamageRatio;
        proj.DotDuration        = tower.DotDuration;
        proj.IgniteChance       = tower.IgniteChance;
        proj.ChainCount         = tower.ChainCount;
        proj.PierceCount        = tower.PierceCount;
        proj.Launch(tower.transform.position, target, dmg, tower.ArmorPen / 100f);
    }

    private static void ExecuteMoltenStrike(Tower tower, Enemy target)
    {
        var   skill    = tower.EquippedSkill;
        // Brutality 활성 시 phys 합산값 전체에 More 배율 적용 (tower base + skill base 모두 증폭)
        float baseDmg  = tower.ScalePhysical(tower.AttackDamage + skill.baseDamage);
        bool  isCrit   = Random.value < Mathf.Clamp01(tower.CritChance / 100f);
        float dmg      = isCrit ? baseDmg * (1f + tower.CritDamage / 100f) : baseDmg;
        // Brutality 활성 시 Fire 변환 차단 → 전량 Physical
        float fireFrac = tower.IsBrutalityActive ? 0f : Mathf.Clamp01(skill.physToFireRatio);
        float phys     = dmg * (1f - fireFrac);
        float fire     = dmg * fireFrac;

        // 1차 근접 타격 — PHY + 전환 FIRE + AddedFire
        if (phys > 0f)
            target.TakeDamage(phys, tower.ArmorPen / 100f, isCrit, DamageType.Physical);
        if (fire > 0f && target.CurrentHp > 0f)
        {
            target.TakeDamage(fire, 0f, isCrit, DamageType.Fire);
            TryIgniteStatic(tower, target, fire);
        }
        if (target.CurrentHp > 0f)
            ApplyFireDamage(tower, target, baseDmg, isCrit);
        if (target.CurrentHp > 0f && tower.StunChance > 0f &&
            Random.value < Mathf.Clamp01(tower.StunChance / 100f))
            target.ApplyStun(0.5f);

        // 2차 마그마 투사체 — 부채꼴 spread + 거리 랜덤
        int count = Mathf.Max(1, skill.projectileCount);
        if (count <= 0) return;

        Vector2 hitPos  = target.transform.position;
        Vector2 forward = ((Vector2)target.transform.position - (Vector2)tower.transform.position).normalized;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector2.right;

        const float SpreadDeg = 60f;          // 부채꼴 총 폭 (±30°)
        const float MinDist   = 1.5f;
        const float MaxDist   = 3.5f;
        float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            var proj = ObjectPoolSystem.Instance.GetProjectile<MagmaProjectile>();
            if (proj == null) return;   // 프리팹 미등록 — 1차 타격만 발생하는 안전 폴백

            float angleOffset = (count == 1) ? 0f
                : SpreadDeg * ((float)i / (count - 1) - 0.5f);
            float angleRad    = (baseAngle + angleOffset) * Mathf.Deg2Rad;
            Vector2 dir       = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            float   dist      = Random.Range(MinDist, MaxDist);
            Vector2 landPos   = hitPos + dir * dist;

            // projectileLessHitRatio 는 hit + ailment 모두 less — DoT/ignite 도 동일 감폭
            float lessRetain = 1f - Mathf.Clamp01(skill.projectileLessHitRatio);

            proj.ExplosionRadius     = skill.explosionRadius;
            proj.ProjectileRadius    = skill.projectileRadius;
            proj.BasePhysDamage      = phys;
            proj.BaseFireDamage      = fire;
            proj.ProjectileLessRatio = skill.projectileLessHitRatio;
            proj.IgniteChance        = tower.IgniteChance;
            proj.DotTickDamage       = tower.AttackDamage * tower.DotDamageRatio * lessRetain;
            proj.DotDuration         = tower.DotDuration;
            proj.LaunchArc(hitPos, landPos, tower.ArmorPen / 100f);
        }
    }

    private static void TryIgniteStatic(Tower tower, Enemy target, float fireDmg)
    {
        if (tower.IgniteChance <= 0f) return;
        if (target.CurrentHp <= 0f) return;
        if (Random.value >= Mathf.Clamp01(tower.IgniteChance / 100f)) return;
        const float igniteDamageRatio = 0.40f;
        const float igniteDuration    = 4f;
        target.ApplyBurning(fireDmg * igniteDamageRatio / igniteDuration, igniteDuration);
    }

    // DirectAttack 전용 — 프로젝타일 없이 즉시 타격하므로 여기서 직접 적용
    private static void ApplyFireDamage(Tower tower, Enemy target, float baseDmg, bool isCrit)
    {
        if (tower.AddedFireRatio <= 0f) return;
        if (target.CurrentHp <= 0f) return;

        float fireDmg = baseDmg * tower.AddedFireRatio;
        if (isCrit) fireDmg *= 1f + tower.CritDamage / 100f;
        target.TakeDamage(fireDmg, 0f, isCrit, DamageType.Fire);

        if (tower.IgniteChance > 0f && target.CurrentHp > 0f &&
            Random.value < Mathf.Clamp01(tower.IgniteChance / 100f))
        {
            const float igniteDamageRatio = 0.40f;
            const float igniteDuration    = 4f;
            target.ApplyBurning(fireDmg * igniteDamageRatio / igniteDuration, igniteDuration);
        }
    }
}
