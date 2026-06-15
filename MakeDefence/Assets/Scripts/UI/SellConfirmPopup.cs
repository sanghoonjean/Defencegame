using UnityEngine;
using UnityEngine.UI;

public class SellConfirmPopup : MonoBehaviour
{
    public static SellConfirmPopup Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text       messageText;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private Button     cancelButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private SkillData          _pendingSkill;
    private Tower              _pendingTower;
    private SupportOptionData  _pendingSupport;
    private int                _pendingSupportSlotIdx;     // -1=인벤토리, >=0=장착 슬롯 인덱스
    private int                _pendingSourceDisplayIdx;   // 인벤 displayOrder 인덱스 (중복 보유 무결성용). -1 이면 fallback

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);
    }

    public void Show(Tower tower, SkillData skill)
    {
        _pendingTower            = tower;
        _pendingSkill            = skill;
        _pendingSourceDisplayIdx = -1;

        if (messageText != null)
            messageText.text = $"'{skill.displayName}'을(를) 판매하시겠습니까?\n하급 큐브 1개를 획득합니다.";

        panel.SetActive(true);
    }

    public void ShowSupportSell(SupportOptionData option, int equippedSlotIndex = -1, int sourceDisplayIdx = -1)
    {
        _pendingTower            = equippedSlotIndex >= 0
                                   ? InventorySystem.Instance?.SelectedTower
                                   : null;
        _pendingSkill            = null;
        _pendingSupport          = option;
        _pendingSupportSlotIdx   = equippedSlotIndex;
        _pendingSourceDisplayIdx = sourceDisplayIdx;

        if (messageText != null)
        {
            string name = string.IsNullOrEmpty(option.displayName)
                ? option.optionType.ToString()
                : option.displayName;
            messageText.text = $"'{name}'을(를) 판매하시겠습니까?\n하급 큐브 1개를 획득합니다.";
        }

        panel.SetActive(true);
    }

    public void ShowInventorySell(SkillData skill, int sourceDisplayIdx = -1)
    {
        _pendingTower            = null;
        _pendingSkill            = skill;
        _pendingSourceDisplayIdx = sourceDisplayIdx;

        if (messageText != null)
            messageText.text = $"'{skill.displayName}'을(를) 판매하시겠습니까?\n하급 큐브 1개를 획득합니다.";

        panel.SetActive(true);
    }

    private void OnConfirm()
    {
        var tower         = _pendingTower;
        var skill         = _pendingSkill;
        var support       = _pendingSupport;
        int supportIdx    = _pendingSupportSlotIdx;
        int sourceDispIdx = _pendingSourceDisplayIdx;
        _pendingTower            = null;
        _pendingSkill            = null;
        _pendingSupport          = null;
        _pendingSupportSlotIdx   = -1;
        _pendingSourceDisplayIdx = -1;
        Hide();

        if (support != null)
        {
            if (supportIdx >= 0)
            {
                // 장착 슬롯 출처
                if (tower == null) return;
                if (tower.SupportOptions[supportIdx] != support) return;
                tower.SetSupportOption(supportIdx, null);
                InventorySystem.Instance?.SelectTower(tower);
            }
            else
            {
                // 인벤 출처 — SourceDisplayIndex 우선, 없으면 자산참조 fallback
                if (ShopSystem.Instance == null) return;
                bool removed = sourceDispIdx >= 0
                    ? ShopSystem.Instance.RemoveByDisplayIndex(sourceDispIdx)
                    : ShopSystem.Instance.RemoveOwnedSupportOption(support);
                if (!removed) return;
            }
        }
        else if (tower != null)
        {
            // 장착 스킬 판매
            if (tower.EquippedSkill != skill) return;
            tower.UnequipSkill();
            InventorySystem.Instance?.SelectTower(tower);
        }
        else
        {
            // 인벤 스킬 판매
            if (ShopSystem.Instance == null) return;
            bool removed = sourceDispIdx >= 0
                ? ShopSystem.Instance.RemoveByDisplayIndex(sourceDispIdx)
                : ShopSystem.Instance.RemoveOwnedSkill(skill);
            if (!removed) return;
        }

        CubeSystem.Instance?.Add(CubeType.Lower, 1);
    }

    private void Hide()
    {
        _pendingSkill            = null;
        _pendingSupport          = null;
        _pendingSupportSlotIdx   = -1;
        _pendingSourceDisplayIdx = -1;
        panel.SetActive(false);
    }
}
