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

        InventorySystem.Instance.UnequipSkill();
        CubeSystem.Instance?.Add(CubeType.Lower, 1);
    }
}
