using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 의 일시정지 버튼. 클릭 시 PauseSystem.Toggle(), OnPauseChanged 구독으로 라벨 갱신.
/// </summary>
[RequireComponent(typeof(Button))]
public class PauseHudButton : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private string playLabel = "▶";
    [SerializeField] private string pauseLabel = "⏸";

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleClick);
        PauseSystem.OnPauseChanged += HandlePauseChanged;
        RefreshLabel(PauseSystem.Instance != null && PauseSystem.Instance.IsPaused);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleClick);
        PauseSystem.OnPauseChanged -= HandlePauseChanged;
    }

    private void HandleClick()
    {
        PauseSystem.Instance?.Toggle();
    }

    private void HandlePauseChanged(bool paused)
    {
        RefreshLabel(paused);
    }

    private void RefreshLabel(bool paused)
    {
        if (label != null) label.text = paused ? playLabel : pauseLabel;
    }
}
