using UnityEngine;
using UnityEngine.UI;

public class InvenUI : MonoBehaviour
{
    private struct SlotRef
    {
        public Image                image;
        public Button               button;
        public InvenSlotDragHandler drag;
    }

    private SlotRef[] _slots;

    private void Awake()
    {
        var list = new System.Collections.Generic.List<SlotRef>();
        Debug.Log($"[InvenUI] Awake — 자식 수: {transform.childCount}");
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

            var drag = slot.gameObject.GetComponent<InvenSlotDragHandler>()
                    ?? slot.gameObject.AddComponent<InvenSlotDragHandler>();
            drag.Init(img);

            list.Add(new SlotRef { image = img, button = btn, drag = drag });
        }
        _slots = list.ToArray();
        Debug.Log($"[InvenUI] Awake 완료 — 등록된 슬롯 수: {_slots.Length}");
    }

    private void OnEnable()
    {
        Debug.Log($"[InvenUI] OnEnable — ShopSystem={ShopSystem.Instance != null}");
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
        Debug.Log($"[InvenUI] Refresh — 슬롯 수: {_slots.Length}, 보유 스킬 수: {owned?.Count ?? -1}");
        for (int i = 0; i < _slots.Length; i++)
        {
            bool hasSkill = owned != null && i < owned.Count;
            var  skill    = hasSkill ? owned[i] : null;

            _slots[i].image.sprite = hasSkill ? skill.icon : null;
            _slots[i].image.color  = hasSkill ? Color.white : Color.clear;
            _slots[i].drag.Skill   = skill;

            if (hasSkill)
                Debug.Log($"[InvenUI] 슬롯[{i}] — sprite={owned[i].icon?.name ?? "null"}, skillType={owned[i].skillType}");

            if (_slots[i].button == null) continue;
            _slots[i].button.onClick.RemoveAllListeners();
            if (hasSkill)
            {
                var s = skill;
                _slots[i].button.onClick.AddListener(() =>
                {
                    if (InventorySystem.Instance?.SelectedTower == null) return;
                    var tower = InventorySystem.Instance.SelectedTower;
                    if (tower.EquippedSkill != null)
                        ShopSystem.Instance?.ReturnSkill(tower.EquippedSkill);
                    ShopSystem.Instance?.RemoveOwnedSkill(s);
                    InventorySystem.Instance.EquipSkill(s);
                });
            }
        }
    }
}
