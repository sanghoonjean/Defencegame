using UnityEngine;
using UnityEngine.UI;

public class InvenUI : MonoBehaviour
{
    private struct SlotRef
    {
        public Image  image;
        public Button button;
    }

    private SlotRef[] _slots;

    private void Awake()
    {
        var list = new System.Collections.Generic.List<SlotRef>();
        Debug.Log($"[InvenUI] Awake — 자식 수: {transform.childCount}");
        foreach (Transform slot in transform)
        {
            var itemImage = slot.Find("ICON");
            if (itemImage == null)
            {
                Debug.LogWarning($"[InvenUI] '{slot.name}' — ICON 자식 없음");
                continue;
            }
            var img = itemImage.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogWarning($"[InvenUI] '{slot.name}/ItemImage' — Image 컴포넌트 없음");
                continue;
            }
            list.Add(new SlotRef
            {
                image  = img,
                button = slot.GetComponent<Button>()
            });
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

            _slots[i].image.sprite = hasSkill ? owned[i].icon : null;
            _slots[i].image.color  = hasSkill ? Color.white : Color.clear;

            if (hasSkill)
                Debug.Log($"[InvenUI] 슬롯[{i}] — sprite={owned[i].icon?.name ?? "null"}, skillType={owned[i].skillType}");

            if (_slots[i].button == null) continue;
            _slots[i].button.onClick.RemoveAllListeners();
            if (hasSkill)
            {
                var skill = owned[i];
                _slots[i].button.onClick.AddListener(
                    () => InventorySystem.Instance?.EquipSkill(skill));
            }
        }
    }
}
