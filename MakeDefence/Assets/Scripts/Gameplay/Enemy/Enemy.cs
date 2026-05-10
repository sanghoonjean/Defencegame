using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static event Action<Enemy> OnEnemyDied;
    public static event Action<Enemy> OnEnemyReachedBase;

    // 활성 적 목록 (Tower가 타겟 탐색에 사용)
    public static readonly List<Enemy> ActiveEnemies = new();

    public EnemyGrade Grade { get; private set; }
    public float MaxHp     { get; private set; }
    public float CurrentHp { get; private set; }

    private float _defense;
    private float _speed;
    private int _playerDamage;
    private Vector2[] _waypoints;
    private int _waypointIndex;
    private float _stunTimer;

    private void OnEnable() => ActiveEnemies.Add(this);
    private void OnDisable() => ActiveEnemies.Remove(this);

    public void Initialize(EnemyData data, int stage, Vector2[] waypoints)
    {
        Grade = data.grade;
        _waypoints = waypoints;
        _waypointIndex = 0;
        _playerDamage = data.playerDamage;

        if (data.fixedStats)
        {
            CurrentHp = data.baseHp;
            _defense = data.baseDefense;
            _speed = data.baseSpeed;
        }
        else
        {
            float hpMult = 1f + stage * 0.05f;
            float defMult = 1f + stage * 0.05f;
            float speedMult = 1f + stage * 0.02f;
            CurrentHp = Mathf.Floor(data.baseHp * hpMult);
            _defense = Mathf.Floor(data.baseDefense * defMult);
            _speed = data.baseSpeed * speedMult;
        }
        MaxHp = CurrentHp;

        if (_waypoints != null && _waypoints.Length > 0)
        {
            transform.position = new Vector3(_waypoints[0].x, _waypoints[0].y, -1f);
            _waypointIndex = 1;
        }
    }

    private void Update()
    {
        if (_stunTimer > 0f)
        {
            _stunTimer -= Time.deltaTime;
            return;
        }
        MoveAlongPath();
    }

    public void ApplyStun(float duration)
    {
        _stunTimer = Mathf.Max(_stunTimer, duration);
    }

    private void MoveAlongPath()
    {
        if (_waypoints == null || _waypointIndex >= _waypoints.Length) return;

        Vector2 target = _waypoints[_waypointIndex];
        Vector2 next = Vector2.MoveTowards(transform.position, target, _speed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, -1f);

        if (Vector2.Distance(transform.position, target) < 0.05f)
        {
            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Length)
                ReachBase();
        }
    }

    public void TakeDamage(float damage, float armorPenRatio = 0f, bool isCrit = false)
    {
        float effectiveDefense = _defense * (1f - Mathf.Clamp01(armorPenRatio));
        float actual = Mathf.Max(1f, damage - effectiveDefense);
        CurrentHp -= actual;
        GameUIManager.ShowDamage(transform.position, actual, isCrit);
        if (CurrentHp <= 0f)
            Die();
    }

    private void Die()
    {
        OnEnemyDied?.Invoke(this);
        ObjectPoolSystem.Instance.Return(this);
    }

    private void ReachBase()
    {
        PlayerSystem.Instance.TakeDamage(_playerDamage);
        OnEnemyReachedBase?.Invoke(this);
        ObjectPoolSystem.Instance.Return(this);
    }
}
