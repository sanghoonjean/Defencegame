using UnityEngine;
using UnityEngine.UI;

public class SupportUnlockPopup : MonoBehaviour
{
    public static SupportUnlockPopup Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text       costText;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private Button     cancelButton;

    private Tower _pendingTower;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);
    }

    public void Show(int cost, Tower tower)
    {
        _pendingTower = tower;

        if (costText != null)
            costText.text = $"상위 큐브 {cost}개를 소모하여 슬롯을 해금합니까?";

        bool canAfford = CubeSystem.Instance != null &&
                         CubeSystem.Instance.GetCount(CubeType.Upper) >= cost;
        confirmButton.interactable = canAfford;

        panel.SetActive(true);
    }

    private void OnConfirm()
    {
        var tower = _pendingTower;
        _pendingTower = null;
        Hide();
        InventorySystem.Instance?.UnlockSupportSlot(tower);
    }

    private void Hide()
    {
        panel.SetActive(false);
    }
}
