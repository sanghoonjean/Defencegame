using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SupportSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] private int        slotIndex;
    [SerializeField] private Image      iconImage;
    [SerializeField] private Text       optionNameText;
    [SerializeField] private GameObject lockedLabel;
    [SerializeField] private GameObject emptyLabel;

    private bool _isLocked = true;

    private void OnEnable()
    {
        InventorySystem.OnTowerSelected += Refresh;
        Refresh(InventorySystem.Instance != null ? InventorySystem.Instance.SelectedTower : null);
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= Refresh;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isLocked) return;

        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null) return;

        if (slotIndex != tower.UnlockedSupportSlots) return;

        int cost = tower.GetNextSupportSlotCost();
        if (cost < 0) return;

        SupportUnlockPopup.Instance?.Show(cost, tower);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_isLocked) return;

        var supportDrag = eventData.pointerDrag?.GetComponent<SupportOptionDragHandler>();
        if (supportDrag == null || supportDrag.Option == null) return;

        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null) return;

        var newOption = supportDrag.Option;
        var prevOption = slotIndex < tower.UnlockedSupportSlots ? tower.SupportOptions[slotIndex] : null;

        bool ok = InventorySystem.Instance.SetSupportOption(slotIndex, newOption);
        if (!ok) return;

        ShopSystem.Instance?.RemoveOwnedSupportOption(newOption);
        if (prevOption != null)
            ShopSystem.Instance?.ReturnSupportOption(prevOption);
    }

    private void Refresh(Tower tower)
    {
        if (tower == null || slotIndex < 0 || slotIndex >= tower.SupportOptions.Count)
        {
            _isLocked = false;
            SetState(locked: false, hasOption: false, option: null);
            return;
        }

        _isLocked = slotIndex >= tower.UnlockedSupportSlots;
        SupportOptionData option = !_isLocked ? tower.SupportOptions[slotIndex] : null;

        SetState(locked: _isLocked, hasOption: option != null, option: option);
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
