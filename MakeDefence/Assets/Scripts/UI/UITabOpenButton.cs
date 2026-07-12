using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탭 패널 전용 HUD 열기 버튼 — UIToggleButton 의 탭 지정 버전.
/// 패널이 닫혀 있으면 열면서 지정 탭을 선택하고, 이미 열려 있으면
/// 같은 탭일 때는 닫고(기존 토글 동작 유지) 다른 탭일 때는 탭만 전환한다.
/// 예: Invertorybtn(tab 0) / SHOPbtn(tab 1) → ItemHubPanel.
/// </summary>
[RequireComponent(typeof(Button))]
public class UITabOpenButton : MonoBehaviour
{
    [Tooltip("열고 닫을 탭 패널 루트 (UITabView 가 붙은 오브젝트).")]
    [SerializeField] private GameObject targetPanel;

    [SerializeField] private UITabView tabView;

    [Tooltip("이 버튼이 선택할 탭 인덱스.")]
    [SerializeField] private int tabIndex;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (targetPanel == null || tabView == null)
        {
            Debug.LogWarning($"[UITabOpenButton] targetPanel/tabView가 연결되지 않았습니다 — {gameObject.name}");
            return;
        }

        if (!targetPanel.activeSelf)
        {
            targetPanel.SetActive(true);
            tabView.SelectTab(tabIndex);
        }
        else if (tabView.CurrentTabIndex == tabIndex)
        {
            targetPanel.SetActive(false);
        }
        else
        {
            tabView.SelectTab(tabIndex);
        }
    }
}
