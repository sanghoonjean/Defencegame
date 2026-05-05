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

        // A: 자동 웨이브 토글
        if (Input.GetKeyDown(KeyCode.A))
            WaveSystem.Instance.SetAutoWave(true);

        // R: 게임 상태 리셋
        if (Input.GetKeyDown(KeyCode.R))
            GameStateSystem.ResetToPlaying();
    }

    private void OnGUI()
    {
        if (WaveSystem.Instance == null || PlayerSystem.Instance == null) return;

        GUI.Label(new Rect(10, 10, 300, 25), $"Stage: {WaveSystem.Instance.CurrentStage}");
        GUI.Label(new Rect(10, 35, 300, 25), $"Wave Active: {WaveSystem.Instance.IsWaveActive}");
        GUI.Label(new Rect(10, 60, 300, 25), $"Player HP: {PlayerSystem.Instance.CurrentHp}");
        GUI.Label(new Rect(10, 85, 300, 25), $"Game State: {GameStateSystem.Current}");
        GUI.Label(new Rect(10, 120, 300, 25), "[Space] 웨이브 시작  [A] 자동웨이브  [R] 리셋");
    }
}
