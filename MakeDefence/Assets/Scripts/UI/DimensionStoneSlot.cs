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
        // 클릭한 stone 을 local 로 캐싱 — Add/Remove 가 OnInventoryChanged 를 발행하면
        // DimensionStoneInventoryView.Rebuild 가 같은 슬롯에 다른 stone 을 Bind 할 수 있어
        // _bound 가 바뀐 채로 SetStone 이 잘못된 stone 을 장착하는 race 회피 (Codex P1).
        var clicked = _bound;
        if (clicked == null) return;
        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null) return;
        if (DimensionStoneInventory.Instance == null) return;

        // swap 패턴 — 기존 stone 인벤 회수 후 새 stone 장착 (소실 방지)
        if (rift.LoadedStone != null)
        {
            DimensionStoneInventory.Instance.Add(rift.LoadedStone);
            rift.ClearStone();
        }
        DimensionStoneInventory.Instance.Remove(clicked);
        rift.SetStone(clicked);
    }
}
