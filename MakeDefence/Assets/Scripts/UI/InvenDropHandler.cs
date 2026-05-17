using UnityEngine;
using UnityEngine.EventSystems;

public class InvenDropHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.GetComponent<SkillSlotDragHandler>() == null) return;

        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null || tower.EquippedSkill == null) return;

        var skill = tower.EquippedSkill;
        InventorySystem.Instance.UnequipSkill();
        ShopSystem.Instance?.ReturnSkill(skill);
    }
}
