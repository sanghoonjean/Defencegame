using System;
using System.Collections;
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
    public int RouteIndex  { get; private set; }

    // transform.position에서 스프라이트 상단까지의 월드 단위 거리 (HP 바를 머리 위에 배치하는 데 사용)
    // 스프라이트 pivot이 중앙이 아닐 수 있어 bounds.extents가 아닌 실제 위치 차이로 계산
    public float SpriteTopOffset => _spriteRenderer != null ? _spriteRenderer.bounds.max.y - transform.position.y : 0f;

    private float _defense;
    private float _speed;
    private int _playerDamage;
    private Vector2[] _waypoints;
    private int _waypointIndex;
    private float _stunTimer;

    private SpriteRenderer _spriteRenderer;

    private float _fireResistance;
    private float _coldResistance;
    private float _lightningResistance;
    private float _poisonResistance;

    private Coroutine _dotCoroutine;
    private Coroutine _burnCoroutine;
    private float     _currentBurnDps;
    private const float DotTickInterval = 0.5f;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable() => ActiveEnemies.Add(this);

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
        if (_dotCoroutine  != null) { StopCoroutine(_dotCoroutine);  _dotCoroutine  = null; }
        if (_burnCoroutine != null) { StopCoroutine(_burnCoroutine); _burnCoroutine = null; }
        _currentBurnDps = 0f;
    }

    public void Initialize(EnemyData data, int stage, Vector2[] waypoints, int routeIndex)
        => Initialize(data, stage, waypoints, routeIndex, RiftWaveModifiers.Default);

    public void Initialize(EnemyData data, int stage, Vector2[] waypoints, int routeIndex, RiftWaveModifiers riftMods)
    {
        Grade = data.grade;
        RouteIndex = routeIndex;
        _waypoints = waypoints;
        _waypointIndex = 0;
        _playerDamage = Mathf.Max(1, Mathf.RoundToInt(data.playerDamage * riftMods.DamageMult));
        _stunTimer = 0f;

        if (_spriteRenderer != null) _spriteRenderer.flipX = false;

        if (data.fixedStats)
        {
            CurrentHp = data.baseHp     * riftMods.HpMult;
            _defense  = data.baseDefense * riftMods.DefenseMult;
            _speed    = data.baseSpeed   * riftMods.SpeedMult;
        }
        else
        {
            float hpMult = 1f + stage * 0.05f;
            float defMult = 1f + stage * 0.05f;
            float speedMult = 1f + stage * 0.02f;
            CurrentHp = Mathf.Floor(data.baseHp * hpMult * riftMods.HpMult);
            _defense  = Mathf.Floor(data.baseDefense * defMult * riftMods.DefenseMult);
            _speed    = data.baseSpeed * speedMult * riftMods.SpeedMult;
        }
        MaxHp = CurrentHp;
        _fireResistance      = Mathf.Clamp(data.fireResistance,      -1f, 0.9f);
        _coldResistance      = Mathf.Clamp(data.coldResistance,      -1f, 0.9f);
        _lightningResistance = Mathf.Clamp(data.lightningResistance, -1f, 0.9f);
        _poisonResistance    = Mathf.Clamp(data.poisonResistance,    -1f, 0.9f);

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

    /// <summary>
    /// 타워 배치/이동/삭제로 인해 실시간 재계산된 경로로 교체한다. 이 경로는 이미
    /// "현재 위치"를 포함하지 않고 바로 다음 목표부터 담고 있으므로 인덱스 0부터 시작한다.
    /// </summary>
    public void SetPath(Vector2[] newPath)
    {
        _waypoints = newPath;
        _waypointIndex = 0;
    }

    public void ApplyStun(float duration)
    {
        _stunTimer = Mathf.Max(_stunTimer, duration);
    }

    public void ApplyBurning(float dps, float duration)
    {
        if (dps <= 0f || duration <= 0f) return;
        if (dps <= _currentBurnDps) return;

        _currentBurnDps = dps;
        if (_burnCoroutine != null) StopCoroutine(_burnCoroutine);
        _burnCoroutine = StartCoroutine(BurnCoroutine(dps, duration));
    }

    private IEnumerator BurnCoroutine(float dps, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(DotTickInterval);
            elapsed += DotTickInterval;
            if (CurrentHp <= 0f) break;
            float tickDmg = dps * DotTickInterval;
            float resistance = _fireResistance;
            float actual = Mathf.Max(1f, tickDmg * (1f - resistance));
            CurrentHp -= actual;
            GameUIManager.ShowDamage(transform.position, actual, false, DamageType.Fire);
            if (CurrentHp <= 0f) { Die(); break; }
        }
        if (_currentBurnDps == dps) _currentBurnDps = 0f;
        _burnCoroutine = null;
    }

    public void ApplyDot(float tickDamage, float duration)
    {
        if (tickDamage <= 0f || duration <= 0f) return;
        if (_dotCoroutine != null) StopCoroutine(_dotCoroutine);
        _dotCoroutine = StartCoroutine(DotCoroutine(tickDamage, duration));
    }

    private IEnumerator DotCoroutine(float tickDamage, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(DotTickInterval);
            elapsed += DotTickInterval;
            if (CurrentHp <= 0f) break;
            TakeDamage(tickDamage, 0f, false, DamageType.Energy);
        }
        _dotCoroutine = null;
    }

    private void MoveAlongPath()
    {
        if (_waypoints == null || _waypointIndex >= _waypoints.Length) return;

        Vector2 target = _waypoints[_waypointIndex];
        Vector2 current = transform.position;
        Vector2 next = Vector2.MoveTowards(current, target, _speed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, -1f);

        if (_spriteRenderer != null)
        {
            float dx = target.x - current.x;
            if (Mathf.Abs(dx) > 0.001f)
                _spriteRenderer.flipX = dx < 0f;
        }

        if (Vector2.Distance(transform.position, target) < 0.05f)
        {
            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Length)
                ReachBase();
        }
    }

    public void TakeDamage(float damage, float armorPenRatio = 0f, bool isCrit = false,
                           DamageType type = DamageType.Physical)
    {
        float effectiveDefense = (type == DamageType.Physical)
            ? _defense * (1f - Mathf.Clamp01(armorPenRatio))
            : 0f;
        float resistance = type switch
        {
            DamageType.Fire      => _fireResistance,
            DamageType.Cold      => _coldResistance,
            DamageType.Lightning => _lightningResistance,
            DamageType.Poison    => _poisonResistance,
            _                    => 0f
        };
        float actual = Mathf.Max(1f, (damage - effectiveDefense) * (1f - resistance));
        CurrentHp -= actual;
        GameUIManager.ShowDamage(transform.position, actual, isCrit, type);
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
