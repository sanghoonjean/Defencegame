using UnityEngine;

public class LightningArrowProjectile : ProjectileBase
{
    [SerializeField] private GameObject _aoeFxPrefab;

    public float    AoeRadius     { get; set; }
    public float    ShockDuration { get; set; }
    public float    CritChance    { get; set; }
    public float    CritDamage    { get; set; }
    public AoeShape AoeShape      { get; set; }
    public float    AoeAngle      { get; set; }
    public float    AoeWidth      { get; set; }

    protected override float OnHit(Enemy target)
    {
        bool  isCrit = Random.value < Mathf.Clamp01(CritChance / 100f);
        _hitIsCrit   = isCrit;
        float dmg    = _damage;
        if (isCrit) dmg *= 1f + CritDamage / 100f;

        Vector2 hitPos = target.transform.position;
        Vector2 forward = _lastMoveDir.sqrMagnitude > 0f ? _lastMoveDir : Vector2.right;

        foreach (var e in Enemy.ActiveEnemies.ToArray())
        {
            if (e == null) continue;
            if (!AoeUtils.IsInAoe(e.transform.position, hitPos, forward,
                    AoeShape, AoeRadius, AoeWidth, AoeAngle)) continue;

            AddHitEnemy(e);
            e.TakeDamage(dmg, _armorPen, isCrit, DamageType.Lightning);
            if (isCrit)
                e.ApplyStun(ShockDuration);
            ApplyFireOnHit(e, isCrit);
            ApplyDotOnHit(e);
        }

        AoeUtils.ShowAoeHit(hitPos, forward, AoeShape, AoeRadius, AoeWidth, AoeAngle, _aoeFxPrefab);
        return dmg;
    }
}
