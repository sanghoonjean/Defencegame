using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 패널의 "웨이브 연속 생성" 토글 버튼.
/// ON 진입 시 첫 차원석 장착 + OpenRift → 매 클리어마다 다음 차원석 자동 장착 + OpenRift.
/// 인벤 empty / OpenRift 실패 / Defeat / Pause / 사용자 OFF 클릭 시 자동 Stop.
///
/// 구독은 OnEnable/OnDisable 에만 두고 Stop() 은 런타임 전환(시각 + 상태)만 담당.
/// 인벤이 빌 때까지 계속 소진 — 클리어 보너스 stone 도 다음 사이클에서 자동 사용 (사용자 결정).
/// </summary>
[RequireComponent(typeof(Button))]
public class RepeatGenerateToggleButton : MonoBehaviour
{
    private Button _button;
    private ColorBlock _originalColors;
    private bool _isActive;
    private DimensionStone _lastEquipped;
    private RiftGenerator _cachedRift;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _originalColors = _button.colors;
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        InventorySystem.OnRiftSelected           += HandleRiftSelected;
        WaveSystem.OnWaveStarted                 += HandleWaveStarted;
        WaveSystem.OnWaveEnded                   += HandleWaveEnded;
        GameStateSystem.OnStateChanged           += HandleStateChanged;
        PauseSystem.OnPauseChanged               += HandlePauseChanged;
        DimensionStoneInventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        InventorySystem.OnRiftSelected           -= HandleRiftSelected;
        WaveSystem.OnWaveStarted                 -= HandleWaveStarted;
        WaveSystem.OnWaveEnded                   -= HandleWaveEnded;
        GameStateSystem.OnStateChanged           -= HandleStateChanged;
        PauseSystem.OnPauseChanged               -= HandlePauseChanged;
        DimensionStoneInventory.OnInventoryChanged -= Refresh;
        RestoreColors();
    }

    private void OnClick()
    {
        if (_isActive) Stop();
        else           BeginRepeat();
    }

    private void BeginRepeat()
    {
        var rift = InventorySystem.Instance?.SelectedRift;
        var inv  = DimensionStoneInventory.Instance;
        if (rift == null || inv == null || inv.Count <= 0) return;
        if (WaveSystem.Instance == null || WaveSystem.Instance.IsWaveActive) return;
        if (GameStateSystem.Current != GameState.Playing) return;

        _isActive   = true;
        _cachedRift = rift;
        ApplyActiveColors();
        Refresh();
        TryConsumeNext();
    }

    private void Stop()
    {
        if (_lastEquipped != null && _cachedRift != null
            && _cachedRift.LoadedStone == _lastEquipped)
        {
            DimensionStoneInventory.Instance?.Add(_cachedRift.LoadedStone);
            _cachedRift.ClearStone();
        }
        _lastEquipped = null;
        _cachedRift   = null;
        _isActive  = false;
        RestoreColors();
        Refresh();
    }

    private void TryConsumeNext()
    {
        if (!_isActive) return;

        var inv = DimensionStoneInventory.Instance;
        if (_cachedRift == null || inv == null) { Stop(); return; }
        if (inv.Count <= 0) { Stop(); return; }

        var stone = inv.Stones[0];
        _lastEquipped = stone;

        DimensionStoneSlot.EquipToRift(_cachedRift, stone);

        bool started = _cachedRift.OpenRift();
        if (!started) Stop();
    }

    private void HandleRiftSelected(RiftGenerator rift)
    {
        Refresh();
    }

    private void HandleWaveStarted(int _) => Refresh();

    private void HandleWaveEnded(bool cleared)
    {
        if (_isActive)
        {
            _lastEquipped = null;
            if (!cleared) { Stop(); return; }
            TryConsumeNext();
        }
        Refresh();
    }

    private void HandleStateChanged(GameState state)
    {
        if (_isActive && state != GameState.Playing) Stop();
        Refresh();
    }

    private void HandlePauseChanged(bool paused)
    {
        if (_isActive && paused) Stop();
        Refresh();
    }

    private void Refresh()
    {
        if (_button == null) return;

        if (_isActive)
        {
            _button.interactable = true;
            return;
        }

        var inv = DimensionStoneInventory.Instance;
        _button.interactable =
            InventorySystem.Instance?.SelectedRift != null
            && inv != null && inv.Count > 0
            && WaveSystem.Instance != null && !WaveSystem.Instance.IsWaveActive
            && GameStateSystem.Current == GameState.Playing
            && (PauseSystem.Instance == null || !PauseSystem.Instance.IsPaused);
    }

    private void ApplyActiveColors()
    {
        var cb = _originalColors;
        cb.normalColor   = _originalColors.pressedColor;
        cb.highlightedColor = _originalColors.pressedColor;
        cb.selectedColor = _originalColors.pressedColor;
        _button.colors = cb;
    }

    private void RestoreColors()
    {
        if (_button != null) _button.colors = _originalColors;
    }
}
