using UnityEngine;

// 개발 테스트용 — 빌드 전 삭제
public class TestRunner : MonoBehaviour
{
    private void Update()
    {
        // Space: 웨이브 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[TestRunner] Space pressed");
            if (WaveSystem.Instance == null) { Debug.LogError("[TestRunner] WaveSystem.Instance is NULL"); return; }
            if (PlayerSystem.Instance == null) { Debug.LogError("[TestRunner] PlayerSystem.Instance is NULL"); return; }
            if (MapTileSystem.Instance == null) { Debug.LogError("[TestRunner] MapTileSystem.Instance is NULL"); return; }
            if (ObjectPoolSystem.Instance == null) { Debug.LogError("[TestRunner] ObjectPoolSystem.Instance is NULL"); return; }
            WaveSystem.Instance.StartWave();
        }

        // A: 자동 웨이브 ON + 미진행 시 즉시 시작
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (WaveSystem.Instance == null)    { Debug.LogError("[TestRunner] WaveSystem.Instance is NULL");    return; }
            if (PlayerSystem.Instance == null)  { Debug.LogError("[TestRunner] PlayerSystem.Instance is NULL");  return; }
            if (MapTileSystem.Instance == null) { Debug.LogError("[TestRunner] MapTileSystem.Instance is NULL"); return; }
            if (ObjectPoolSystem.Instance == null) { Debug.LogError("[TestRunner] ObjectPoolSystem.Instance is NULL"); return; }
            WaveSystem.Instance.SetAutoWave(true);
            if (!WaveSystem.Instance.IsWaveActive)
                WaveSystem.Instance.StartWave();
        }

        // C: Lower 큐브 10개 지급 (디버그)
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (CubeSystem.Instance == null) { Debug.LogError("[TestRunner] CubeSystem.Instance is NULL"); return; }
            CubeSystem.Instance.Add(CubeType.Lower, 10);
            Debug.Log($"[TestRunner] C키 — Lower 큐브 +10 (현재: {CubeSystem.Instance.GetCount(CubeType.Lower)})");
        }

        // R: 완전 리셋 (웨이브 중지 + 적 제거 + HP 초기화 + 상태 복귀)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (WaveSystem.Instance != null) WaveSystem.Instance.StopWave();
            if (ObjectPoolSystem.Instance != null)
            {
                foreach (var e in Enemy.ActiveEnemies.ToArray())
                    ObjectPoolSystem.Instance.Return(e);
            }
            if (PlayerSystem.Instance != null) PlayerSystem.Instance.ResetHp();
            GameStateSystem.ResetToPlaying();
        }
    }

    private void OnGUI()
    {
        if (WaveSystem.Instance == null || PlayerSystem.Instance == null) return;

        GUI.Label(new Rect(10, 10, 300, 25), $"Stage: {WaveSystem.Instance.CurrentStage}");
        GUI.Label(new Rect(10, 35, 300, 25), $"Wave Active: {WaveSystem.Instance.IsWaveActive}");
        GUI.Label(new Rect(10, 60, 300, 25), $"Player HP: {PlayerSystem.Instance.CurrentHp}");
        GUI.Label(new Rect(10, 85, 300, 25), $"Game State: {GameStateSystem.Current}");
        GUI.Label(new Rect(10, 120, 300, 25), "[Space] 웨이브 시작  [A] 자동웨이브  [C] 큐브+10  [R] 리셋");
    }
}
