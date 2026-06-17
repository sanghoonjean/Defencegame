using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 의 배속 버튼. 클릭 시 GameSpeedSystem.Cycle() 호출, OnSpeedChanged 구독으로 라벨 갱신.
/// </summary>
[RequireComponent(typeof(Button))]
public class GameSpeedHudButton : MonoBehaviour
{
    [SerializeField] private Text label;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleClick);
        GameSpeedSystem.OnSpeedChanged += HandleSpeedChanged;
        RefreshLabel(GameSpeedSystem.Instance != null ? GameSpeedSystem.Instance.Current : 1f);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleClick);
        GameSpeedSystem.OnSpeedChanged -= HandleSpeedChanged;
    }

    private void HandleClick()
    {
        GameSpeedSystem.Instance?.Cycle();
    }

    private void HandleSpeedChanged(float speed)
    {
        RefreshLabel(speed);
    }

    private void RefreshLabel(float speed)
    {
        if (label != null) label.text = $"{Mathf.RoundToInt(speed)}x";
    }
}
