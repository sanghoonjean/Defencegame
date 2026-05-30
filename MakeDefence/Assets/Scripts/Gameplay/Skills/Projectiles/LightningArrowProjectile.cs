using UnityEngine;

public class LightningArrowProjectile : ProjectileBase
{
    [SerializeField] private GameObject _aoeFxPrefab;

    public float AoeRadius     { get; set; }
    public float ShockDuration { get; set; }
    public float CritChance    { get; set; }
    public float CritDamage    { get; set; }

    protected override float OnHit(Enemy target)
    {
        bool  isCrit = Random.value < Mathf.Clamp01(CritChance / 100f);
        _hitIsCrit   = isCrit;
        float dmg    = _damage;
        if (isCrit) dmg *= 1f + CritDamage / 100f;

        Vector2 hitPos   = target.transform.position;
        float   radiusSq = AoeRadius * AoeRadius;

        foreach (var e in Enemy.ActiveEnemies.ToArray())
        {
            if (e == null) continue;
            if (((Vector2)e.transform.position - hitPos).sqrMagnitude > radiusSq) continue;

            AddHitEnemy(e);
            e.TakeDamage(dmg, _armorPen, isCrit, DamageType.Lightning);
            if (isCrit)
                e.ApplyStun(ShockDuration);
            ApplyFireOnHit(e, isCrit);
            ApplyDotOnHit(e);
        }

        GameUIManager.ShowAoeHit(hitPos, AoeRadius, _aoeFxPrefab);
        return dmg;
    }
}
