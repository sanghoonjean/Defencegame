using UnityEngine;

public class CausticGround : MonoBehaviour
{
    private float _radius;
    private float _tickDamage;
    private float _armorPen;
    private float _tickInterval;
    private float _duration;
    private float _tickTimer;
    private float _lifeTimer;

    public void Init(float radius, float tickDamage, float armorPen,
                     float tickInterval, float duration)
    {
        _radius       = radius;
        _tickDamage   = tickDamage;
        _armorPen     = armorPen;
        _tickInterval = tickInterval;
        _duration     = duration;
        _tickTimer    = 0f;
        _lifeTimer    = 0f;
    }

    private void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer = 0f;
            ApplyDot();
        }
    }

    private void ApplyDot()
    {
        float   radiusSq = _radius * _radius;
        Vector2 pos      = transform.position;

        foreach (var e in Enemy.ActiveEnemies.ToArray())
        {
            if (e == null) continue;
            if (((Vector2)e.transform.position - pos).sqrMagnitude <= radiusSq)
                e.TakeDamage(_tickDamage, _armorPen);
        }
    }
}
