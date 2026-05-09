using UnityEngine;

public static class SkillDispatcher
{
    public static void Execute(Tower tower, Enemy target)
    {
        var skill = tower.EquippedSkill;
        if (skill == null)
        {
            DirectAttack(tower, target);
            return;
        }

        switch (skill.skillType)
        {
            case SkillType.Fireball:
                LaunchFireball(tower, target);
                break;
            default:
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
        if (proj == null) return;

        var skill = tower.EquippedSkill;
        proj.AoeRadius = skill.aoeRadius;
        proj.Launch(tower.transform.position, target, skill.baseDamage, tower.ArmorPen / 100f);
    }
}
