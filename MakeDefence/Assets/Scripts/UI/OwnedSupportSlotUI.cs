using UnityEngine;
using UnityEngine.UI;

public class OwnedSupportSlotUI : MonoBehaviour
{
    [SerializeField] private Image  iconImage;
    [SerializeField] private Text   nameText;

    private SupportOptionDragHandler _dragHandler;
    private SupportOptionData        _option;

    private void Awake()
    {
        _dragHandler = gameObject.GetComponent<SupportOptionDragHandler>()
                    ?? gameObject.AddComponent<SupportOptionDragHandler>();

        var icon = iconImage != null ? iconImage : GetComponentInChildren<Image>();
        _dragHandler.Init(icon);
    }

    public void Setup(SupportOptionData option)
    {
        _option = option;
        _dragHandler.Option = option;

        if (iconImage != null)
            iconImage.sprite = option.icon;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(option.displayName)
                ? option.optionType.ToString()
                : option.displayName;
    }
}
