using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameUIManager 가 IMGUI 로 그리는 월드 바(적 HP / 타워 MP)를 가려야 하는
/// UI 패널에 부착하는 등록용 컴포넌트. 활성화된 동안 정적 레지스트리에 등록되고,
/// 자신의 스크린 Rect(GUI 좌표계)를 노출한다.
///
/// - SetActive 토글 패널: OnEnable/OnDisable 등록·해제로 자동 처리
/// - CanvasGroup.alpha 로 숨는 패널(Unit_Panel, DimesionStoneInventoryUI):
///   GameObject 가 활성 유지되므로 alpha 임계값으로 가시성 판정
/// - Screen Space Overlay 캔버스 전용 (월드 코너 = 스크린 픽셀 좌표)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIScreenBlocker : MonoBehaviour
{
    private const float AlphaThreshold = 0.01f;

    private static readonly List<UIScreenBlocker> _active = new();

    /// <summary>현재 활성화(등록)된 블로커 목록.</summary>
    public static IReadOnlyList<UIScreenBlocker> Active => _active;

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;

    private readonly Vector3[] _corners = new Vector3[4];
    private Rect _cachedRect;
    private int  _cachedFrame = -1;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _canvasGroup   = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        _active.Add(this);
    }

    private void OnDisable()
    {
        _active.Remove(this);
        _cachedFrame = -1;
    }

    /// <summary>패널이 실제로 보이는 상태인지 (CanvasGroup alpha 숨김 포함).</summary>
    public bool IsVisible => _canvasGroup == null || _canvasGroup.alpha > AlphaThreshold;

    /// <summary>
    /// GUI 좌표계(좌상단 원점, y 아래로 증가) 스크린 Rect.
    /// 프레임당 1회만 계산하고 캐싱한다.
    /// </summary>
    public Rect GetGUIRect()
    {
        if (Time.frameCount == _cachedFrame) return _cachedRect;
        _cachedFrame = Time.frameCount;

        // Screen Space Overlay: 월드 코너가 곧 스크린 픽셀 좌표
        // corners[0] = 좌하단, corners[2] = 우상단
        _rectTransform.GetWorldCorners(_corners);
        float xMin = _corners[0].x;
        float yMin = _corners[0].y;
        float xMax = _corners[2].x;
        float yMax = _corners[2].y;

        _cachedRect = new Rect(xMin, Screen.height - yMax, xMax - xMin, yMax - yMin);
        return _cachedRect;
    }
}
