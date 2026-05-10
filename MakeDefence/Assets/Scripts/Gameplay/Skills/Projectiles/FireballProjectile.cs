using UnityEngine;

public class FireballProjectile : ProjectileBase
{
    public float AoeRadius { get; set; }
    public bool  IsCrit    { get; set; }

    protected override float OnHit(Enemy target)
    {
        _hitIsCrit = IsCrit;
        target.TakeDamage(_damage, _armorPen, IsCrit);
        if (StunChance > 0f && Random.value < Mathf.Clamp01(StunChance / 100f))
            target.ApplyStun(0.5f);
        return _damage;
    }
}
