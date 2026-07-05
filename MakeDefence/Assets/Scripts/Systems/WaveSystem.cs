using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSystem : MonoBehaviour
{
    public static WaveSystem Instance { get; private set; }

    public static event Action<int> OnWaveStarted;
    public static event Action<bool> OnWaveEnded;  // true = 클리어
    public static event Action<int> OnRiftRewardGranted;  // 균열 클리어 시 추가 큐브 수
    public static event Action<int> OnRouteCleared;  // 해당 route에 스폰된 몬스터가 모두 사라짐
    public static event Action<int, int> OnAliveCountChanged;  // (alive, total)

    public bool IsWaveActive { get; private set; }
    public bool IsRiftWaveActive { get; private set; }
    public int CurrentStage { get; private set; } = 1;
    public int UnlockedStage { get; private set; } = 1;
    public int AliveCount => _aliveCount;
    public int TotalCount => _totalCount;

    // tech-debt: 스폰 간격 미확정 — Inspector에서 조정
    [SerializeField] private float spawnInterval = 1f;

    [SerializeField] private EnemyData normalData;
    [SerializeField] private EnemyData magicData;
    [SerializeField] private EnemyData rareData;
    [SerializeField] private EnemyData uniqueData;

    // 균열 클리어 시 보너스로 지급할 베이스 큐브 수 (잠정값)
    [SerializeField] private int riftBaseRewardCubes = 3;

    private bool _autoWave;
    private int _aliveCount;
    private int _totalCount;
    private int[] _aliveCountByRoute;
    private Coroutine _spawnCoroutine;
    private RiftWaveModifiers _currentRiftMods = RiftWaveModifiers.Default;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Enemy.OnEnemyDied += HandleEnemyRemoved;
        Enemy.OnEnemyReachedBase += HandleEnemyRemoved;
        PlayerSystem.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDied -= HandleEnemyRemoved;
        Enemy.OnEnemyReachedBase -= HandleEnemyRemoved;
        PlayerSystem.OnPlayerDied -= HandlePlayerDied;
    }

    public void SetDifficulty(int stage)
    {
        if (IsWaveActive || stage < 1 || stage > UnlockedStage) return;
        CurrentStage = stage;
    }

    public void SetAutoWave(bool enabled) => _autoWave = enabled;

    // 경로 시각화 (#335) — route별로 스폰된 몬스터가 아직 남아있는지 (늦은 구독 시 초기 상태 복원용)
    public bool IsRouteActive(int routeIndex) =>
        _aliveCountByRoute != null && routeIndex >= 0 && routeIndex < _aliveCountByRoute.Length
        && _aliveCountByRoute[routeIndex] > 0;

    public void StartWave()
    {
        if (IsWaveActive) { Debug.Log("[WaveSystem] StartWave: already active"); return; }
        if (!HasSpawnRoutes()) return;

        IsWaveActive = true;
        IsRiftWaveActive = false;
        _currentRiftMods = RiftWaveModifiers.Default;

        Debug.Log($"[WaveSystem] StartWave stage={CurrentStage}");

        if (normalData == null) Debug.LogError("[WaveSystem] normalData is NULL — Inspector에서 EnemyData 연결 필요");
        if (magicData == null)  Debug.LogError("[WaveSystem] magicData is NULL");
        if (rareData == null)   Debug.LogError("[WaveSystem] rareData is NULL");
        if (uniqueData == null) Debug.LogError("[WaveSystem] uniqueData is NULL");

        PlayerSystem.Instance.ResetHp();

        var spawnList = BuildSpawnList(CurrentStage);
        Debug.Log($"[WaveSystem] spawnList count={spawnList.Count}");
        _aliveCount = spawnList.Count;
        _totalCount = spawnList.Count;
        InitAliveCountByRoute(spawnList.Count);
        _spawnCoroutine = StartCoroutine(SpawnEnemies(spawnList));

        // StartCoroutine 은 첫 yield 전까지 동기적으로 실행되므로, 첫 몬스터의 EnemyData 가
        // null이면 위 코루틴 안에서 StopWave() 가 이미 호출되어 IsWaveActive 가 false 로
        // 바뀌어 있을 수 있다. 그 경우 OnWaveStarted 를 invoke하면 구독자가 이미 끝난 웨이브를
        // 여전히 진행 중으로 오인해 마커가 영구히 걸린다 (Codex 리뷰 지적, P2).
        if (IsWaveActive)
        {
            OnWaveStarted?.Invoke(CurrentStage);
            OnAliveCountChanged?.Invoke(_aliveCount, _totalCount);
        }
    }

    /// <summary>
    /// 균열 생성기에서 호출. 차원석 옵션이 적용된 강화 웨이브를 시작한다.
    /// 일반 웨이브와 동시 활성 금지. 클리어 시에도 _autoWave 미연동(1회성).
    /// </summary>
    public bool StartRiftWave(RiftWaveModifiers modifiers)
    {
        if (IsWaveActive) { Debug.Log("[WaveSystem] StartRiftWave: already active"); return false; }
        if (!HasSpawnRoutes()) return false;

        IsWaveActive = true;
        IsRiftWaveActive = true;
        _currentRiftMods = modifiers;

        Debug.Log($"[WaveSystem] StartRiftWave stage={CurrentStage} hp={modifiers.HpMult:F2} extra={modifiers.ExtraCount} reward={modifiers.RewardCubeMult:F2}");

        if (normalData == null) Debug.LogError("[WaveSystem] normalData is NULL");
        if (magicData == null)  Debug.LogError("[WaveSystem] magicData is NULL");
        if (rareData == null)   Debug.LogError("[WaveSystem] rareData is NULL");
        if (uniqueData == null) Debug.LogError("[WaveSystem] uniqueData is NULL");

        PlayerSystem.Instance.ResetHp();

        var spawnList = BuildSpawnList(CurrentStage);
        for (int i = 0; i < modifiers.ExtraCount; i++)
            spawnList.Add(EnemyGrade.Normal);
        _aliveCount = spawnList.Count;
        _totalCount = spawnList.Count;
        InitAliveCountByRoute(spawnList.Count);
        _spawnCoroutine = StartCoroutine(SpawnEnemies(spawnList));

        // StartWave() 와 동일한 이유로 IsWaveActive 가 이미 false로 꺼졌을 수 있으므로 가드한다.
        if (IsWaveActive)
        {
            OnWaveStarted?.Invoke(CurrentStage);
            OnAliveCountChanged?.Invoke(_aliveCount, _totalCount);
        }
        return true;
    }

    // routeIndex = i % routeCount 분배 규칙(SpawnEnemies)과 동일하게 route별 스폰 예정 수를 계산한다.
    private void InitAliveCountByRoute(int spawnCount)
    {
        int routeCount = MapTileSystem.Instance.RouteCount;
        _aliveCountByRoute = new int[routeCount];
        for (int i = 0; i < spawnCount; i++)
            _aliveCountByRoute[i % routeCount]++;
    }

    public void StopWave()
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        bool wasActive = IsWaveActive;
        IsWaveActive = false;
        IsRiftWaveActive = false;

        // StopCoroutine 은 코루틴을 즉시 중단시켜 SpawnEnemies 내부의 종료 처리가 실행되지 않으므로,
        // 웨이브 취소를 구독자(MonsterPathVisualizer 등)에게 여기서 직접 알린다.
        if (wasActive)
        {
            // 스폰 도중 취소되면 아직 스폰되지 않은 몬스터는 앞으로도 HandleEnemyRemoved를
            // 호출할 계기가 없어 _aliveCount가 그 수만큼 남은 채로 멈춘다 (Codex 리뷰 지적) —
            // 웨이브가 끝났으니 0으로 확정해 HUD가 마지막 값에 멈춰있지 않도록 한다.
            _aliveCount = 0;
            OnWaveEnded?.Invoke(false);
            OnAliveCountChanged?.Invoke(_aliveCount, _totalCount);
        }
    }

    private List<EnemyGrade> BuildSpawnList(int stage)
    {
        int total = GetEnemyCount(stage);
        int uniqueCount = stage >= 10 ? 1 : 0;
        int remaining = total - uniqueCount;

        float rareRatio = Mathf.Max(0f, (stage - 4) * 0.03f);
        float magicRatio = Mathf.Min(stage * 0.02f, 0.35f);

        int rareCount = Mathf.FloorToInt(remaining * rareRatio);
        int magicCount = Mathf.FloorToInt((remaining - rareCount) * magicRatio);
        int normalCount = remaining - rareCount - magicCount;

        var list = new List<EnemyGrade>();
        for (int i = 0; i < normalCount; i++) list.Add(EnemyGrade.Normal);
        for (int i = 0; i < magicCount; i++) list.Add(EnemyGrade.Magic);
        for (int i = 0; i < rareCount; i++) list.Add(EnemyGrade.Rare);
        for (int i = 0; i < uniqueCount; i++) list.Add(EnemyGrade.Unique);

        // Fisher-Yates 셔플
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private static int GetEnemyCount(int stage)
    {
        if (stage <= 4) return 15;
        if (stage <= 8) return 20;
        if (stage <= 12) return 25;
        return 30;
    }

    // 진입부에서 방어됨 — SpawnEnemies 는 항상 routeCount >= 1 상태로 진입한다고 가정.
    // 여기서 다시 실패해 yield break 하면 _aliveCount 가 감소할 계기가 없어 웨이브가 stuck 이 되므로,
    // 진입부 (StartWave / StartRiftWave) 가 유일한 방어 지점.
    private bool HasSpawnRoutes()
    {
        if (MapTileSystem.Instance == null || MapTileSystem.Instance.RouteCount == 0)
        {
            Debug.LogError("[WaveSystem] spawnRoutes 미설정 — 웨이브 시작 취소");
            return false;
        }
        return true;
    }

    private IEnumerator SpawnEnemies(List<EnemyGrade> spawnList)
    {
        int routeCount = MapTileSystem.Instance.RouteCount;
        Debug.Log($"[WaveSystem] SpawnEnemies routes={routeCount} enemies={spawnList.Count}");

        int spawnedCount = 0;
        for (int i = 0; i < spawnList.Count; i++)
        {
            var grade = spawnList[i];
            var data = GetDataForGrade(grade);
            if (data == null)
            {
                Debug.LogError($"[WaveSystem] EnemyData null for grade={grade}");
                StopWave();
                yield break;
            }
            int routeIndex = i % routeCount;
            var path = PathfindingSystem.Instance.ComputePath(
                MapTileSystem.Instance.GetSpawnPoint(routeIndex),
                MapTileSystem.Instance.GetBasePoint(),
                includeStart: true);
            var enemy = ObjectPoolSystem.Instance.Get();
            enemy.Initialize(data, CurrentStage, path, routeIndex, _currentRiftMods);
            spawnedCount++;
            Debug.Log($"[WaveSystem] Spawned {spawnedCount}/{spawnList.Count} grade={grade} route={routeIndex}");

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private EnemyData GetDataForGrade(EnemyGrade grade) => grade switch
    {
        EnemyGrade.Magic => magicData,
        EnemyGrade.Rare => rareData,
        EnemyGrade.Unique => uniqueData,
        _ => normalData
    };

    // 기지 도달이 곧 플레이어 사망인 경우 Enemy.ReachBase()가 PlayerSystem.TakeDamage(먼저) →
    // OnPlayerDied → WaveSystem.HandlePlayerDied → StopWave()로 IsWaveActive를 이미 false로
    // 바꿔놓은 뒤 OnEnemyReachedBase가 invoke된다. 그래서 가드를 IsWaveActive가 아니라
    // _aliveCount로 걸어야 그 마지막 몬스터의 카운트/알림이 누락되지 않는다 (Codex 리뷰 지적).
    private void HandleEnemyRemoved(Enemy enemy)
    {
        if (_aliveCount <= 0) return;
        _aliveCount--;

        int routeIndex = enemy.RouteIndex;
        if (_aliveCountByRoute != null && routeIndex >= 0 && routeIndex < _aliveCountByRoute.Length)
        {
            _aliveCountByRoute[routeIndex]--;
            if (_aliveCountByRoute[routeIndex] <= 0)
                OnRouteCleared?.Invoke(routeIndex);
        }

        OnAliveCountChanged?.Invoke(_aliveCount, _totalCount);

        if (IsWaveActive && _aliveCount <= 0)
            EndWave();
    }

    private void HandlePlayerDied()
    {
        StopWave();
        GameStateSystem.SetState(GameState.Defeat);
    }

    private void EndWave()
    {
        if (GameStateSystem.Current != GameState.Playing) return;
        bool wasRift = IsRiftWaveActive;
        var riftMods = _currentRiftMods;
        IsWaveActive = false;
        IsRiftWaveActive = false;
        _currentRiftMods = RiftWaveModifiers.Default;

        bool cleared = PlayerSystem.Instance.CurrentHp > 0;
        if (cleared)
        {
            if (!wasRift && CurrentStage == UnlockedStage && UnlockedStage < 16)
                UnlockedStage++;

            if (wasRift)
                GrantRiftReward(riftMods);

            OnWaveEnded?.Invoke(true);

            // 균열 웨이브는 1회성이지만 일반 게임 흐름을 멈추지 않는다 — Playing 유지.
            // 일반 웨이브는 autoWave 면 자동 재시작, 아니면 결과 화면(WaveResult).
            if (wasRift)
            {
                // no-op — Playing 유지
            }
            else if (_autoWave)
            {
                StartWave();
            }
            else
            {
                GameStateSystem.SetState(GameState.WaveResult);
            }
        }
        else
        {
            OnWaveEnded?.Invoke(false);
        }
    }

    private void GrantRiftReward(RiftWaveModifiers mods)
    {
        if (CubeSystem.Instance == null) return;
        int reward = RiftRewardCalculator.CalculateCubeReward(riftBaseRewardCubes, mods.RewardCubeMult);
        if (reward <= 0) return;
        CubeSystem.Instance.Add(CubeType.Lower, reward);
        OnRiftRewardGranted?.Invoke(reward);
        Debug.Log($"[WaveSystem] 균열 보상 Lower 큐브 +{reward} (mult={mods.RewardCubeMult:F2})");
    }
}
