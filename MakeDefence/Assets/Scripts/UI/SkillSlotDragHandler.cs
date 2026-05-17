using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
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
        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null || tower.EquippedSkill == null)
        {
            eventData.pointerDrag = null;
            return;
        }

        var go = new GameObject("SkillDragGhost");
        go.transform.SetParent(_rootCanvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghost               = go.AddComponent<Image>();
        _ghost.sprite        = tower.EquippedSkill.icon;
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

        var tower = InventorySystem.Instance?.SelectedTower;
        if (_iconImage != null)
            _iconImage.color = (tower?.EquippedSkill != null) ? Color.white : Color.clear;
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
