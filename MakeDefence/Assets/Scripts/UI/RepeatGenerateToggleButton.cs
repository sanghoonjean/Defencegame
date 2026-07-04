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

    private void Awake()
    {
        _button = GetComponent<Button>();
        _originalColors = _button.colors;
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        WaveSystem.OnWaveStarted       += HandleWaveStarted;
        WaveSystem.OnWaveEnded         += HandleWaveEnded;
        GameStateSystem.OnStateChanged += HandleStateChanged;
        PauseSystem.OnPauseChanged     += HandlePauseChanged;
        ShopSystem.OnInventoryChanged  += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        WaveSystem.OnWaveStarted       -= HandleWaveStarted;
        WaveSystem.OnWaveEnded         -= HandleWaveEnded;
        GameStateSystem.OnStateChanged -= HandleStateChanged;
        PauseSystem.OnPauseChanged     -= HandlePauseChanged;
        ShopSystem.OnInventoryChanged  -= Refresh;
        RestoreColors();
    }

    private void OnClick()
    {
        if (_isActive) Stop();
        else           BeginRepeat();
    }

    private void BeginRepeat()
    {
        var generator = WaveGeneratorSystem.Instance;
        var shop = ShopSystem.Instance;
        if (generator == null || shop == null || shop.OwnedStones.Count <= 0) return;
        if (WaveSystem.Instance == null || WaveSystem.Instance.IsWaveActive) return;
        if (GameStateSystem.Current != GameState.Playing) return;

        _isActive = true;
        ApplyActiveColors();
        Refresh();
        TryConsumeNext();
    }

    private void Stop()
    {
        var generator = WaveGeneratorSystem.Instance;
        if (_lastEquipped != null && generator != null
            && generator.LoadedStone == _lastEquipped)
        {
            ShopSystem.Instance?.AddStone(generator.LoadedStone);
            generator.ClearStone();
        }
        _lastEquipped = null;
        _isActive  = false;
        RestoreColors();
        Refresh();
    }

    private void TryConsumeNext()
    {
        if (!_isActive) return;

        var generator = WaveGeneratorSystem.Instance;
        var shop = ShopSystem.Instance;
        if (generator == null || shop == null) { Stop(); return; }
        if (shop.OwnedStones.Count <= 0) { Stop(); return; }

        var stone = shop.OwnedStones[0];
        _lastEquipped = stone;

        InventorySystem.EquipStone(stone);

        bool started = generator.OpenRift();
        if (!started) Stop();
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

        var shop = ShopSystem.Instance;
        _button.interactable =
            WaveGeneratorSystem.Instance != null
            && shop != null && shop.OwnedStones.Count > 0
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
