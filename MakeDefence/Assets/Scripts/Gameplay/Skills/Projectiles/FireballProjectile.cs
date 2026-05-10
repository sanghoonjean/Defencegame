using UnityEngine;

public class FireballProjectile : ProjectileBase
{
    public float AoeRadius { get; set; }

    protected override float OnHit(Enemy target)
    {
        target.TakeDamage(_damage, _armorPen);
        if (StunChance > 0f && Random.value < Mathf.Clamp01(StunChance / 100f))
            target.ApplyStun(0.5f);

        GameUIManager.ShowAoeHit(target.transform.position, AoeRadius);
        return _damage;
    }
}
