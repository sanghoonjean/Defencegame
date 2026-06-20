using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 처치 시 큐브 픽업 생성, 웨이브 종료 시 수확/폐기 총괄.
/// ExecutionOrder 를 WaveSystem 보다 앞에 두어 Enemy.OnEnemyDied 구독을
/// 먼저 등록 → 마지막 적/보스 처치 시 픽업이 EndWave 보다 먼저 생성됨.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DroppedCubeSystem : MonoBehaviour
{
    public static DroppedCubeSystem Instance { get; private set; }
    public static event Action OnPendingChanged;

    [Header("References")]
    [SerializeField] private DroppedCubePickup pickupPrefab;
    [SerializeField] private CubeUIDisplay cubeUIDisplay;

    [Header("Drop Chance (per grade)")]
    [SerializeField, Range(0f, 1f)] private float normalKillDropChance   = 0.08f;
    [SerializeField, Range(0f, 1f)] private float magicKillDropChance    = 0.20f;
    [SerializeField, Range(0f, 1f)] private float rareKillDropChance     = 0.40f;
    [SerializeField, Range(0f, 1f)] private float uniqueKillDropChance   = 1.00f;
    [SerializeField, Range(0f, 1f)] private float lastBossKillDropChance = 1.00f;

    [Header("Drop Count (per grade)")]
    [SerializeField] private int normalKillDropCount   = 1;
    [SerializeField] private int magicKillDropCount    = 1;
    [SerializeField] private int rareKillDropCount     = 1;
    [SerializeField] private int uniqueKillDropCount   = 1;
    [SerializeField] private int lastBossKillDropCount = 3;

    [Header("Effect Tuning")]
    [SerializeField] private float collectStaggerSec   = 0.05f;
    [SerializeField] private float collectArcDuration  = 0.5f;
    [SerializeField] private float discardFadeDuration = 0.3f;
    [SerializeField] private float spawnPositionJitter = 0.3f;

    private readonly HashSet<DroppedCubePickup> _activePickups = new();
    private readonly Dictionary<CubeType, int>  _pendingCounts = new()
    {
        { CubeType.Lower,   0 },
        { CubeType.Upper,   0 },
        { CubeType.TopTier, 0 },
        { CubeType.Delete,  0 },
        { CubeType.Clone,   0 },
    };
    private bool _dropsBlocked;

    public IReadOnlyDictionary<CubeType, int> PendingCounts => _pendingCounts;

    private void Awake()
    {
        Instance = this;
        if (pickupPrefab == null)
            Debug.LogError("[DroppedCubeSystem] pickupPrefab is NULL — Inspector 에서 DroppedCubePickup 프리팹을 연결하세요");
        if (cubeUIDisplay == null)
            Debug.LogWarning("[DroppedCubeSystem] cubeUIDisplay is NULL — 수확 도착 좌표 fallback 사용");
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
        if (enemy == null || pickupPrefab == null || CubeSystem.Instance == null) return;

        (float chance, int count) = enemy.Grade switch
        {
            EnemyGrade.Normal   => (normalKillDropChance,   normalKillDropCount),
            EnemyGrade.Magic    => (magicKillDropChance,    magicKillDropCount),
            EnemyGrade.Rare     => (rareKillDropChance,     rareKillDropCount),
            EnemyGrade.Unique   => (uniqueKillDropChance,   uniqueKillDropCount),
            EnemyGrade.LastBoss => (lastBossKillDropChance, lastBossKillDropCount),
            _                   => (0f, 0),
        };
        if (count <= 0 || chance <= 0f) return;
        if (UnityEngine.Random.value > chance) return;

        Vector2 deathPos = enemy.transform.position;
        for (int i = 0; i < count; i++)
        {
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * spawnPositionJitter;
            SpawnPickup(CubeSystem.Instance.RollDrop(), deathPos + jitter);
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
        }
        else
        {
            _dropsBlocked = true;
            DiscardAll();
        }
    }

    private void SpawnPickup(CubeType type, Vector2 worldPos)
    {
        var pickup = Instantiate(pickupPrefab);
        pickup.Initialize(type, worldPos);
        _activePickups.Add(pickup);

        _pendingCounts[type] = _pendingCounts[type] + 1;
        OnPendingChanged?.Invoke();
    }

    public void UnregisterPickup(DroppedCubePickup pickup)
    {
        _activePickups.Remove(pickup);
    }

    private void CollectAll()
    {
        // 1. 데이터 격리: 현재 컬렉션 swap, 멤버는 새 빈 컬렉션
        var pickupsSnapshot = new List<DroppedCubePickup>(_activePickups);
        _activePickups.Clear();

        var countsSnapshot = new Dictionary<CubeType, int>(_pendingCounts);
        ResetPendingCounts();
        OnPendingChanged?.Invoke();

        // 2. 즉시 카운트 반영
        if (CubeSystem.Instance != null)
        {
            foreach (var kv in countsSnapshot)
                if (kv.Value > 0) CubeSystem.Instance.Add(kv.Key, kv.Value);
        }

        // 3. 시각 효과 (self-contained)
        StartCoroutine(CollectAnimationRoutine(pickupsSnapshot));
    }

    private IEnumerator CollectAnimationRoutine(List<DroppedCubePickup> pickups)
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            var pickup = pickups[i];
            if (pickup == null) continue;
            CubeType type = pickup.Type;
            Vector3 target = GetCollectTarget(type);
            pickup.StartCollect(target, collectArcDuration, () =>
            {
                if (cubeUIDisplay != null) cubeUIDisplay.PlayPunch(type);
            });
            if (collectStaggerSec > 0f)
                yield return new WaitForSeconds(collectStaggerSec);
        }
    }

    private Vector3 GetCollectTarget(CubeType type)
    {
        var cam = Camera.main;
        if (cubeUIDisplay != null && cam != null)
            return cubeUIDisplay.GetCounterWorldPoint(type, cam);
        // fallback: 화면 위쪽
        if (cam != null)
        {
            Vector3 sp = new Vector3(Screen.width * 0.5f, Screen.height + 30f, 10f);
            return cam.ScreenToWorldPoint(sp);
        }
        return Vector3.zero;
    }

    private void DiscardAll()
    {
        var pickupsSnapshot = new List<DroppedCubePickup>(_activePickups);
        _activePickups.Clear();
        ResetPendingCounts();
        OnPendingChanged?.Invoke();

        foreach (var pickup in pickupsSnapshot)
        {
            if (pickup != null) pickup.StartDiscard(discardFadeDuration);
        }
    }

    private void ResetPendingCounts()
    {
        _pendingCounts[CubeType.Lower]   = 0;
        _pendingCounts[CubeType.Upper]   = 0;
        _pendingCounts[CubeType.TopTier] = 0;
        _pendingCounts[CubeType.Delete]  = 0;
        _pendingCounts[CubeType.Clone]   = 0;
    }
}
