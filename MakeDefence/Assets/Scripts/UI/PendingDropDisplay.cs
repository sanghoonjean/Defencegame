using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수확 대기 큐브 카운터를 HUD 에 표시 (가시성 안전망 D).
/// "수확 대기: Lower×3 Upper×1 ..." 형태로 단일 Text 출력.
/// 카운트 0 이면 빈 문자열.
/// </summary>
public class PendingDropDisplay : MonoBehaviour
{
    [SerializeField] private Text pendingText;
    [SerializeField] private string prefix = "Pending: ";

    private readonly StringBuilder _sb = new();

    private void OnEnable()
    {
        DroppedCubeSystem.OnPendingChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        DroppedCubeSystem.OnPendingChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (pendingText == null) return;
        if (DroppedCubeSystem.Instance == null)
        {
            pendingText.text = string.Empty;
            return;
        }

        var counts = DroppedCubeSystem.Instance.PendingCounts;
        _sb.Length = 0;
        int total = 0;
        foreach (var kv in counts)
        {
            if (kv.Value <= 0) continue;
            if (_sb.Length > 0) _sb.Append("  ");
            _sb.Append(kv.Key).Append('×').Append(kv.Value);
            total += kv.Value;
        }
        pendingText.text = total > 0 ? prefix + _sb : string.Empty;
    }
}
