using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CanvasGroup 기반 패널 토글 버튼. UIToggleButton(GameObject.SetActive) 과 달리
/// 패널 GameObject 는 항상 active 상태를 유지하고 alpha/interactable/blocksRaycasts 만 바꾼다.
/// 패널 내부에 "숨겨져도 계속 동작해야 하는" 로직(OnEnable 구독 등)이 있을 때 사용 —
/// 예: DimesionStoneInventoryUI 의 RepeatGenerateToggleButton 은 패널이 닫혀도
/// WaveSystem.OnWaveEnded 구독을 유지해야 연속 생성 루프가 끊기지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
public class CanvasGroupToggleButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup targetGroup;

    private bool _visible;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
        if (targetGroup != null) _visible = targetGroup.alpha > 0f;
    }

    private void Toggle()
    {
        if (targetGroup == null)
        {
            Debug.LogWarning($"[CanvasGroupToggleButton] targetGroup이 연결되지 않았습니다 — {gameObject.name}");
            return;
        }
        _visible = !_visible;
        ApplyVisibility(_visible);
    }

    private void ApplyVisibility(bool visible)
    {
        targetGroup.alpha          = visible ? 1f : 0f;
        targetGroup.interactable   = visible;
        targetGroup.blocksRaycasts = visible;
    }
}
