using UnityEngine;

public class PreciseArrowProjectile : ProjectileBase
{
    public float BonusCritChance { get; set; }

    protected override void OnHit(Enemy target)
    {
        float dmg    = _damage;
        bool  isCrit = Random.value < Mathf.Clamp01(BonusCritChance / 100f);
        if (isCrit) dmg *= 1.5f;

        target.TakeDamage(dmg, _armorPen);
    }
}
