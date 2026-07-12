using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 패널을 드래그로 이동 가능하게 만든다.
/// 패널 루트(배경 Image 를 가진 오브젝트, Raycast Target 켜짐)에 부착한다.
///
/// 배경 또는 상단 헤더바(TXTPanel)를 눌러 드래그하면 패널 전체가 이동한다.
/// 슬롯처럼 자체 드래그 핸들러(예: InvenSlotDragHandler)를 가진 자식 위에서
/// 시작한 드래그는 EventSystem 이 가장 안쪽 핸들러로 라우팅하므로 아이템 드래그가
/// 그대로 동작하고 패널은 움직이지 않는다. IDropHandler(ShopDropHandler /
/// InvenDropHandler) 는 드래그를 시작하지 않으므로 충돌하지 않는다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Tooltip("이동시킬 대상. 비워두면 이 오브젝트의 RectTransform 을 이동한다.")]
    [SerializeField] private RectTransform _target;

    [Tooltip("드래그 시작 시 형제 중 맨 앞(최상단)으로 가져온다.")]
    [SerializeField] private bool _bringToFront = true;

    [Tooltip("패널이 부모(캔버스) 영역 안에 머물도록 위치를 제한한다.")]
    [SerializeField] private bool _clampToParent = true;

    private RectTransform _parentRect;
    private Vector2       _pointerOffset;

    private readonly Vector3[] _selfCorners   = new Vector3[4];
    private readonly Vector3[] _parentCorners = new Vector3[4];

    private void Awake()
    {
        if (_target == null) _target = (RectTransform)transform;
        _parentRect = _target.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_parentRect == null) return;
        if (_bringToFront) _target.SetAsLastSibling();

        // 포인터와 패널 위치의 차이를 부모 로컬 좌표에서 기록해 드래그 중 일정하게 유지한다.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect, eventData.position, eventData.pressEventCamera, out var pointerLocal);
        _pointerOffset = _target.anchoredPosition - pointerLocal;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_parentRect == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRect, eventData.position, eventData.pressEventCamera, out var pointerLocal))
            return;

        _target.anchoredPosition = pointerLocal + _pointerOffset;
        if (_clampToParent) ClampToParent();
    }

    // 월드 코너 기준으로 부모 밖으로 벗어난 만큼만 되돌린다 (앵커/피벗 설정과 무관하게 동작).
    private void ClampToParent()
    {
        _parentRect.GetWorldCorners(_parentCorners); // 0 = 좌하단, 2 = 우상단
        _target.GetWorldCorners(_selfCorners);

        float selfW   = _selfCorners[2].x   - _selfCorners[0].x;
        float selfH   = _selfCorners[2].y   - _selfCorners[0].y;
        float parentW = _parentCorners[2].x - _parentCorners[0].x;
        float parentH = _parentCorners[2].y - _parentCorners[0].y;

        Vector3 push = Vector3.zero;

        // 패널이 부모보다 큰 축은 제한하지 않는다 (양쪽 밀림이 상쇄되어 떨리는 것을 방지).
        if (selfW <= parentW)
        {
            if (_selfCorners[0].x < _parentCorners[0].x)      push.x = _parentCorners[0].x - _selfCorners[0].x;
            else if (_selfCorners[2].x > _parentCorners[2].x) push.x = _parentCorners[2].x - _selfCorners[2].x;
        }
        if (selfH <= parentH)
        {
            if (_selfCorners[0].y < _parentCorners[0].y)      push.y = _parentCorners[0].y - _selfCorners[0].y;
            else if (_selfCorners[2].y > _parentCorners[2].y) push.y = _parentCorners[2].y - _selfCorners[2].y;
        }

        if (push != Vector3.zero) _target.position += push;
    }
}
