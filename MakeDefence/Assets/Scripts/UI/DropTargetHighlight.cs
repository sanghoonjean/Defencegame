using UnityEngine;
using UnityEngine.UI;

// 드랍 가능 영역(SkillSlotUI, InvenDropHandler, ShopDropHandler 등)에 부착.
// InvenSlotDragHandler 드래그 시작/종료 이벤트를 받아 배경 색상을 변경한다.
public class DropTargetHighlight : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0f, 0.35f);

    private Color _originalColor;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        if (targetImage != null)
            _originalColor = targetImage.color;
    }

    private void OnEnable()
    {
        InvenSlotDragHandler.OnSkillDragStarted += ShowHighlight;
        InvenSlotDragHandler.OnSkillDragEnded   += HideHighlight;
    }

    private void OnDisable()
    {
        InvenSlotDragHandler.OnSkillDragStarted -= ShowHighlight;
        InvenSlotDragHandler.OnSkillDragEnded   -= HideHighlight;
        HideHighlight();
    }

    private void ShowHighlight()
    {
        if (targetImage != null) targetImage.color = highlightColor;
    }

    private void HideHighlight()
    {
        if (targetImage != null) targetImage.color = _originalColor;
    }
}
