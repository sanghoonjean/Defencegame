using UnityEngine;

public class PreciseArrowProjectile : ProjectileBase
{
    public float BonusCritChance { get; set; }
    public float BonusCritDamage { get; set; }

    protected override float OnHit(Enemy target)
    {
        float dmg    = _damage;
        bool  isCrit = Random.value < Mathf.Clamp01(BonusCritChance / 100f);
        if (isCrit) dmg *= 1f + BonusCritDamage / 100f;

        target.TakeDamage(dmg, _armorPen);

        if (StunChance > 0f && Random.value < Mathf.Clamp01(StunChance / 100f))
            target.ApplyStun(0.5f);

        return dmg;
    }
}
