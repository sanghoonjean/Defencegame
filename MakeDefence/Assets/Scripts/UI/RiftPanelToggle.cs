using UnityEngine;

/// <summary>
/// 균열 생성기 선택 시 차원석 인벤토리 패널을 보이게 + 균열 오브젝트
/// 바로 오른쪽으로 따라다니게 한다.
/// 사용자가 디자인한 Canvas/DimesionStoneInventoryUI 같은 패널에 부착.
/// SetActive 대신 CanvasGroup.alpha 로 가시성을 토글해 OnEnable/OnDisable
/// 구독 흐름을 잃지 않는다.
/// </summary>
[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class RiftPanelToggle : MonoBehaviour
{
    [Header("패널 위치 — 균열 우측 픽셀 offset")]
    [Tooltip("균열 화면 좌표에서 패널 중심까지의 픽셀 offset. " +
             "x=양수=오른쪽, y=양수=위, y=음수=아래.")]
    [SerializeField] private Vector2 screenOffset = new Vector2(100f, -150f);

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private RiftGenerator _current;
    private Camera _cam;

    private void Awake()
    {
        _canvasGroup  = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        ApplyVisibility(false);
    }

    private void OnEnable()
    {
        InventorySystem.OnRiftSelected += HandleRiftSelected;
        HandleRiftSelected(InventorySystem.Instance?.SelectedRift);
    }

    private void OnDisable()
    {
        InventorySystem.OnRiftSelected -= HandleRiftSelected;
    }

    private void LateUpdate()
    {
        if (_current == null) return;
        UpdatePanelPosition();
    }

    private void HandleRiftSelected(RiftGenerator rift)
    {
        _current = rift;
        ApplyVisibility(rift != null);
        if (rift != null) UpdatePanelPosition();
    }

    private void UpdatePanelPosition()
    {
        if (_rectTransform == null || _current == null) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector3 worldPos = _current.transform.position;
        Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
        _rectTransform.position = new Vector3(
            screenPos.x + screenOffset.x,
            screenPos.y + screenOffset.y,
            0f);
    }

    private void ApplyVisibility(bool show)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha          = show ? 1f : 0f;
        _canvasGroup.blocksRaycasts = show;
        _canvasGroup.interactable   = show;
    }
}
