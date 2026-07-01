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

        // C: 모든 재화 10개 지급 (디버그)
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (CubeSystem.Instance == null) { Debug.LogError("[TestRunner] CubeSystem.Instance is NULL"); return; }
            CubeSystem.Instance.Add(CubeType.Lower,   10);
            CubeSystem.Instance.Add(CubeType.Upper,   10);
            CubeSystem.Instance.Add(CubeType.TopTier, 10);
            CubeSystem.Instance.Add(CubeType.Delete,  10);
            CubeSystem.Instance.Add(CubeType.Clone,   10);
            Debug.Log("[TestRunner] C키 — 모든 재화 +10");
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

        // B: BuildMode 토글 (Tower ↔ Rift)
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (InputManager.Instance == null) return;
            var next = InputManager.Instance.CurrentBuildMode == BuildMode.Tower
                ? BuildMode.Rift : BuildMode.Tower;
            InputManager.Instance.SetBuildMode(next);
            Debug.Log($"[TestRunner] BuildMode → {next}");
        }

        // O: 선택된 Rift 에 인벤 첫 차원석 자동 장착 + OpenRift
        if (Input.GetKeyDown(KeyCode.O))
        {
            var rift = InventorySystem.Instance?.SelectedRift;
            if (rift == null) { Debug.Log("[TestRunner] O: SelectedRift 없음"); return; }
            if (rift.LoadedStone == null && ShopSystem.Instance != null && ShopSystem.Instance.OwnedStones.Count > 0)
            {
                var stone = ShopSystem.Instance.OwnedStones[0];
                ShopSystem.Instance.RemoveStone(stone);
                rift.SetStone(stone);
                Debug.Log("[TestRunner] O: 차원석 자동 장착");
            }
            bool opened = rift.OpenRift();
            Debug.Log($"[TestRunner] O: OpenRift → {opened}");
        }

        // 1~5: 선택된 Rift 에 큐브 적용
        TryApplyCubeKey(KeyCode.Alpha1, CubeType.Lower);
        TryApplyCubeKey(KeyCode.Alpha2, CubeType.Upper);
        TryApplyCubeKey(KeyCode.Alpha3, CubeType.TopTier);
        TryApplyCubeKey(KeyCode.Alpha4, CubeType.Delete);
        TryApplyCubeKey(KeyCode.Alpha5, CubeType.Clone);
    }

    private void TryApplyCubeKey(KeyCode key, CubeType cube)
    {
        if (!Input.GetKeyDown(key)) return;
        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null) return;
        bool ok = rift.ApplyCube(cube);
        Debug.Log($"[TestRunner] {cube} → {ok}");
    }
}
