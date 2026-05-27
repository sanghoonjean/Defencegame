using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvenSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public static event Action OnSkillDragStarted;
    public static event Action OnSkillDragEnded;

    public SkillData Skill     { get; set; }
    public int       SlotIndex { get; set; } = -1;

    private Image  _iconImage;
    private Canvas _rootCanvas;
    private Image  _ghost;

    public void Init(Image iconImage)
    {
        _iconImage   = iconImage;
        _rootCanvas  = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Skill == null) { eventData.pointerDrag = null; return; }

        var go = new GameObject("DragGhost");
        go.transform.SetParent(_rootCanvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghost             = go.AddComponent<Image>();
        _ghost.sprite      = Skill.icon;
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
            _iconImage.color = Skill != null ? Color.white : Color.clear;

        OnSkillDragEnded?.Invoke();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        // 장착 슬롯에서 드랍: 언이퀴 + 인벤토리 반환 (InvenDropHandler와 동일)
        if (eventData.pointerDrag.GetComponent<SkillSlotDragHandler>() != null)
        {
            var tower = InventorySystem.Instance?.SelectedTower;
            if (tower == null || tower.EquippedSkill == null) return;
            var skill = tower.EquippedSkill;
            InventorySystem.Instance.UnequipSkill();
            ShopSystem.Instance?.ReturnSkill(skill);
            return;
        }

        // 인벤토리 슬롯 간 드랍: 스왑 / 이동
        var source = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (source == null || source == this) return;
        if (SlotIndex < 0 || source.SlotIndex < 0) return;
        if (ShopSystem.Instance == null) return;

        if (source.Skill != null && Skill == null)
            ShopSystem.Instance.MoveOwnedSkill(source.SlotIndex, SlotIndex);
        else if (source.Skill != null && Skill != null)
            ShopSystem.Instance.SwapOwnedSkills(source.SlotIndex, SlotIndex);
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
