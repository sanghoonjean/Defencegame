using UnityEngine;

// Molten Strike 폭발 투사체.
// base.Launch 미사용 — 적 추적이 아니라 좌표 → 좌표 포물선 비행을 한다.
// base._launched 는 false 유지되어 ProjectileBase.Update 가 early-return 한다.
public class MagmaProjectile : ProjectileBase
{
    [SerializeField] private GameObject _aoeFxPrefab;

    private const float FlightDuration = 0.5f;
    private const float ArcHeight      = 1.5f;

    private bool    _arcLaunched;
    private float   _arcElapsed;
    private Vector2 _arcOrigin;
    private Vector2 _arcLand;

    public float ExplosionRadius     { get; set; }
    public float ProjectileRadius    { get; set; }
    public float BasePhysDamage      { get; set; }
    public float BaseFireDamage      { get; set; }
    public float ProjectileLessRatio { get; set; }

    public void LaunchArc(Vector2 origin, Vector2 landPos, float armorPen)
    {
        _arcOrigin   = origin;
        _arcLand     = landPos;
        _arcElapsed  = 0f;
        _arcLaunched = true;
        _armorPen    = armorPen;
        transform.position = new Vector3(origin.x, origin.y, -1f);
    }

    protected override void Update()
    {
        if (!_arcLaunched) return;

        _arcElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_arcElapsed / FlightDuration);

        Vector2 flat = Vector2.Lerp(_arcOrigin, _arcLand, t);
        float   arc  = ArcHeight * 4f * t * (1f - t);
        transform.position = new Vector3(flat.x, flat.y + arc, -1f);

        if (t >= 1f)
        {
            OnLand();
            _arcLaunched = false;
            ReturnToPool();
        }
    }

    private void OnLand()
    {
        float retain = 1f - Mathf.Clamp01(ProjectileLessRatio);
        float phys   = BasePhysDamage * retain;
        float fire   = BaseFireDamage * retain;

        Vector2 center   = _arcLand;
        float   radiusSq = ExplosionRadius * ExplosionRadius;

        foreach (var e in Enemy.ActiveEnemies.ToArray())
        {
            if (e == null) continue;
            if (((Vector2)e.transform.position - center).sqrMagnitude > radiusSq) continue;

            if (phys > 0f)
                e.TakeDamage(phys, _armorPen, false, DamageType.Physical);
            if (fire > 0f && e.CurrentHp > 0f)
            {
                e.TakeDamage(fire, 0f, false, DamageType.Fire);
                TryIgnite(e, fire);
            }
            ApplyDotOnHit(e);
        }

        GameUIManager.ShowAoeHit(center, ExplosionRadius, _aoeFxPrefab);
    }
}
