using UnityEngine;
using UnityEngine.UI;

public class OwnedSupportSlotUI : MonoBehaviour
{
    [SerializeField] private Image  iconImage;
    [SerializeField] private Text   nameText;

    private InvenSlotDragHandler _dragHandler;
    private SupportOptionData    _option;

    private void Awake()
    {
        _dragHandler = gameObject.GetComponent<InvenSlotDragHandler>()
                    ?? gameObject.AddComponent<InvenSlotDragHandler>();

        var icon = iconImage != null ? iconImage : GetComponentInChildren<Image>();
        _dragHandler.Init(icon);
        _dragHandler.SourceDisplayIndex = -1; // OwnedSupportListUI는 자체 리스트 — 인벤 displayOrder 와 별개
    }

    public void Setup(SupportOptionData option)
    {
        _option = option;
        _dragHandler.Skill   = null;
        _dragHandler.Support = option;

        if (iconImage != null)
            iconImage.sprite = option.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(option.displayName)
                ? option.optionType.ToString()
                : option.displayName;
    }
}
