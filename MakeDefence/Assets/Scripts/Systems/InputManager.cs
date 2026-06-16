using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 좌클릭 입력의 단일 진입점.
/// UI 가드 → Tower hit → 빈 칸 배치 위임 순으로 분기한다.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private void Awake() { Instance = this; }

    private void Update()
    {
        HandleClick();
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
        }

        var coord = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        TowerPlacer.Instance?.TryPlace(coord);
        InventorySystem.Instance?.Deselect();
    }
}
