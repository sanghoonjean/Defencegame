using UnityEngine;
using UnityEngine.UI;

public class SellConfirmPopup : MonoBehaviour
{
    public static SellConfirmPopup Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text       messageText;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private Button     cancelButton;

    private SkillData          _pendingSkill;
    private Tower              _pendingTower;
    private SupportOptionData  _pendingSupport;
    private int                _pendingSupportSlotIdx; // -1=인벤토리, >=0=장착 슬롯 인덱스

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);
    }

    public void Show(Tower tower, SkillData skill)
    {
        _pendingTower = tower;
        _pendingSkill = skill;

        if (messageText != null)
            messageText.text = $"'{skill.displayName}'을(를) 판매하시겠습니까?\n하급 큐브 1개를 획득합니다.";

        panel.SetActive(true);
    }

    // equippedSlotIndex: -1=인벤토리 출처, >=0=장착 슬롯 인덱스
    public void ShowSupportSell(SupportOptionData option, int equippedSlotIndex = -1)
    {
        // 장착 슬롯 출처면 현재 타워를 캡처 (팝업 열린 사이 선택 변경 대비)
        _pendingTower          = equippedSlotIndex >= 0
                                 ? InventorySystem.Instance?.SelectedTower
                                 : null;
        _pendingSkill          = null;
        _pendingSupport        = option;
        _pendingSupportSlotIdx = equippedSlotIndex;

        if (messageText != null)
        {
            string name = string.IsNullOrEmpty(option.displayName)
                ? option.optionType.ToString()
                : option.displayName;
            messageText.text = $"'{name}'을(를) 판매하시겠습니까?\n하급 큐브 1개를 획득합니다.";
        }

        panel.SetActive(true);
    }

    // 인벤토리 슬롯 → 상점 판매용 (장착 슬롯 아님)
    public void ShowInventorySell(SkillData skill)
    {
        _pendingTower = null;
        _pendingSkill = skill;

        if (messageText != null)
            messageText.text = $"'{skill.displayName}'을(를) 판매하시겠습니까?\n하급 큐브 1개를 획득합니다.";

        panel.SetActive(true);
    }

    private void OnConfirm()
    {
        var tower      = _pendingTower;
        var skill      = _pendingSkill;
        var support    = _pendingSupport;
        int supportIdx = _pendingSupportSlotIdx;
        _pendingTower          = null;
        _pendingSkill          = null;
        _pendingSupport        = null;
        _pendingSupportSlotIdx = -1;
        Hide();

        if (support != null)
        {
            if (supportIdx >= 0)
            {
                // 장착 슬롯 출처: 드래그 시점에 캡처한 타워 사용
                if (tower == null) return;
                if (tower.SupportOptions[supportIdx] != support) return;
                tower.SetSupportOption(supportIdx, null);
                InventorySystem.Instance?.SelectTower(tower);
            }
            else
            {
                // 인벤토리 출처
                if (ShopSystem.Instance == null) return;
                if (!ShopSystem.Instance.RemoveOwnedSupportOption(support)) return;
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
            // 인벤토리 스킬 판매
            if (ShopSystem.Instance == null) return;
            if (!ShopSystem.Instance.RemoveOwnedSkill(skill)) return;
        }

        CubeSystem.Instance?.Add(CubeType.Lower, 1);
    }

    private void Hide()
    {
        _pendingSkill          = null;
        _pendingSupport        = null;
        _pendingSupportSlotIdx = -1;
        panel.SetActive(false);
    }
}
