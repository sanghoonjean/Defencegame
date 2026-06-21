using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BuildMode { Tower, Rift }

/// <summary>
/// 좌클릭 입력의 단일 진입점.
/// UI 가드 → Tower/Rift hit → 빈 칸 배치 위임 순으로 분기한다.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public static event Action<BuildMode> OnBuildModeChanged;

    public BuildMode CurrentBuildMode { get; private set; } = BuildMode.Tower;

    private void Awake() { Instance = this; }

    public void SetBuildMode(BuildMode mode)
    {
        if (CurrentBuildMode == mode) return;
        CurrentBuildMode = mode;
        OnBuildModeChanged?.Invoke(mode);
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

            var rift = hit.GetComponent<RiftGenerator>();
            if (rift != null && InventorySystem.Instance != null)
            {
                // 같은 균열을 다시 클릭하면 토글로 닫는다
                if (InventorySystem.Instance.SelectedRift == rift)
                    InventorySystem.Instance.Deselect();
                else
                    InventorySystem.Instance.SelectRift(rift);
                return;
            }
        }

        var coord = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        bool placed = CurrentBuildMode == BuildMode.Rift
            ? (RiftGeneratorPlacer.Instance != null && RiftGeneratorPlacer.Instance.TryPlace(coord))
            : (TowerPlacer.Instance        != null && TowerPlacer.Instance.TryPlace(coord));

        if (!placed)
            InventorySystem.Instance?.Deselect();
    }
}
