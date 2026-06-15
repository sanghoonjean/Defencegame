using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타워 삭제 확인 모달.
/// Show(tower) 시점에 타워를 캡처해 두고, Confirm 시 캡처된 타워를 그대로
/// InventorySystem.DeleteTower 로 전달한다. 팝업 오픈 ↔ 확정 사이에
/// SelectedTower 가 바뀌어도 잘못된 타워가 삭제되지 않는다.
/// (기존 <see cref="SellConfirmPopup"/> 의 _pendingTower 패턴 동일)
/// </summary>
public class TowerDeleteConfirmPopup : MonoBehaviour
{
    public static TowerDeleteConfirmPopup Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text       messageText;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private Button     cancelButton;

    private Tower _pendingTower;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(Hide);
    }

    public void Show(Tower tower)
    {
        if (tower == null) return;

        _pendingTower = tower;

        if (messageText != null)
            messageText.text = "타워를 삭제하시겠습니까?\n하급 큐브 1개를 획득합니다.";

        if (panel != null) panel.SetActive(true);
    }

    /// <summary>현재 InventorySystem.SelectedTower 를 대상으로 팝업을 띄운다.</summary>
    public void ShowForSelectedTower()
    {
        var tower = InventorySystem.Instance != null
            ? InventorySystem.Instance.SelectedTower
            : null;
        Show(tower);
    }

    private void OnConfirm()
    {
        var target = _pendingTower;
        _pendingTower = null;
        Hide();

        InventorySystem.Instance?.DeleteTower(target);
    }

    private void Hide()
    {
        _pendingTower = null;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        bool isOpen = panel != null && panel.activeSelf;

        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnConfirm();
            }
            return;
        }

        if (!Input.GetKeyDown(KeyCode.D)) return;
        if (IsTextInputFocused()) return;
        if (IsAnotherModalOpen()) return;
        if (InventorySystem.Instance == null || InventorySystem.Instance.SelectedTower == null) return;

        ShowForSelectedTower();
    }

    private static bool IsAnotherModalOpen()
    {
        if (SellConfirmPopup.Instance    != null && SellConfirmPopup.Instance.IsOpen)    return true;
        if (SupportUnlockPopup.Instance  != null && SupportUnlockPopup.Instance.IsOpen)  return true;
        return false;
    }

    private static bool IsTextInputFocused()
    {
        var es = EventSystem.current;
        if (es == null) return false;

        var go = es.currentSelectedGameObject;
        if (go == null) return false;

        return go.GetComponent<InputField>() != null
            || go.GetComponent<TMP_InputField>() != null;
    }
}
