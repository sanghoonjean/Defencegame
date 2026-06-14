using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SupportSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] private int        slotIndex;
    [SerializeField] private Image      iconImage;
    [SerializeField] private Text       optionNameText;
    [SerializeField] private Image      lockIcon;
    [SerializeField] private GameObject emptyLabel;

    public int SlotIndex => slotIndex;

    private bool                 _isLocked = true;
    private InvenSlotDragHandler _dragHandler;

    private void Awake()
    {
        _dragHandler = gameObject.GetComponent<InvenSlotDragHandler>()
                    ?? gameObject.AddComponent<InvenSlotDragHandler>();
        _dragHandler.Init(iconImage);
        _dragHandler.SourceDisplayIndex = -1; // 장착 슬롯 → 인벤 인덱스 없음
    }

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
        Debug.Log($"[SupportSlotUI] OnPointerClick — slotIndex={slotIndex}, _isLocked={_isLocked}");
        if (!_isLocked) { Debug.Log($"[SupportSlotUI] 슬롯[{slotIndex}] 이미 해금됨, 무시"); return; }

        var tower = InventorySystem.Instance?.SelectedTower;
        Debug.Log($"[SupportSlotUI] SelectedTower={tower?.name ?? "null"}, InventorySystem={InventorySystem.Instance != null}");
        if (tower == null) return;

        Debug.Log($"[SupportSlotUI] slotIndex={slotIndex}, UnlockedSupportSlots={tower.UnlockedSupportSlots}");
        if (slotIndex != tower.UnlockedSupportSlots) { Debug.Log($"[SupportSlotUI] 순서 불일치 — 클릭 무시"); return; }

        int cost = tower.GetNextSupportSlotCost();
        Debug.Log($"[SupportSlotUI] cost={cost}, SupportUnlockPopup.Instance={SupportUnlockPopup.Instance != null}");
        if (cost < 0) return;

        SupportUnlockPopup.Instance?.Show(cost, tower);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_isLocked) return;

        var drag = eventData.pointerDrag?.GetComponent<InvenSlotDragHandler>();
        if (drag == null || drag.Support == null) return; // Skill 페이로드는 거부

        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null) return;

        if (slotIndex < 0 || slotIndex >= tower.SupportOptions.Count) return;

        var newOption  = drag.Support;
        var prevOption = slotIndex < tower.UnlockedSupportSlots ? tower.SupportOptions[slotIndex] : null;

        // 소스가 장착 슬롯인지 확인
        var sourceSlotUI  = eventData.pointerDrag.GetComponent<SupportSlotUI>();
        int sourceSlotIdx = sourceSlotUI != null ? sourceSlotUI.SlotIndex : -1;

        if (sourceSlotIdx == slotIndex) return;

        // 같은 타워의 다른 슬롯에 이미 동일 옵션이 장착돼 있으면 거부
        for (int i = 0; i < tower.UnlockedSupportSlots; i++)
        {
            if (i == slotIndex) continue;
            if (i == sourceSlotIdx) continue;
            if (tower.SupportOptions[i] == newOption) return;
        }

        bool ok = InventorySystem.Instance.SetSupportOption(slotIndex, newOption);
        if (!ok) return;

        if (sourceSlotIdx >= 0)
        {
            // 장착 슬롯 간 swap
            InventorySystem.Instance.SetSupportOption(sourceSlotIdx, prevOption);
        }
        else
        {
            // 인벤 출처: SourceDisplayIndex 기반 제거 (중복 보유 무결성)
            if (drag.SourceDisplayIndex >= 0)
                ShopSystem.Instance?.RemoveByDisplayIndex(drag.SourceDisplayIndex);
            else
                ShopSystem.Instance?.RemoveOwnedSupportOption(newOption); // fallback

            if (prevOption != null)
                ShopSystem.Instance?.ReturnSupportOption(prevOption);
        }
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
        if (_dragHandler != null)
        {
            _dragHandler.Skill   = null;
            _dragHandler.Support = (!locked && hasOption) ? option : null;
        }

        if (lockIcon != null)
            lockIcon.gameObject.SetActive(locked);

        if (emptyLabel != null)
            emptyLabel.SetActive(!locked && !hasOption);

        if (iconImage != null)
        {
            bool show = !locked && hasOption;
            iconImage.gameObject.SetActive(show);
            if (show) iconImage.color = Color.white;
            iconImage.sprite = show ? option?.icon : null;
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
