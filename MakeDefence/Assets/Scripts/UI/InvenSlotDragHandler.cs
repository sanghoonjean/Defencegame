using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvenSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public static event Action OnSkillDragStarted;
    public static event Action OnSkillDragEnded;

    public SkillData         Skill              { get; set; }
    public SupportOptionData Support            { get; set; }
    public DimensionStone    Stone              { get; set; }
    public Sprite            StoneIconOverride  { get; set; }

    // SourceDisplayIndex: 인벤 그리드에서의 슬롯 위치. 인벤 외부 (장착 슬롯 등) 에서 시작한 드래그는 -1
    public int SourceDisplayIndex { get; set; } = -1;
    // legacy alias — 기존 호출자 호환
    public int SlotIndex { get => SourceDisplayIndex; set => SourceDisplayIndex = value; }

    public InventoryItemKind Kind =>
        Skill   != null ? InventoryItemKind.Skill   :
        Support != null ? InventoryItemKind.Support :
                          InventoryItemKind.Stone;
    public bool   HasItem => Skill != null || Support != null || Stone != null;
    public Sprite Icon    =>
        Skill   != null ? Skill.icon   :
        Support != null ? Support.icon :
                          StoneIconOverride;

    private Image  _iconImage;
    private Canvas _rootCanvas;
    private Image  _ghost;

    public void Init(Image iconImage)
    {
        _iconImage  = iconImage;
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem) { eventData.pointerDrag = null; return; }

        var go = new GameObject("DragGhost");
        go.transform.SetParent(_rootCanvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghost               = go.AddComponent<Image>();
        _ghost.sprite        = Icon;
        _ghost.raycastTarget = false;
        _ghost.rectTransform.sizeDelta = new Vector2(60f, 60f);

        MoveGhost(eventData);

        if (_iconImage != null)
            _iconImage.color = new Color(1f, 1f, 1f, 0.3f);

        OnSkillDragStarted?.Invoke();
    }

    public void OnDrag(PointerEventData eventData) => MoveGhost(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghost != null) { Destroy(_ghost.gameObject); _ghost = null; }
        if (_iconImage != null)
            _iconImage.color = HasItem ? Color.white : Color.clear;

        OnSkillDragEnded?.Invoke();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        // 이 핸들러는 SupportSlotUI / OwnedSupportSlotUI 등 다른 슬롯에도 부착되지만,
        // IDropHandler 동작은 인벤 슬롯에서만 의미가 있다. SourceDisplayIndex < 0 은 인벤 외부 슬롯의 표시이므로
        // 여기서 드롭 처리를 그대로 두면 장착 메인 스킬을 서포트 슬롯에 떨어뜨릴 때 의도치 않은 unequip 이 발생한다.
        if (SourceDisplayIndex < 0) return;

        // GenerateSlot (Rift 장착 stone) → 인벤 슬롯: 회수.
        // 인벤 슬롯이 IDropHandler 라 panel-level InvenDropHandler 까지 안 가므로 여기서도 처리.
        var generate = eventData.pointerDrag.GetComponent<GenerateSlotDropTarget>();
        if (generate != null)
        {
            InventorySystem.TryUnloadStoneFromRift(generate);
            return;
        }

        // 장착 스킬 슬롯에서 드랍: 언이퀴 + 인벤 반환
        if (eventData.pointerDrag.GetComponent<SkillSlotDragHandler>() != null)
        {
            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null || tower.EquippedSkill == null) return;
            var skill = tower.EquippedSkill;
            InventorySystem.Instance.UnequipSkill();
            ShopSystem.Instance?.ReturnSkill(skill);
            return;
        }

        var source = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (source == null || source == this) return;

        // 장착 서포트 슬롯 → 인벤 슬롯 위에 드롭: 해제 + 인벤 반환.
        // (InvenDropHandler 는 인벤 패널 배경만 받기 때문에 슬롯 위에 떨어진 경우 여기서 처리해야 한다)
        if (source.SourceDisplayIndex < 0)
        {
            var sourceSupportSlot = eventData.pointerDrag.GetComponent<SupportSlotUI>();
            if (sourceSupportSlot == null || source.Support == null) return;

            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null) return;

            int slotIdx = sourceSupportSlot.SlotIndex;
            if (slotIdx < 0 || slotIdx >= tower.UnlockedSupportSlots) return;

            var option = source.Support;
            if (tower.SupportOptions[slotIdx] != option) return;

            InventorySystem.Instance.SetSupportOption(slotIdx, null);
            ShopSystem.Instance?.ReturnSupportOption(option);
            return;
        }

        // 인벤 슬롯 ↔ 인벤 슬롯 — 자유 재배치 (cross-type swap 포함)
        if (ShopSystem.Instance == null) return;

        // 빈 슬롯으로 드롭 (target index >= 보유 수): Move 로 동작. 채워진 슬롯은 Swap.
        int ownedCount = ShopSystem.Instance.OwnedDisplayOrder.Count;
        if (SourceDisplayIndex >= ownedCount)
            ShopSystem.Instance.MoveDisplayOrder(source.SourceDisplayIndex, SourceDisplayIndex);
        else
            ShopSystem.Instance.SwapDisplayOrder(source.SourceDisplayIndex, SourceDisplayIndex);
    }

    private void MoveGhost(PointerEventData eventData)
    {
        if (_ghost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var local);
        _ghost.rectTransform.localPosition = local;
    }
}
