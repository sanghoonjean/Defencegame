using UnityEngine;
using UnityEngine.UI;

public class SupportInvenUI : MonoBehaviour
{
    private struct SlotRef
    {
        public Image                    image;
        public SupportOptionDragHandler drag;
    }

    private SlotRef[] _slots;

    private void Awake()
    {
        var list = new System.Collections.Generic.List<SlotRef>();
        foreach (Transform slot in transform)
        {
            var icon = slot.Find("ICON");
            if (icon == null) continue;
            var img = icon.GetComponent<Image>();
            if (img == null) continue;

            var drag = slot.gameObject.GetComponent<SupportOptionDragHandler>()
                    ?? slot.gameObject.AddComponent<SupportOptionDragHandler>();
            drag.Init(img);

            list.Add(new SlotRef { image = img, drag = drag });
        }
        _slots = list.ToArray();
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
        var owned = ShopSystem.Instance?.OwnedSupports;
        for (int i = 0; i < _slots.Length; i++)
        {
            bool hasOption = owned != null && i < owned.Count;
            var  option    = hasOption ? owned[i] : null;

            _slots[i].image.sprite = hasOption ? option.icon : null;
            _slots[i].image.color  = hasOption ? Color.white : Color.clear;
            _slots[i].drag.Option  = option;
        }
    }
}
