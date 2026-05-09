using UnityEngine;

public static class SkillDispatcher
{
    public static void Execute(Tower tower, Enemy target)
    {
        var skill = tower.EquippedSkill;
        if (skill == null)
        {
            Debug.Log("[SkillDispatcher] 스킬 없음 → 직접 공격");
            DirectAttack(tower, target);
            return;
        }

        Debug.Log($"[SkillDispatcher] 스킬 타입: {skill.skillType}");
        switch (skill.skillType)
        {
            case SkillType.Fireball:
                LaunchFireball(tower, target);
                break;
            default:
                Debug.Log($"[SkillDispatcher] 미구현 스킬 → 직접 공격");
                DirectAttack(tower, target);
                break;
        }
    }

    private static void DirectAttack(Tower tower, Enemy target)
    {
        float dmg    = tower.AttackDamage;
        bool  isCrit = Random.value < Mathf.Clamp01(tower.CritChance / 100f);
        if (isCrit) dmg *= 1f + tower.CritDamage / 100f;

        target.TakeDamage(dmg, tower.ArmorPen / 100f);

        if (tower.StunChance > 0f && Random.value < Mathf.Clamp01(tower.StunChance / 100f))
            target.ApplyStun(0.5f);
    }

    private static void LaunchFireball(Tower tower, Enemy target)
    {
        var proj = ObjectPoolSystem.Instance.GetProjectile<FireballProjectile>();
        Debug.Log($"[SkillDispatcher] FireballProjectile 획득: {(proj != null ? "성공" : "실패")}");
        if (proj == null) { DirectAttack(tower, target); return; }

        var   skill  = tower.EquippedSkill;
        float dmg    = tower.AttackDamage + skill.baseDamage;
        bool  isCrit = Random.value < Mathf.Clamp01(tower.CritChance / 100f);
        if (isCrit) dmg *= 1f + tower.CritDamage / 100f;

        proj.AoeRadius  = skill.aoeRadius;
        proj.StunChance = tower.StunChance;
        proj.Launch(tower.transform.position, target, dmg, tower.ArmorPen / 100f);
    }
}
