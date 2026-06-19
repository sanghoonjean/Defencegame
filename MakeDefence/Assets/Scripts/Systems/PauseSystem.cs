using System;
using UnityEngine;

/// <summary>
/// 게임 시뮬레이션 일시 정지 / 재개.
/// Time.timeScale 의 소유권 통합: 일시정지 아님 → GameSpeedSystem.Current, 일시정지 → 0.
/// 진입은 활성 웨이브 중에만 허용 (WaveSystem.StartWave 가 SetState 미호출 + SpawnEnemies 가 scaled WaitForSeconds 사용 → 비활성 시점 진입 시 스폰 코루틴 stall 위험).
/// 해제는 어떤 상태에서도 허용 — GameStateSystem.OnStateChanged 구독으로 모든 상태 전이 시 자동 해제.
/// </summary>
public class PauseSystem : MonoBehaviour
{
    public static PauseSystem Instance { get; private set; }

    public static event Action<bool> OnPauseChanged;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Instance = this;
        IsPaused = false;
        GameStateSystem.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        GameStateSystem.OnStateChanged -= HandleStateChanged;
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }

    private void OnApplicationQuit()
    {
        Time.timeScale = 1f;
    }

    public void Toggle() => Set(!IsPaused);

    public void Set(bool paused)
    {
        if (paused && (WaveSystem.Instance == null || !WaveSystem.Instance.IsWaveActive))
            return;

        IsPaused      = paused;
        Time.timeScale = paused ? 0f : (GameSpeedSystem.Instance?.Current ?? 1f);
        OnPauseChanged?.Invoke(paused);
    }

    private void HandleStateChanged(GameState state)
    {
        if (IsPaused) Set(false);
    }
}
