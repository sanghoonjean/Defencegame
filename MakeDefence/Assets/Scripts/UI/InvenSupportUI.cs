using UnityEngine;
using UnityEngine.UI;

public class InvenSupportUI : MonoBehaviour
{
    private struct SlotRef
    {
        public Image                      image;
        public Button                     button;
        public InvenSupportSlotDragHandler drag;
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

            foreach (var tmp in slot.GetComponentsInChildren<TMPro.TMP_Text>(true))
                tmp.gameObject.SetActive(false);

            var btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                var bgImage = slot.GetComponent<Image>();
                if (bgImage != null) bgImage.color = Color.clear;
                btn.targetGraphic = img;
            }

            var drag = slot.gameObject.GetComponent<InvenSupportSlotDragHandler>()
                    ?? slot.gameObject.AddComponent<InvenSupportSlotDragHandler>();
            drag.Init(img);

            list.Add(new SlotRef { image = img, button = btn, drag = drag });
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

            if (_slots[i].button == null) continue;
            _slots[i].button.onClick.RemoveAllListeners();
            if (hasOption)
            {
                var o = option;
                _slots[i].button.onClick.AddListener(() =>
                {
                    var tower = InventorySystem.Instance?.SelectedTower;
                    if (tower == null) return;
                    for (int slot = 0; slot < tower.UnlockedSupportSlots; slot++)
                    {
                        if (tower.SupportOptions[slot] != null) continue;
                        var existing = tower.SupportOptions[slot];
                        if (existing != null)
                            ShopSystem.Instance?.ReturnSupportOption(existing);
                        ShopSystem.Instance?.RemoveOwnedSupportOption(o);
                        InventorySystem.Instance.SetSupportOption(slot, o);
                        return;
                    }
                });
            }
        }
    }
}
