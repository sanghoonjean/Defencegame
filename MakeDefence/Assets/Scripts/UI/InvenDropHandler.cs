using UnityEngine;
using UnityEngine.EventSystems;

public class InvenDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        // GenerateSlot (장착된 stone) → 인벤: 회수
        var generate = eventData.pointerDrag.GetComponent<GenerateSlotDropTarget>();
        if (generate != null)
        {
            InventorySystem.TryUnloadStone(generate);
            return;
        }

        // 스킬 장착 슬롯 → 인벤: 스킬 해제 + 인벤 반환
        if (eventData.pointerDrag.GetComponent<SkillSlotDragHandler>() != null)
        {
            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null || tower.EquippedSkill == null) return;

            var skill = tower.EquippedSkill;
            InventorySystem.Instance.UnequipSkill();
            ShopSystem.Instance?.ReturnSkill(skill);
            return;
        }

        // 서포트 장착 슬롯 → 인벤: 해제 후 인벤 반환
        var drag = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (drag == null || drag.Support == null) return; // Skill 페이로드는 SkillSlotDragHandler 경로

        // 인벤 → 인벤 (장착 슬롯 출처 아님) 은 InvenSlotDragHandler 자체의 swap 으로 처리됨 — 여기는 무시
        var sourceSlot = eventData.pointerDrag.GetComponent<SupportSlotUI>();
        if (sourceSlot == null) return;

        var tower2 = InventorySystem.Instance?.SelectedTower;
        if (tower2 == null) return;

        int slotIdx = sourceSlot.SlotIndex;
        if (slotIdx < 0 || slotIdx >= tower2.UnlockedSupportSlots) return;

        var option = drag.Support;
        if (tower2.SupportOptions[slotIdx] != option) return;

        InventorySystem.Instance.SetSupportOption(slotIdx, null);
        ShopSystem.Instance?.ReturnSupportOption(option);
    }
}
