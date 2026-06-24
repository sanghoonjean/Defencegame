using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 패널의 "웨이브 연속 생성" 토글 버튼.
/// ON 진입 시 인벤 카운트 스냅샷 → 매 클리어마다 다음 차원석 자동 장착 + OpenRift.
/// _remaining 0 / OpenRift 실패 / Defeat / Rift 해제 / 사용자 OFF 클릭 시 자동 Stop.
///
/// 구독은 OnEnable/OnDisable 에만 두고 Stop() 은 런타임 전환(시각 + 상태)만 담당.
/// 클리어 보너스 stone(DroppedStoneSystem.GrantClearBonus) 으로 인한 무한 farming 은
/// _remaining 카운터로 차단된다.
/// </summary>
[RequireComponent(typeof(Button))]
public class RepeatGenerateToggleButton : MonoBehaviour
{
    private Button _button;
    private ColorBlock _originalColors;
    private bool _isActive;
    private int _remaining;
    private DimensionStone _lastEquipped;

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

        _isActive  = true;
        _remaining = inv.Count;
        ApplyActiveColors();
        Refresh();
        TryConsumeNext();
    }

    private void Stop()
    {
        if (_lastEquipped != null)
        {
            var rift = InventorySystem.Instance?.SelectedRift;
            if (rift != null && rift.LoadedStone == _lastEquipped)
            {
                DimensionStoneInventory.Instance?.Add(rift.LoadedStone);
                rift.ClearStone();
            }
            _lastEquipped = null;
        }
        _isActive  = false;
        _remaining = 0;
        RestoreColors();
        Refresh();
    }

    private void TryConsumeNext()
    {
        if (!_isActive) return;

        var rift = InventorySystem.Instance?.SelectedRift;
        var inv  = DimensionStoneInventory.Instance;
        if (rift == null || inv == null) { Stop(); return; }
        if (_remaining <= 0 || inv.Count <= 0) { Stop(); return; }

        var stone = inv.Stones[0];
        _lastEquipped = stone;
        _remaining--;

        DimensionStoneSlot.EquipToRift(rift, stone);

        bool started = rift.OpenRift();
        if (!started) Stop();
    }

    private void HandleRiftSelected(RiftGenerator rift)
    {
        if (rift == null && _isActive) Stop();
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
