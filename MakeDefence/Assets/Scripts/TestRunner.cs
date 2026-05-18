using UnityEngine;
using UnityEngine.EventSystems;

// 개발 테스트용 — 빌드 전 삭제
public class TestRunner : MonoBehaviour
{
    [SerializeField] private SupportOptionData testSupportOption;

    private void Update()
    {
        HandleClick();

        // Space: 웨이브 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[TestRunner] Space pressed");
            if (WaveSystem.Instance == null)       { Debug.LogError("[TestRunner] WaveSystem.Instance is NULL");       return; }
            if (PlayerSystem.Instance == null)     { Debug.LogError("[TestRunner] PlayerSystem.Instance is NULL");     return; }
            if (MapTileSystem.Instance == null)    { Debug.LogError("[TestRunner] MapTileSystem.Instance is NULL");    return; }
            if (ObjectPoolSystem.Instance == null) { Debug.LogError("[TestRunner] ObjectPoolSystem.Instance is NULL"); return; }
            WaveSystem.Instance.StartWave();
        }

        // A: 자동 웨이브 ON + 미진행 시 즉시 시작
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (WaveSystem.Instance == null)       { Debug.LogError("[TestRunner] WaveSystem.Instance is NULL");       return; }
            if (PlayerSystem.Instance == null)     { Debug.LogError("[TestRunner] PlayerSystem.Instance is NULL");     return; }
            if (MapTileSystem.Instance == null)    { Debug.LogError("[TestRunner] MapTileSystem.Instance is NULL");    return; }
            if (ObjectPoolSystem.Instance == null) { Debug.LogError("[TestRunner] ObjectPoolSystem.Instance is NULL"); return; }
            WaveSystem.Instance.SetAutoWave(true);
            if (!WaveSystem.Instance.IsWaveActive)
                WaveSystem.Instance.StartWave();
        }

        // F: 선택 타워에 테스트 보조 옵션 장착 (디버그)
        if (Input.GetKeyDown(KeyCode.F))
        {
            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null) { Debug.LogError("[TestRunner] 타워를 먼저 선택하세요"); return; }
            if (testSupportOption == null) { Debug.LogError("[TestRunner] TestRunner에 SupportOptionData 에셋을 연결하세요"); return; }
            tower.UnlockSupportSlot();
            bool ok = tower.SetSupportOption(0, testSupportOption);
            Debug.Log($"[TestRunner] F키 — 보조 옵션 장착 {(ok ? "성공" : "실패")}: {testSupportOption.displayName}, AddedFireRatio={tower.AddedFireRatio}");
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

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (overUI) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y));
        if (hit != null)
        {
            var tower = hit.GetComponent<Tower>();
            if (tower != null && InventorySystem.Instance != null)
            {
                InventorySystem.Instance.SelectTower(tower);
                Debug.Log($"[TestRunner] 타워 선택: {tower.TileCoord}");
                return;
            }
        }

        InventorySystem.Instance?.Deselect();
    }

    private void OnGUI()
    {
        if (WaveSystem.Instance == null || PlayerSystem.Instance == null) return;

        GUI.Label(new Rect(10, 10, 300, 25), $"Stage: {WaveSystem.Instance.CurrentStage}");
        GUI.Label(new Rect(10, 35, 300, 25), $"Wave Active: {WaveSystem.Instance.IsWaveActive}");
        GUI.Label(new Rect(10, 60, 300, 25), $"Player HP: {PlayerSystem.Instance.CurrentHp}");
        GUI.Label(new Rect(10, 85, 300, 25), $"Game State: {GameStateSystem.Current}");
        string selected = InventorySystem.Instance?.SelectedTower != null
            ? $"{InventorySystem.Instance.SelectedTower.TileCoord}"
            : "없음";
        GUI.Label(new Rect(10, 110, 300, 25), $"선택 타워: {selected}");
        string fireRatio = InventorySystem.Instance?.SelectedTower != null
            ? $"{InventorySystem.Instance.SelectedTower.AddedFireRatio * 100f:F0}%"
            : "-";
        GUI.Label(new Rect(10, 135, 400, 25), $"화염 피해 비율: {fireRatio}");
        GUI.Label(new Rect(10, 160, 400, 25), "[Space] 웨이브  [A] 자동  [C] 큐브+10  [R] 리셋  [F] 보조옵션장착");
    }
}
