using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    private const float MoveSpeed = 8f;
    private const float HitRadius = 0.15f;

    private Enemy _target;
    protected float _damage;
    protected float _armorPen;
    private bool _launched;

    public float StunChance   { get; set; }
    public float SplashRadius { get; set; }

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
            OnHit(_target);
            ApplySplash(_target);
            ReturnToPool();
        }
    }

    protected virtual void OnHit(Enemy target)
    {
        target.TakeDamage(_damage, _armorPen);
    }

    private void ApplySplash(Enemy primaryTarget)
    {
        if (SplashRadius <= 0f || _damage <= 0f) return;

        float   splashDmg = _damage * 0.5f;
        float   radiusSq  = SplashRadius * SplashRadius;
        Vector2 pos       = primaryTarget.transform.position;
        var     enemies   = Enemy.ActiveEnemies;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null || e == primaryTarget) continue;
            if (((Vector2)e.transform.position - pos).sqrMagnitude <= radiusSq)
                e.TakeDamage(splashDmg, _armorPen);
        }
    }

    private void ReturnToPool()
    {
        _launched = false;
        _target   = null;
        ObjectPoolSystem.Instance.ReturnProjectile(this);
    }
}
