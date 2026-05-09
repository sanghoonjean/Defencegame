using UnityEngine;

// 개발 테스트용 — 빌드 전 삭제
public class TestRunner : MonoBehaviour
{
    [SerializeField] private SkillData fireballSkill;
    [SerializeField] private SkillData preciseArrowSkill;

    private SkillData _selectedSkill;
    private bool _pointerOverGUI;

    private void Awake()
    {
        _selectedSkill = fireballSkill;
    }

    private static bool IsMouseOverGUIRect(Rect guiRect)
    {
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return guiRect.Contains(mouse);
    }

    private void Update()
    {
        // 마우스 좌클릭: 타워 선택 (GUI 버튼 영역 클릭 시 제외)
        if (Input.GetMouseButtonDown(0) && !_pointerOverGUI)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y));
            if (hit != null)
            {
                var tower = hit.GetComponent<Tower>();
                if (tower != null && InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.SelectTower(tower);
                    Debug.Log($"[TestRunner] 타워 선택: {tower.TileCoord}");
                }
            }
        }

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

        // T: 선택된 타워에 선택된 스킬 장착
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (InventorySystem.Instance == null) { Debug.LogError("[TestRunner] InventorySystem.Instance is NULL"); return; }
            if (_selectedSkill == null) { Debug.LogError("[TestRunner] 선택된 스킬이 없음 — Inspector에서 스킬 연결 확인"); return; }
            if (InventorySystem.Instance.SelectedTower == null) { Debug.LogWarning("[TestRunner] 선택된 타워 없음 — 타워를 먼저 클릭하세요"); return; }
            InventorySystem.Instance.EquipSkill(_selectedSkill);
            Debug.Log($"[TestRunner] T키 — {_selectedSkill.name} 장착 완료");
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
        // 버튼 영역(x:10~230, y:185~225) 위에 마우스가 있으면 타워 선택 차단
        _pointerOverGUI = IsMouseOverGUIRect(new Rect(10, 185, 220, 40));

        if (WaveSystem.Instance == null || PlayerSystem.Instance == null) return;

        GUI.Label(new Rect(10, 10, 300, 25), $"Stage: {WaveSystem.Instance.CurrentStage}");
        GUI.Label(new Rect(10, 35, 300, 25), $"Wave Active: {WaveSystem.Instance.IsWaveActive}");
        GUI.Label(new Rect(10, 60, 300, 25), $"Player HP: {PlayerSystem.Instance.CurrentHp}");
        GUI.Label(new Rect(10, 85, 300, 25), $"Game State: {GameStateSystem.Current}");
        string selected = InventorySystem.Instance?.SelectedTower != null
            ? $"{InventorySystem.Instance.SelectedTower.TileCoord}"
            : "없음";
        GUI.Label(new Rect(10, 110, 300, 25), $"선택 타워: {selected}");
        string skillName = _selectedSkill != null ? _selectedSkill.name : "없음";
        GUI.Label(new Rect(10, 135, 300, 25), $"선택 스킬: {skillName}");
        GUI.Label(new Rect(10, 160, 400, 25), "[Space] 웨이브  [A] 자동  [T] 스킬장착  [C] 큐브+10  [R] 리셋");

        // 스킬 선택 버튼
        if (GUI.Button(new Rect(10, 190, 100, 30), "Fireball"))
        {
            if (fireballSkill != null) _selectedSkill = fireballSkill;
            else Debug.LogError("[TestRunner] fireballSkill이 Inspector에 연결되지 않음");
        }
        GUI.enabled = preciseArrowSkill != null;
        if (GUI.Button(new Rect(120, 190, 100, 30), "정밀 화살"))
        {
            _selectedSkill = preciseArrowSkill;
            Debug.Log("[TestRunner] 스킬 선택: 정밀 화살");
        }
        GUI.enabled = true;
    }
}
