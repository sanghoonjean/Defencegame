using UnityEngine;
using UnityEngine.EventSystems;

public class InvenDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        // 스킬 장착 슬롯 → 인벤토리: 스킬 해제
        if (eventData.pointerDrag.GetComponent<SkillSlotDragHandler>() != null)
        {
            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null || tower.EquippedSkill == null) return;

            var skill = tower.EquippedSkill;
            InventorySystem.Instance.UnequipSkill();
            ShopSystem.Instance?.ReturnSkill(skill);
            return;
        }

        // 서포트 장착 슬롯 → 인벤토리: 서포트 해제 후 인벤토리로 반환
        var supportDrag = eventData.pointerDrag.GetComponent<SupportOptionDragHandler>();
        if (supportDrag != null && supportDrag.Option != null)
        {
            var sourceSlot = eventData.pointerDrag.GetComponent<SupportSlotUI>();
            if (sourceSlot == null) return; // 인벤토리 → 인벤토리는 무시

            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null) return;

            int slotIdx = sourceSlot.SlotIndex;
            if (slotIdx < 0 || slotIdx >= tower.UnlockedSupportSlots) return;
            if (tower.SupportOptions[slotIdx] != supportDrag.Option) return;

            InventorySystem.Instance.SetSupportOption(slotIdx, null);
            ShopSystem.Instance?.ReturnSupportOption(supportDrag.Option);
        }
    }
}
