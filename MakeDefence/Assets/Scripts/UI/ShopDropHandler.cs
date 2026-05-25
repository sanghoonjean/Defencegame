using UnityEngine;
using UnityEngine.EventSystems;

public class ShopDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.GetComponent<SkillSlotDragHandler>() == null) return;

        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null || tower.EquippedSkill == null) return;

        var skill = tower.EquippedSkill;

        if (SellConfirmPopup.Instance != null)
        {
            SellConfirmPopup.Instance.Show(tower, skill);
        }
        else
        {
            // 팝업 미설치 시 즉시 판매 (폴백)
            tower.UnequipSkill();
            CubeSystem.Instance?.Add(CubeType.Lower, 1);
        }
    }
}
