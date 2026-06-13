using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvenUI : MonoBehaviour
{
    [SerializeField] private GameObject _slotPrefab; // 선택: 동적 확장 모드용. null 이면 기존 자식 슬롯만 사용

    private struct SlotRef
    {
        public Image                image;
        public Button               button;
        public InvenSlotDragHandler drag;
    }

    private readonly List<SlotRef> _slots = new();

    private void Awake()
    {
        Debug.Log($"[InvenUI] Awake — 자식 수: {transform.childCount}, slotPrefab={(_slotPrefab != null ? "set" : "null")}");
        foreach (Transform child in transform)
        {
            // 슬롯 프리팹 본체는 자식으로 들어있을 수 있으므로 제외 (이름 기반 스킵은 생략 — _slotPrefab은 보통 씬 밖)
            TryRegisterSlot(child);
        }
        Debug.Log($"[InvenUI] Awake 완료 — 등록된 슬롯 수: {_slots.Count}");
    }

    private bool TryRegisterSlot(Transform slot)
    {
        var icon = slot.Find("ICON");
        if (icon == null) return false;
        var img = icon.GetComponent<Image>();
        if (img == null) return false;

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
        drag.SourceDisplayIndex = _slots.Count;

        _slots.Add(new SlotRef { image = img, button = btn, drag = drag });
        return true;
    }

    private void EnsureSlotCount(int needed)
    {
        if (_slotPrefab == null) return; // 동적 모드 비활성: 기존 슬롯 수 그대로
        while (_slots.Count < needed)
        {
            var newSlot = Instantiate(_slotPrefab, transform);
            if (!TryRegisterSlot(newSlot.transform))
            {
                Debug.LogWarning("[InvenUI] _slotPrefab 인스턴스 등록 실패 — ICON Image 누락?");
                Destroy(newSlot);
                break;
            }
        }
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
        var order = ShopSystem.Instance?.OwnedDisplayOrder;
        int count = order?.Count ?? 0;
        Debug.Log($"[InvenUI] Refresh — 슬롯 수: {_slots.Count}, displayOrder 수: {count}");

        EnsureSlotCount(count);

        for (int i = 0; i < _slots.Count; i++)
        {
            bool hasItem = i < count;
            var  item    = hasItem ? ShopSystem.Instance.GetDisplayItem(i) : default;

            _slots[i].drag.Skill              = hasItem ? item.Skill   : null;
            _slots[i].drag.Support            = hasItem ? item.Support : null;
            _slots[i].drag.SourceDisplayIndex = i;

            _slots[i].image.sprite = hasItem ? item.Icon : null;
            _slots[i].image.color  = hasItem ? Color.white : Color.clear;

            if (_slots[i].button == null) continue;
            _slots[i].button.onClick.RemoveAllListeners();

            // 클릭 장착: 스킬만 (서포트는 슬롯 인덱스 필요로 클릭 장착 미지원)
            if (hasItem && item.Kind == InventoryItemKind.Skill)
            {
                int    displayIdx = i;
                var    s          = item.Skill;
                _slots[i].button.onClick.AddListener(() =>
                {
                    if (InventorySystem.Instance?.SelectedTower == null) return;
                    var tower = InventorySystem.Instance.SelectedTower;
                    if (tower.EquippedSkill != null)
                        ShopSystem.Instance?.ReturnSkill(tower.EquippedSkill);
                    ShopSystem.Instance?.RemoveByDisplayIndex(displayIdx);
                    InventorySystem.Instance.EquipSkill(s);
                });
            }
        }
    }
}
