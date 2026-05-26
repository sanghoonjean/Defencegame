using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvenSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
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
    }

    public void OnDrag(PointerEventData eventData) => MoveGhost(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghost != null) { Destroy(_ghost.gameObject); _ghost = null; }
        if (_iconImage != null)
            _iconImage.color = Skill != null ? Color.white : Color.clear;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var source = eventData.pointerDrag?.GetComponent<InvenSlotDragHandler>();
        if (source == null || source == this) return;
        if (SlotIndex < 0 || source.SlotIndex < 0) return;
        if (ShopSystem.Instance == null) return;

        // 소스에 스킬 있고 목적지가 빈 슬롯이면 이동, 둘 다 스킬 있으면 스왑
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
