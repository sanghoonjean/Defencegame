using UnityEngine;

public class FireballProjectile : ProjectileBase
{
    [SerializeField] private GameObject _aoeFxPrefab;

    public float AoeRadius { get; set; }
    public bool  IsCrit    { get; set; }

    protected override float OnHit(Enemy target)
    {
        AoeFxPrefab = _aoeFxPrefab;
        _hitIsCrit  = IsCrit;
        target.TakeDamage(_damage, _armorPen, IsCrit, DamageType.Fire);
        TryIgnite(target, _damage);
        if (StunChance > 0f && Random.value < Mathf.Clamp01(StunChance / 100f))
            target.ApplyStun(0.5f);
        ApplyFireOnHit(target, IsCrit);
        ApplyDotOnHit(target);
        return _damage;
    }
}
