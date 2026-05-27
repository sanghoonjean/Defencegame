using System.Collections.Generic;
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

    public int ChainCount  { get; set; }
    public int PierceCount { get; set; }
    public DamageType SplashDamageType { get; set; } = DamageType.Physical;
    private readonly HashSet<Enemy> _hitEnemies = new();
    private Vector2 _lastMoveDir;

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
        _lastMoveDir    = (dest - current).normalized;
        Vector2 next    = Vector2.MoveTowards(current, dest, MoveSpeed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, -1f);

        if (Vector2.Distance(next, dest) < HitRadius)
        {
            _hitEnemies.Add(_target);
            float actualDmg = OnHit(_target);
            ApplySplash(_target, actualDmg, _hitIsCrit);
            if (SplashRadius > 0f && actualDmg > 0f)
                GameUIManager.ShowAoeHit(_target.transform.position, SplashRadius);

            if (PierceCount > 0 && TryPierce())
                return;

            if (ChainCount > 0 && TryChain())
                return;

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

    protected void AddHitEnemy(Enemy e) => _hitEnemies.Add(e);

    private bool TryPierce()
    {
        Vector2 currentPos = transform.position;
        Enemy   nearest    = null;
        float   minDist    = float.MaxValue;

        foreach (var e in Enemy.ActiveEnemies)
        {
            if (e == null || _hitEnemies.Contains(e)) continue;
            Vector2 toEnemy = (Vector2)e.transform.position - currentPos;
            if (Vector2.Dot(_lastMoveDir, toEnemy.normalized) <= 0f) continue;
            float dist = toEnemy.magnitude;
            if (dist < minDist) { minDist = dist; nearest = e; }
        }

        if (nearest == null) return false;

        PierceCount--;
        _target   = nearest;
        _launched = true;
        return true;
    }

    protected virtual void OnChain(Vector2 chainOrigin) { }

    private bool TryChain()
    {
        Vector2 currentPos = transform.position;
        Enemy   nearest    = null;
        float   minDist    = float.MaxValue;

        foreach (var e in Enemy.ActiveEnemies)
        {
            if (e == null || _hitEnemies.Contains(e)) continue;
            float dist = Vector2.Distance(currentPos, e.transform.position);
            if (dist < minDist) { minDist = dist; nearest = e; }
        }

        if (nearest == null) return false;

        ChainCount--;
        OnChain(currentPos);
        _target   = nearest;
        _launched = true;
        return true;
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
                _hitEnemies.Add(e);
                e.TakeDamage(splashDmg, _armorPen, isCrit, SplashDamageType);
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
        ChainCount         = 0;
        PierceCount        = 0;
        SplashDamageType   = DamageType.Physical;
        _lastMoveDir       = Vector2.zero;
        _hitEnemies.Clear();
        ObjectPoolSystem.Instance.ReturnProjectile(this);
    }
}

