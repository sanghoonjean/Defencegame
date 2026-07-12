using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button 클릭 시 지정된 패널을 SetActive(false) 로 숨기는 "닫기 전용" 버튼.
/// UIToggleButton(토글) / CanvasGroupToggleButton(alpha 토글) 과 달리 항상 숨김만 수행한다.
/// 열기는 별도의 열기 버튼(UIToggleButton)이 담당 — 예: SHOP_UI / InventoryUI 의 CancelButton.
/// </summary>
[RequireComponent(typeof(Button))]
public class UICloseButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Close);
    }

    private void Close()
    {
        if (targetPanel == null)
        {
            Debug.LogWarning($"[UICloseButton] targetPanel이 연결되지 않았습니다 — {gameObject.name}");
            return;
        }
        targetPanel.SetActive(false);
    }
}
