using UnityEngine;
using UnityEngine.EventSystems;

public class ShopDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        // 장착 슬롯에서 드랍
        var skillSlotDrag = eventData.pointerDrag.GetComponent<SkillSlotDragHandler>();
        if (skillSlotDrag != null)
        {
            SellEquippedSkill();
            return;
        }

        // 인벤토리 슬롯에서 드랍
        var invenDrag = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (invenDrag != null && invenDrag.Skill != null)
        {
            SellInventorySkill(invenDrag.Skill);
            return;
        }

        // 서포트 슬롯에서 드랍
        var supportDrag = eventData.pointerDrag.GetComponent<SupportOptionDragHandler>();
        if (supportDrag != null && supportDrag.Option != null)
        {
            SellSupportOption(supportDrag.Option);
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

    private void SellInventorySkill(SkillData skill)
    {
        if (SellConfirmPopup.Instance != null)
        {
            SellConfirmPopup.Instance.ShowInventorySell(skill);
        }
        else
        {
            if (ShopSystem.Instance.RemoveOwnedSkill(skill))
                CubeSystem.Instance?.Add(CubeType.Lower, 1);
        }
    }

    private void SellSupportOption(SupportOptionData option)
    {
        if (SellConfirmPopup.Instance != null)
        {
            SellConfirmPopup.Instance.ShowSupportSell(option);
        }
        else
        {
            // 인벤토리 우선, 없으면 장착 슬롯 탐색
            if (ShopSystem.Instance != null && ShopSystem.Instance.RemoveOwnedSupportOption(option))
            {
                CubeSystem.Instance?.Add(CubeType.Lower, 1);
                return;
            }

            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null) return;
            for (int i = 0; i < tower.UnlockedSupportSlots; i++)
            {
                if (tower.SupportOptions[i] != option) continue;
                InventorySystem.Instance.SetSupportOption(i, null);
                CubeSystem.Instance?.Add(CubeType.Lower, 1);
                return;
            }
        }
    }
}
