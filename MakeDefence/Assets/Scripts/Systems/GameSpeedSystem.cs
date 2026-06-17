using System;
using UnityEngine;

/// <summary>
/// 게임 진행 속도 (1x / 2x / 3x) 토글 + 단축키.
/// Time.timeScale 을 직접 조정해 시뮬레이션 코드 (Tower / Enemy / Projectile / WaveSystem 등) 전체를 일괄 가속한다.
/// GameStateSystem.OnStateChanged 구독 — 모든 상태 전이에서 1x 로 복귀 (리셋/사망/승리/웨이브 종료 공통).
/// </summary>
public class GameSpeedSystem : MonoBehaviour
{
    public static GameSpeedSystem Instance { get; private set; }

    public static event Action<float> OnSpeedChanged;

    private static readonly float[] STEPS = { 1f, 2f, 3f };

    public float Current { get; private set; } = 1f;

    private void Awake()
    {
        Instance = this;
        Set(1f);
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

    public void Cycle()
    {
        int idx = Array.IndexOf(STEPS, Current);
        int next = (idx + 1) % STEPS.Length;
        if (idx < 0) next = 0;
        Set(STEPS[next]);
    }

    public void Set(float speed)
    {
        Current        = speed;
        Time.timeScale = speed;
        OnSpeedChanged?.Invoke(speed);
    }

    private void HandleStateChanged(GameState state)
    {
        Set(1f);
    }
}
