using UnityEngine;

public class LightningArrowProjectile : ProjectileBase
{
    public float AoeRadius     { get; set; }
    public float ShockDuration { get; set; }
    public float CritChance    { get; set; }
    public float CritDamage    { get; set; }

    protected override void OnHit(Enemy target)
    {
        bool  isCrit = Random.value < Mathf.Clamp01(CritChance / 100f);
        float dmg    = _damage;
        if (isCrit) dmg *= 1f + CritDamage / 100f;

        Vector2 hitPos   = target.transform.position;
        float   radiusSq = AoeRadius * AoeRadius;

        foreach (var e in Enemy.ActiveEnemies.ToArray())
        {
            if (e == null) continue;
            if (((Vector2)e.transform.position - hitPos).sqrMagnitude > radiusSq) continue;

            e.TakeDamage(dmg, _armorPen);
            if (isCrit)
                e.ApplyStun(ShockDuration);
        }
    }
}
