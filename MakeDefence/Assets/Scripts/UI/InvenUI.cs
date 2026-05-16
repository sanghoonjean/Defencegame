using UnityEngine;
using UnityEngine.UI;

public class InvenUI : MonoBehaviour
{
    private Image[] _slotImages;

    private void Awake()
    {
        var slots = new System.Collections.Generic.List<Image>();
        foreach (Transform slot in transform)
        {
            var itemImage = slot.Find("ItemImage");
            if (itemImage != null)
            {
                var img = itemImage.GetComponent<Image>();
                if (img != null) slots.Add(img);
            }
        }
        _slotImages = slots.ToArray();
    }

    private void OnEnable()
    {
        ShopSystem.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        ShopSystem.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        var owned = ShopSystem.Instance?.OwnedSkills;
        for (int i = 0; i < _slotImages.Length; i++)
        {
            bool hasSkill = owned != null && i < owned.Count;
            _slotImages[i].sprite = hasSkill ? owned[i].icon : null;
            _slotImages[i].color  = hasSkill ? Color.white : Color.clear;
        }
    }
}
