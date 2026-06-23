using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 패널의 "웨이브 생성" 버튼.
/// 클릭 → SelectedRift.OpenRift() (장착 차원석 소모 + RiftWave 시작).
/// SelectedRift / LoadedStone / WaveSystem / GameState 변화 → Button.interactable 자동 갱신.
/// </summary>
[RequireComponent(typeof(Button))]
public class OpenRiftButton : MonoBehaviour
{
    private Button _button;
    private RiftGenerator _current;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        InventorySystem.OnRiftSelected   += HandleRiftSelected;
        WaveSystem.OnWaveStarted         += HandleWaveStarted;
        WaveSystem.OnWaveEnded           += HandleWaveEnded;
        GameStateSystem.OnStateChanged   += HandleStateChanged;
        HandleRiftSelected(InventorySystem.Instance?.SelectedRift);
    }

    private void OnDisable()
    {
        InventorySystem.OnRiftSelected   -= HandleRiftSelected;
        WaveSystem.OnWaveStarted         -= HandleWaveStarted;
        WaveSystem.OnWaveEnded           -= HandleWaveEnded;
        GameStateSystem.OnStateChanged   -= HandleStateChanged;
        if (_current != null)
        {
            _current.OnStoneChanged -= Refresh;
            _current = null;
        }
    }

    private void HandleRiftSelected(RiftGenerator rift)
    {
        if (_current != null) _current.OnStoneChanged -= Refresh;
        _current = rift;
        if (_current != null) _current.OnStoneChanged += Refresh;
        Refresh();
    }

    private void HandleWaveStarted(int _) => Refresh();
    private void HandleWaveEnded(bool _) => Refresh();
    private void HandleStateChanged(GameState _) => Refresh();

    private void Refresh()
    {
        if (_button == null) return;
        _button.interactable =
            _current != null
            && _current.LoadedStone != null
            && WaveSystem.Instance != null && !WaveSystem.Instance.IsWaveActive
            && GameStateSystem.Current == GameState.Playing;
    }

    private void OnClick()
    {
        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null) return;
        rift.OpenRift();
    }
}
