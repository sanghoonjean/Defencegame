using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 차원석 인벤토리 패널에 부착하는 드롭 존.
/// GenerateSlot 에서 시작된 드래그를 받으면 SelectedRift 의 stone 을 인벤으로 회수한다.
///
/// 인벤 슬롯에는 별도 IDropHandler 가 없으므로, 슬롯 위에서 떨어뜨려도 이벤트가
/// 패널까지 bubble 되어 여기서 잡힌다. GenerateSlot 자체에는 GenerateSlotDropTarget 의
/// IDropHandler 가 있어 그 위에 떨어뜨리면 unload 되지 않는다 (의도).
///
/// 이 GO 에는 raycastTarget = true 인 Graphic 이 있어야 OnDrop 이 호출된다.
/// </summary>
public class DimensionStoneInventoryDropTarget : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var source = eventData.pointerDrag.GetComponent<GenerateSlotDropTarget>();
        if (source == null) return;

        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null || rift.LoadedStone == null) return;

        // GenerateSlot drag 의 캐시한 stone 이 현재 LoadedStone 과 일치할 때만 회수.
        // (드래그 중 다른 경로로 stone 이 바뀌었을 race 회피)
        if (source.DraggingStone != null && source.DraggingStone != rift.LoadedStone) return;

        var stone = rift.LoadedStone;
        DimensionStoneInventory.Instance?.Add(stone);
        rift.ClearStone();
    }
}
