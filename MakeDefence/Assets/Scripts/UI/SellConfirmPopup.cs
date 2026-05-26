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

    public void ShowSupportSell(SupportOptionData option)
    {
        _pendingTower   = null;
        _pendingSkill   = null;
        _pendingSupport = option;

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
        var tower   = _pendingTower;
        var skill   = _pendingSkill;
        var support = _pendingSupport;
        _pendingTower   = null;
        _pendingSkill   = null;
        _pendingSupport = null;
        Hide();

        if (support != null)
        {
            // 서포트 판매: 인벤토리 우선, 없으면 장착 슬롯 탐색
            bool removed = ShopSystem.Instance != null &&
                           ShopSystem.Instance.RemoveOwnedSupportOption(support);

            if (!removed)
            {
                var t = InventorySystem.Instance?.SelectedTower;
                if (t == null) return;
                bool found = false;
                for (int i = 0; i < t.UnlockedSupportSlots; i++)
                {
                    if (t.SupportOptions[i] != support) continue;
                    InventorySystem.Instance.SetSupportOption(i, null);
                    found = true;
                    break;
                }
                if (!found) return;
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
        _pendingSkill   = null;
        _pendingSupport = null;
        panel.SetActive(false);
    }
}
