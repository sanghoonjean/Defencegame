using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탭시트 컨트롤러. 탭 버튼과 페이지를 인덱스로 짝지어, 탭 클릭 시 해당 페이지만
/// SetActive(true) 하고 활성 탭 버튼을 색상으로 강조한다.
/// 페이지 전환이 SetActive 기반이므로 페이지 내부 UI 의 OnEnable Refresh 패턴과 호환된다.
/// </summary>
public class UITabView : MonoBehaviour
{
    [Tooltip("탭 버튼 목록. pages 와 인덱스로 짝을 이룬다.")]
    [SerializeField] private Button[] tabButtons;

    [Tooltip("탭별 페이지 루트. tabButtons 와 인덱스로 짝을 이룬다.")]
    [SerializeField] private GameObject[] pages;

    [Tooltip("최초 활성화 시 선택할 탭 인덱스. 이후에는 마지막 선택 탭을 유지한다.")]
    [SerializeField] private int defaultTabIndex = 0;

    [Tooltip("활성 탭 버튼의 Image 색상.")]
    [SerializeField] private Color activeTabColor = Color.white;

    [Tooltip("비활성 탭 버튼의 Image 색상.")]
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f);

    private int _currentTabIndex = -1;

    /// <summary>현재 선택된 탭 인덱스. 아직 한 번도 선택되지 않았으면 -1.</summary>
    public int CurrentTabIndex => _currentTabIndex;

    private void Awake()
    {
        if (!IsConfigValid()) return;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            int index = i; // 클로저가 루프 변수를 공유하지 않도록 복사
            tabButtons[i].onClick.AddListener(() => SelectTab(index));
        }
    }

    private void OnEnable()
    {
        // 패널이 열릴 때 마지막 선택 탭(최초에는 default 탭)을 복원한다.
        if (!IsConfigValid()) return;
        SelectTab(_currentTabIndex >= 0 ? _currentTabIndex : defaultTabIndex);
    }

    /// <summary>해당 인덱스의 페이지만 활성화하고 탭 버튼 하이라이트를 갱신한다.</summary>
    public void SelectTab(int index)
    {
        if (!IsConfigValid()) return;
        if (index < 0 || index >= pages.Length)
        {
            Debug.LogWarning($"[UITabView] 잘못된 탭 인덱스 {index} — {gameObject.name}");
            return;
        }

        _currentTabIndex = index;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == index);
            if (tabButtons[i] != null && tabButtons[i].image != null)
                tabButtons[i].image.color = i == index ? activeTabColor : inactiveTabColor;
        }
    }

    private bool IsConfigValid()
    {
        if (tabButtons == null || pages == null ||
            tabButtons.Length == 0 || tabButtons.Length != pages.Length)
        {
            Debug.LogWarning($"[UITabView] tabButtons/pages 설정이 잘못되었습니다 — {gameObject.name}");
            return false;
        }
        return true;
    }
}
