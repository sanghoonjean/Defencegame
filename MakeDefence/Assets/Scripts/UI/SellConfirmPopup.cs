using UnityEngine;
using UnityEngine.UI;

public class SellConfirmPopup : MonoBehaviour
{
    public static SellConfirmPopup Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text       messageText;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private Button     cancelButton;

    private SkillData _pendingSkill;
    private Tower     _pendingTower;

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

    private void OnConfirm()
    {
        var tower = _pendingTower;
        var skill = _pendingSkill;
        _pendingTower = null;
        _pendingSkill = null;
        Hide();

        if (tower == null || tower.EquippedSkill != skill) return;

        tower.UnequipSkill();
        InventorySystem.Instance?.SelectTower(tower);   // UI 갱신
        CubeSystem.Instance?.Add(CubeType.Lower, 1);
    }

    private void Hide()
    {
        _pendingSkill = null;
        panel.SetActive(false);
    }
}
