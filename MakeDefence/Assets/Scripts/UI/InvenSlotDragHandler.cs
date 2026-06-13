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

    // SourceDisplayIndex: 인벤 그리드에서의 슬롯 위치. 인벤 외부 (장착 슬롯 등) 에서 시작한 드래그는 -1
    public int SourceDisplayIndex { get; set; } = -1;
    // legacy alias — 기존 호출자 호환
    public int SlotIndex { get => SourceDisplayIndex; set => SourceDisplayIndex = value; }

    public InventoryItemKind Kind => Skill != null ? InventoryItemKind.Skill : InventoryItemKind.Support;
    public bool   HasItem => Skill != null || Support != null;
    public Sprite Icon    => Skill != null ? Skill.icon : Support?.icon;

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

        // 인벤 슬롯 ↔ 인벤 슬롯 — 자유 재배치 (cross-type swap 포함)
        var source = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (source == null || source == this) return;
        if (source.SourceDisplayIndex < 0) return;        // 장착 슬롯 등 외부 드래그는 별도 경로
        if (SourceDisplayIndex < 0) return;
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
