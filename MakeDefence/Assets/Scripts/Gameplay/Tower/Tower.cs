using System;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public static event Action<Tower> OnTowerPlaced;

    // tech-debt: 타워 기본 스탯 미확정 — Inspector에서 조정
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1f;

    public Vector2Int TileCoord { get; private set; }
    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;

    private float _attackTimer;

    public void Place(Vector2Int coord)
    {
        TileCoord = coord;
        OnTowerPlaced?.Invoke(this);
    }

    private void Update()
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer < attackCooldown) return;

        var target = FindTarget();
        if (target == null) return;

        Attack(target);
        _attackTimer = 0f;
    }

    private Enemy FindTarget()
    {
        Enemy closest = null;
        float minDist = attackRange;

        foreach (var e in Enemy.ActiveEnemies)
        {
            if (e == null) continue;
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = e;
            }
        }
        return closest;
    }

    private void Attack(Enemy target)
    {
        // Phase 2에서 스킬/아이템 옵션 합산으로 확장
        target.TakeDamage(attackDamage);
    }
}
