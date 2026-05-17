using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SupportSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private int       slotIndex;
    [SerializeField] private Image     iconImage;
    [SerializeField] private Text      optionNameText;
    [SerializeField] private GameObject lockedLabel;
    [SerializeField] private GameObject emptyLabel;

    private void OnEnable()
    {
        InventorySystem.OnTowerSelected += Refresh;
        Refresh(InventorySystem.Instance != null ? InventorySystem.Instance.SelectedTower : null);
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= Refresh;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag?.GetComponent<InvenSupportSlotDragHandler>();
        if (drag == null || drag.Option == null) return;

        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null) return;
        if (slotIndex >= tower.UnlockedSupportSlots) return;

        var existing = tower.SupportOptions[slotIndex];
        if (existing != null)
            ShopSystem.Instance?.ReturnSupportOption(existing);

        ShopSystem.Instance?.RemoveOwnedSupportOption(drag.Option);
        InventorySystem.Instance.SetSupportOption(slotIndex, drag.Option);
    }

    private void Refresh(Tower tower)
    {
        if (tower == null || slotIndex < 0 || slotIndex >= tower.SupportOptions.Count)
        {
            SetState(locked: false, hasOption: false, option: null);
            return;
        }

        bool isUnlocked = slotIndex < tower.UnlockedSupportSlots;
        SupportOptionData option = isUnlocked ? tower.SupportOptions[slotIndex] : null;

        SetState(locked: !isUnlocked, hasOption: option != null, option: option);
    }

    private void SetState(bool locked, bool hasOption, SupportOptionData option)
    {
        if (lockedLabel != null)
            lockedLabel.SetActive(locked);

        if (emptyLabel != null)
            emptyLabel.SetActive(!locked && !hasOption);

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(!locked && hasOption);
            iconImage.sprite = (!locked && hasOption) ? option?.icon : null;
        }

        if (optionNameText != null)
        {
            optionNameText.gameObject.SetActive(!locked && hasOption);
            if (!locked && hasOption && option != null)
                optionNameText.text = string.IsNullOrEmpty(option.displayName)
                    ? option.optionType.ToString()
                    : option.displayName;
        }
    }
}
