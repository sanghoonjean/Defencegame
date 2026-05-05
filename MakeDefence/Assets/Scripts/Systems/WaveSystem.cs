using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSystem : MonoBehaviour
{
    public static WaveSystem Instance { get; private set; }

    public static event Action<int> OnWaveStarted;
    public static event Action<bool> OnWaveEnded;  // true = 클리어

    public bool IsWaveActive { get; private set; }
    public int CurrentStage { get; private set; } = 1;
    public int UnlockedStage { get; private set; } = 1;

    // tech-debt: 스폰 간격 미확정 — Inspector에서 조정
    [SerializeField] private float spawnInterval = 1f;

    [SerializeField] private EnemyData normalData;
    [SerializeField] private EnemyData magicData;
    [SerializeField] private EnemyData rareData;
    [SerializeField] private EnemyData uniqueData;

    private bool _autoWave;
    private int _aliveCount;
    private Coroutine _spawnCoroutine;

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

    public void StartWave()
    {
        if (IsWaveActive) { Debug.Log("[WaveSystem] StartWave: already active"); return; }
        IsWaveActive = true;

        Debug.Log($"[WaveSystem] StartWave stage={CurrentStage}");

        if (normalData == null) Debug.LogError("[WaveSystem] normalData is NULL — Inspector에서 EnemyData 연결 필요");
        if (magicData == null)  Debug.LogError("[WaveSystem] magicData is NULL");
        if (rareData == null)   Debug.LogError("[WaveSystem] rareData is NULL");
        if (uniqueData == null) Debug.LogError("[WaveSystem] uniqueData is NULL");

        PlayerSystem.Instance.ResetHp();

        var spawnList = BuildSpawnList(CurrentStage);
        Debug.Log($"[WaveSystem] spawnList count={spawnList.Count}");
        _aliveCount = spawnList.Count;
        _spawnCoroutine = StartCoroutine(SpawnEnemies(spawnList));

        OnWaveStarted?.Invoke(CurrentStage);
    }

    public void StopWave()
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        IsWaveActive = false;
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

    private IEnumerator SpawnEnemies(List<EnemyGrade> spawnList)
    {
        var waypoints = MapTileSystem.Instance.GetFullPath();
        Debug.Log($"[WaveSystem] SpawnEnemies waypoints={waypoints.Length}");
        int spawnedCount = 0;
        foreach (var grade in spawnList)
        {
            var data = GetDataForGrade(grade);
            if (data == null) { Debug.LogError($"[WaveSystem] EnemyData null for grade={grade}"); yield break; }
            var enemy = ObjectPoolSystem.Instance.Get();
            enemy.Initialize(data, CurrentStage, waypoints);
            spawnedCount++;
            Debug.Log($"[WaveSystem] Spawned {spawnedCount}/{spawnList.Count} grade={grade}");
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

    private void HandleEnemyRemoved(Enemy _)
    {
        if (!IsWaveActive) return;
        _aliveCount--;
        if (_aliveCount <= 0)
            EndWave();
    }

    private void HandlePlayerDied()
    {
        StopWave();
        GameStateSystem.SetState(GameState.Defeat);
        OnWaveEnded?.Invoke(false);
    }

    private void EndWave()
    {
        if (GameStateSystem.Current != GameState.Playing) return;
        IsWaveActive = false;

        bool cleared = PlayerSystem.Instance.CurrentHp > 0;
        if (cleared)
        {
            if (CurrentStage == UnlockedStage && UnlockedStage < 16)
                UnlockedStage++;

            // TODO Phase 2: CubeSystem.DropReward(CurrentStage)
            OnWaveEnded?.Invoke(true);

            if (_autoWave)
                StartWave();
            else
                GameStateSystem.SetState(GameState.WaveResult);
        }
        else
        {
            OnWaveEnded?.Invoke(false);
        }
    }
}
