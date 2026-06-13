using UnityEngine;
using UnityEngine.EventSystems;

public class ShopDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        // 장착 스킬 슬롯에서 드랍 → 장착 스킬 판매
        var skillSlotDrag = eventData.pointerDrag.GetComponent<SkillSlotDragHandler>();
        if (skillSlotDrag != null)
        {
            SellEquippedSkill();
            return;
        }

        // 통합 핸들러: 인벤 스킬 / 인벤 서포트 / 장착 서포트
        var drag = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (drag == null) return;

        if (drag.Skill != null)
        {
            SellInventorySkill(drag.Skill, drag.SourceDisplayIndex);
            return;
        }

        if (drag.Support != null)
        {
            var equipSlot = eventData.pointerDrag.GetComponent<SupportSlotUI>();
            int slotIdx   = equipSlot != null ? equipSlot.SlotIndex : -1;
            SellSupportOption(drag.Support, slotIdx, drag.SourceDisplayIndex);
        }
    }

    private void SellEquippedSkill()
    {
        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null || tower.EquippedSkill == null) return;

        if (SellConfirmPopup.Instance != null)
        {
            SellConfirmPopup.Instance.Show(tower, tower.EquippedSkill);
        }
        else
        {
            InventorySystem.Instance.UnequipSkill();
            CubeSystem.Instance?.Add(CubeType.Lower, 1);
        }
    }

    private void SellInventorySkill(SkillData skill, int sourceDisplayIdx)
    {
        if (SellConfirmPopup.Instance != null)
        {
            SellConfirmPopup.Instance.ShowInventorySell(skill, sourceDisplayIdx);
        }
        else
        {
            bool removed = sourceDisplayIdx >= 0
                ? ShopSystem.Instance.RemoveByDisplayIndex(sourceDisplayIdx)
                : ShopSystem.Instance.RemoveOwnedSkill(skill);
            if (removed) CubeSystem.Instance?.Add(CubeType.Lower, 1);
        }
    }

    private void SellSupportOption(SupportOptionData option, int equippedSlotIndex, int sourceDisplayIdx)
    {
        if (SellConfirmPopup.Instance != null)
        {
            SellConfirmPopup.Instance.ShowSupportSell(option, equippedSlotIndex, sourceDisplayIdx);
        }
        else if (equippedSlotIndex >= 0)
        {
            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null) return;
            if (tower.SupportOptions[equippedSlotIndex] != option) return;
            InventorySystem.Instance.SetSupportOption(equippedSlotIndex, null);
            CubeSystem.Instance?.Add(CubeType.Lower, 1);
        }
        else if (ShopSystem.Instance != null)
        {
            bool removed = sourceDisplayIdx >= 0
                ? ShopSystem.Instance.RemoveByDisplayIndex(sourceDisplayIdx)
                : ShopSystem.Instance.RemoveOwnedSupportOption(option);
            if (removed) CubeSystem.Instance?.Add(CubeType.Lower, 1);
        }
    }
}
