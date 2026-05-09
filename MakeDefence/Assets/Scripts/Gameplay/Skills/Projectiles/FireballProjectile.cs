using UnityEngine;

public class FireballProjectile : ProjectileBase
{
    public float AoeRadius { get; set; }

    protected override void OnHit(Enemy target)
    {
        Vector2 hitPos   = target.transform.position;
        float   radiusSq = AoeRadius * AoeRadius;

        foreach (var e in Enemy.ActiveEnemies.ToArray())
        {
            if (e == null) continue;
            if (((Vector2)e.transform.position - hitPos).sqrMagnitude <= radiusSq)
                e.TakeDamage(_damage, _armorPen);
        }
    }
}
