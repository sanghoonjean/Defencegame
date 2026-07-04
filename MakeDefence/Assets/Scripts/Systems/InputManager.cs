using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BuildMode { Tower, None }

/// <summary>
/// 좌클릭 입력의 단일 진입점.
/// UI 가드 → Tower/Rift hit → 빈 칸 배치 위임 순으로 분기한다.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public static event Action<BuildMode> OnBuildModeChanged;

    public BuildMode CurrentBuildMode { get; private set; } = BuildMode.None;

    private void Awake() { Instance = this; }

    public void SetBuildMode(BuildMode mode)
    {
        if (CurrentBuildMode == mode) return;
        CurrentBuildMode = mode;
        OnBuildModeChanged?.Invoke(mode);
        if (mode == BuildMode.Tower)
            TowerPlacer.Instance?.EnterPlacementMode();
        else
            TowerPlacer.Instance?.ExitPlacementMode();
    }

    private void Update()
    {
        HandleClick();

        if (Input.GetKeyDown(KeyCode.F))
        {
            GameSpeedSystem.Instance?.Cycle();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            PauseSystem.Instance?.Toggle();
        }
    }

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // 타워 배치 대기 모드: ghost 위치에 배치 시도 후 모드 종료
        if (TowerPlacer.Instance != null && TowerPlacer.Instance.IsPlacingTower)
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var c = new Vector2Int(Mathf.FloorToInt(wp.x), Mathf.FloorToInt(wp.y));
            TowerPlacer.Instance.TryPlace(c);
            TowerPlacer.Instance.ExitPlacementMode();
            return;
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y));
        if (hit != null)
        {
            var tower = hit.GetComponent<Tower>();
            if (tower != null && InventorySystem.Instance != null)
            {
                InventorySystem.Instance.SelectTower(tower);
                return;
            }
        }

        var coord = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        bool placed = CurrentBuildMode == BuildMode.Tower
            && TowerPlacer.Instance != null && TowerPlacer.Instance.TryPlace(coord);

        if (!placed)
            InventorySystem.Instance?.Deselect();
    }
}
