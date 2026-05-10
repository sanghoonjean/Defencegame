using UnityEngine;

public class CausticArrowProjectile : ProjectileBase
{
    [SerializeField] private GameObject causticGroundPrefab;

    public float AoeRadius    { get; set; }
    public float DotDuration  { get; set; }
    public float TickDamage   { get; set; }
    public float TickInterval { get; set; }

    protected override void OnHit(Enemy target)
    {
        if (causticGroundPrefab == null)
        {
            Debug.LogError("[CausticArrowProjectile] causticGroundPrefab이 연결되지 않음");
            return;
        }

        var go = Instantiate(causticGroundPrefab, target.transform.position, Quaternion.identity);
        var cg = go.GetComponent<CausticGround>();
        if (cg == null)
        {
            Debug.LogError("[CausticArrowProjectile] causticGroundPrefab에 CausticGround 컴포넌트 없음");
            Destroy(go);
            return;
        }
        cg.Init(AoeRadius, TickDamage, _armorPen, TickInterval, DotDuration);
        GameUIManager.ShowAoeHit(target.transform.position, AoeRadius);
    }
}
