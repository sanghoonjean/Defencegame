using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 1칸. 클릭 시 SelectedRift 가 있으면 그 균열에 장착.
/// 기존 LoadedStone 이 있으면 swap (회수 후 새 stone 장착) — Codex P2 반영.
/// </summary>
[RequireComponent(typeof(Button))]
public class DimensionStoneSlot : MonoBehaviour
{
    private Button _button;
    private DimensionStone _bound;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    public void Bind(DimensionStone stone)
    {
        _bound = stone;
    }

    private void OnClick()
    {
        if (_bound == null) return;
        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null) return;
        if (DimensionStoneInventory.Instance == null) return;

        // swap 패턴 — 기존 stone 인벤 회수 후 새 stone 장착 (소실 방지)
        if (rift.LoadedStone != null)
        {
            DimensionStoneInventory.Instance.Add(rift.LoadedStone);
            rift.ClearStone();
        }
        DimensionStoneInventory.Instance.Remove(_bound);
        rift.SetStone(_bound);
    }
}
