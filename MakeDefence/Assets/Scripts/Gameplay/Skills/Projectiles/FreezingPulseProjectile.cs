using UnityEngine;

public class FreezingPulseProjectile : ProjectileBase
{
    public float MaxRangeBonus  { get; set; }
    public float MaxRange       { get; set; }
    public float FreezeDuration { get; set; }
    public float CritChance     { get; set; }
    public float CritDamage     { get; set; }

    private Vector2 _launchOrigin;

    public void LaunchFrom(Vector2 origin, Enemy target, float damage, float armorPen)
    {
        _launchOrigin = origin;
        Launch(origin, target, damage, armorPen);
    }

    protected override void OnHit(Enemy target)
    {
        float dist       = Vector2.Distance(_launchOrigin, target.transform.position);
        float t          = MaxRange > 0f ? Mathf.Clamp01(dist / MaxRange) : 1f;
        float multiplier = Mathf.Lerp(MaxRangeBonus, 1f, t);

        float dmg    = _damage * multiplier;
        bool  isCrit = Random.value < Mathf.Clamp01(CritChance / 100f);
        if (isCrit) dmg *= 1f + CritDamage / 100f;

        target.TakeDamage(dmg, _armorPen);

        if (StunChance > 0f && Random.value < Mathf.Clamp01(StunChance / 100f))
            target.ApplyStun(FreezeDuration);
    }
}
