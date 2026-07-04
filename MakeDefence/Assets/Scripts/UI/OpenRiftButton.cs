using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 패널의 "웨이브 생성" 버튼.
/// 클릭 → WaveGeneratorSystem.OpenRift() (장착 차원석 소모 + RiftWave 시작).
/// LoadedStone / WaveSystem / GameState 변화 → Button.interactable 자동 갱신.
/// </summary>
[RequireComponent(typeof(Button))]
public class OpenRiftButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        WaveGeneratorSystem.OnStoneChanged += Refresh;
        WaveSystem.OnWaveStarted         += HandleWaveStarted;
        WaveSystem.OnWaveEnded           += HandleWaveEnded;
        GameStateSystem.OnStateChanged   += HandleStateChanged;
    }

    // Start 는 씬 전체의 Awake 가 끝난 뒤 실행되므로, 계층 순서와 무관하게
    // WaveGeneratorSystem.Instance 가 이미 준비된 상태에서 최초 평가가 이뤄진다.
    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        WaveGeneratorSystem.OnStoneChanged -= Refresh;
        WaveSystem.OnWaveStarted         -= HandleWaveStarted;
        WaveSystem.OnWaveEnded           -= HandleWaveEnded;
        GameStateSystem.OnStateChanged   -= HandleStateChanged;
    }

    private void HandleWaveStarted(int _) => Refresh();
    private void HandleWaveEnded(bool _) => Refresh();
    private void HandleStateChanged(GameState _) => Refresh();

    private void Refresh()
    {
        if (_button == null) return;
        var generator = WaveGeneratorSystem.Instance;
        _button.interactable =
            generator != null
            && generator.LoadedStone != null
            && WaveSystem.Instance != null && !WaveSystem.Instance.IsWaveActive
            && GameStateSystem.Current == GameState.Playing;
    }

    private void OnClick()
    {
        WaveGeneratorSystem.Instance?.OpenRift();
    }
}
