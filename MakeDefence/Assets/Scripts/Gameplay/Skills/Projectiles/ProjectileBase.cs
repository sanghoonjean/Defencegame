using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    private const float MoveSpeed = 8f;
    private const float HitRadius = 0.15f;

    private Enemy _target;
    protected float _damage;
    protected float _armorPen;
    protected bool  _hitIsCrit;
    private bool _launched;

    public float StunChance         { get; set; }
    public float SplashRadius       { get; set; }
    public float SplashStunDuration { get; set; }

    public float AddedFireRatio { get; set; }
    public float FireCritDamage { get; set; }
    public float FireBaseDamage { get; set; }

    public float DotTickDamage  { get; set; }
    public float DotDuration    { get; set; }

    public void Launch(Vector2 origin, Enemy target, float damage, float armorPen)
    {
        transform.position = new Vector3(origin.x, origin.y, -1f);
        _target = target;
        _damage = damage;
        _armorPen = armorPen;
        _launched = true;
    }

    private void Update()
    {
        if (!_launched) return;

        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }

        Vector2 current = transform.position;
        Vector2 dest    = _target.transform.position;
        Vector2 next    = Vector2.MoveTowards(current, dest, MoveSpeed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, -1f);

        if (Vector2.Distance(next, dest) < HitRadius)
        {
            float actualDmg = OnHit(_target);
            ApplySplash(_target, actualDmg, _hitIsCrit);
            if (SplashRadius > 0f && actualDmg > 0f)
                GameUIManager.ShowAoeHit(_target.transform.position, SplashRadius);
            ReturnToPool();
        }
    }

    protected virtual float OnHit(Enemy target)
    {
        target.TakeDamage(_damage, _armorPen);
        ApplyFireOnHit(target, false);
        ApplyDotOnHit(target);
        return _damage;
    }

    protected void ApplyDotOnHit(Enemy target)
    {
        if (DotTickDamage <= 0f || DotDuration <= 0f) return;
        if (target.CurrentHp <= 0f) return;
        target.ApplyDot(DotTickDamage, DotDuration);
    }

    protected void ApplyFireOnHit(Enemy target, bool isCrit)
    {
        if (AddedFireRatio <= 0f) return;
        if (target.CurrentHp <= 0f) return;
        float fireDmg = FireBaseDamage * AddedFireRatio;
        if (isCrit) fireDmg *= 1f + FireCritDamage / 100f;
        target.TakeDamage(fireDmg, 0f, isCrit, DamageType.Fire);
    }

    private void ApplySplash(Enemy primaryTarget, float actualDamage, bool isCrit)
    {
        if (SplashRadius <= 0f) return;

        float splashDmg = actualDamage;
        if (splashDmg <= 0f) return;

        float   radiusSq = SplashRadius * SplashRadius;
        Vector2 pos      = primaryTarget.transform.position;
        var     enemies  = Enemy.ActiveEnemies;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null || e == primaryTarget) continue;
            if (((Vector2)e.transform.position - pos).sqrMagnitude <= radiusSq)
            {
                e.TakeDamage(splashDmg, _armorPen, isCrit);
                if (StunChance > 0f && SplashStunDuration > 0f &&
                    Random.value < Mathf.Clamp01(StunChance / 100f))
                    e.ApplyStun(SplashStunDuration);
                ApplyFireOnHit(e, isCrit);
            }
        }
    }

    private void ReturnToPool()
    {
        _launched = false;
        _target   = null;
        AddedFireRatio = 0f;
        FireCritDamage = 0f;
        FireBaseDamage = 0f;
        DotTickDamage  = 0f;
        DotDuration    = 0f;
        ObjectPoolSystem.Instance.ReturnProjectile(this);
    }
}

