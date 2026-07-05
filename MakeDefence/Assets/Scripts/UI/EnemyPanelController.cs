using UnityEngine;
using UnityEngine.UI;

public class EnemyPanelController : MonoBehaviour
{
    [SerializeField] private Text countText;

    private void OnEnable()
    {
        WaveSystem.OnAliveCountChanged += OnAliveCountChanged;
        if (WaveSystem.Instance != null)
            OnAliveCountChanged(WaveSystem.Instance.AliveCount, WaveSystem.Instance.TotalCount);
    }

    private void OnDisable()
    {
        WaveSystem.OnAliveCountChanged -= OnAliveCountChanged;
    }

    // Awake 순서는 GameObject 간에 보장되지 않아 OnEnable 시점엔 WaveSystem.Instance가
    // 아직 null일 수 있다. Start는 씬의 모든 Awake가 끝난 뒤 실행되는 것이 보장되므로
    // 여기서 한 번 더 동기화한다.
    private void Start()
    {
        if (WaveSystem.Instance != null)
            OnAliveCountChanged(WaveSystem.Instance.AliveCount, WaveSystem.Instance.TotalCount);
    }

    private void OnAliveCountChanged(int alive, int total)
    {
        if (countText == null) return;
        countText.text = $"{alive}/{total}";
    }
}
