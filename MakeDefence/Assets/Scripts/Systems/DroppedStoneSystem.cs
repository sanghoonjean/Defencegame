using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 처치 시 일정 확률로 차원석 픽업 spawn.
/// 웨이브 클리어 시 픽업 fade out + 인벤 +1 (개당), 추가로 클리어 보장 1개.
/// 웨이브 실패 시 픽업 폐기 (인벤 추가 없음).
///
/// `[DefaultExecutionOrder(-100)]` 필수 — WaveSystem(기본 0) 보다 먼저
/// Enemy.OnEnemyDied 를 구독해 마지막 킬 → 픽업 등록 → OnWaveEnded(true)
/// → CollectAll 순서 보장 (DroppedCubeSystem 의 검증된 패턴).
/// </summary>
[DefaultExecutionOrder(-100)]
public class DroppedStoneSystem : MonoBehaviour
{
    public static DroppedStoneSystem Instance { get; private set; }
    public static event Action OnPendingChanged;

    [Header("References")]
    [SerializeField] private DroppedStonePickup pickupPrefab;

    [Header("Drop Table (Lower 큐브와 동일)")]
    [SerializeField] private StoneDropChanceTable dropTable = new();

    [Header("Effect Tuning")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float spawnArrangementRadius = 0.5f;
    [SerializeField] private float spawnPositionJitter    = 0.2f;
    [SerializeField] private float spawnMinSeparation     = 0.6f;
    [SerializeField] private int   spawnSeparationAttempts = 6;

    private readonly HashSet<DroppedStonePickup> _activePickups = new();
    private int _pending;
    private bool _dropsBlocked;

    public int Pending => _pending;

    private void Awake()
    {
        Instance = this;
        if (pickupPrefab == null)
            Debug.LogError("[DroppedStoneSystem] pickupPrefab is NULL — Inspector 에서 DroppedStonePickup 연결 필요");
    }

    private void OnEnable()
    {
        Enemy.OnEnemyDied        += HandleEnemyDied;
        WaveSystem.OnWaveStarted += HandleWaveStarted;
        WaveSystem.OnWaveEnded   += HandleWaveEnded;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDied        -= HandleEnemyDied;
        WaveSystem.OnWaveStarted -= HandleWaveStarted;
        WaveSystem.OnWaveEnded   -= HandleWaveEnded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (_dropsBlocked) return;
        if (enemy == null || pickupPrefab == null) return;

        var (chance, count) = dropTable.Resolve((int)enemy.Grade);
        if (count <= 0 || chance <= 0f) return;
        if (UnityEngine.Random.value > chance) return;

        Vector2 deathPos = enemy.transform.position;
        float baseAngle = UnityEngine.Random.value * Mathf.PI * 2f;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Vector2.zero;
            if (count > 1)
            {
                float angle = baseAngle + (i / (float)count) * Mathf.PI * 2f;
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnArrangementRadius;
            }
            offset += UnityEngine.Random.insideUnitCircle * spawnPositionJitter;
            SpawnPickup(deathPos + offset);
        }
    }

    private void HandleWaveStarted(int stage)
    {
        _dropsBlocked = false;
    }

    private void HandleWaveEnded(bool cleared)
    {
        if (cleared)
        {
            CollectAll();
            GrantClearBonus();
        }
        else
        {
            _dropsBlocked = true;
            DiscardAll();
        }
    }

    private void SpawnPickup(Vector2 worldPos)
    {
        worldPos = DroppedPickupRegistry.ResolveSpawnPos(worldPos, spawnMinSeparation, spawnSeparationAttempts);
        var pickup = Instantiate(pickupPrefab);
        pickup.Initialize(worldPos);
        _activePickups.Add(pickup);
        _pending++;
        OnPendingChanged?.Invoke();
    }

    public void UnregisterPickup(DroppedStonePickup pickup)
    {
        _activePickups.Remove(pickup);
    }

    private void CollectAll()
    {
        var snapshot = new List<DroppedStonePickup>(_activePickups);
        _activePickups.Clear();
        int harvested = _pending;
        _pending = 0;
        OnPendingChanged?.Invoke();

        if (ShopSystem.Instance != null)
        {
            for (int i = 0; i < harvested; i++)
                ShopSystem.Instance.AddStone(DimensionStone.CreateRandom());
        }

        foreach (var pickup in snapshot)
            if (pickup != null) pickup.StartCollectFade(fadeDuration);
    }

    private void DiscardAll()
    {
        var snapshot = new List<DroppedStonePickup>(_activePickups);
        _activePickups.Clear();
        _pending = 0;
        OnPendingChanged?.Invoke();

        foreach (var pickup in snapshot)
            if (pickup != null) pickup.StartDiscardFade(fadeDuration);
    }

    /// <summary>웨이브 클리어 시 픽업과 별개로 무조건 차원석 1개 추가.</summary>
    private void GrantClearBonus()
    {
        if (ShopSystem.Instance == null) return;
        ShopSystem.Instance.AddStone(DimensionStone.CreateRandom());
        Debug.Log("[DroppedStoneSystem] 클리어 보장 차원석 +1");
    }
}
