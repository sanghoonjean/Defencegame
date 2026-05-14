using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보조 옵션 슬롯 1칸에 부착.
/// slotIndex (0~2)를 Inspector에서 지정하고
/// InventorySystem.OnTowerSelected 이벤트로 갱신된다.
/// </summary>
public class SupportSlotUI : MonoBehaviour
{
    [SerializeField] private int       slotIndex;
    [SerializeField] private Image     iconImage;
    [SerializeField] private Text      optionNameText;
    [SerializeField] private GameObject lockedLabel;
    [SerializeField] private GameObject emptyLabel;

    private void OnEnable()
    {
        InventorySystem.OnTowerSelected += Refresh;
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= Refresh;
    }

    private void Start()
    {
        Refresh(InventorySystem.Instance != null ? InventorySystem.Instance.SelectedTower : null);
    }

    private void Refresh(Tower tower)
    {
        if (tower == null)
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
            iconImage.gameObject.SetActive(!locked && hasOption && option?.icon != null);
            if (!locked && hasOption && option?.icon != null)
                iconImage.sprite = option.icon;
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
