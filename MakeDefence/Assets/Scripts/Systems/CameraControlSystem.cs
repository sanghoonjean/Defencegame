using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스 우클릭 드래그로 카메라 팬, 휠로 줌. 2D 직교 카메라 전용.
/// Main Camera 에 직접 부착해서 사용.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraControlSystem : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private float zoomMin = 3f;
    [SerializeField] private float zoomMax = 12f;
    [SerializeField] private float zoomStep = 1f;
    [SerializeField] private bool zoomToCursor = true;

    [Header("Pan Clamp (Camera center)")]
    [SerializeField] private Vector2 panMinWorld = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 panMaxWorld = new Vector2(50f, 50f);
    [SerializeField] private bool useMapBoundsIfAvailable = true;

    private Camera _cam;
    private bool _dragging;
    private Vector3 _dragOriginWorld;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic)
        {
            Debug.LogWarning("[CameraControlSystem] Non-orthographic camera detected. Disabling.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (useMapBoundsIfAvailable
            && MapTileSystem.Instance != null
            && MapTileSystem.Instance.TryGetMapWorldBounds(out var b))
        {
            panMinWorld = new Vector2(b.min.x, b.min.y);
            panMaxWorld = new Vector2(b.max.x, b.max.y);
        }
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f)) return;
        if (IsPointerOverUI()) return;

        Vector3 beforeWorld = zoomToCursor ? ScreenToWorld(Input.mousePosition) : transform.position;

        float newSize = Mathf.Clamp(_cam.orthographicSize - scroll * zoomStep, zoomMin, zoomMax);
        if (Mathf.Approximately(newSize, _cam.orthographicSize)) return;
        _cam.orthographicSize = newSize;

        if (zoomToCursor)
        {
            Vector3 afterWorld = ScreenToWorld(Input.mousePosition);
            Vector3 shift = beforeWorld - afterWorld;
            transform.position += new Vector3(shift.x, shift.y, 0f);
        }

        ClampPosition();
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (IsPointerOverUI()) return;
            _dragging = true;
            _dragOriginWorld = ScreenToWorld(Input.mousePosition);
        }
        // GetMouseButtonUp 프레임을 놓쳐도 (윈도우 포커스 전환, 커서가 게임 뷰 이탈 등)
        // 다음 프레임에 RMB 상태로 즉시 드래그 해제
        if (!Input.GetMouseButton(1))
        {
            _dragging = false;
            return;
        }
        if (!_dragging) return;

        Vector3 currentWorld = ScreenToWorld(Input.mousePosition);
        Vector3 delta = _dragOriginWorld - currentWorld;
        if (delta.sqrMagnitude <= 0f) return;
        transform.position += new Vector3(delta.x, delta.y, 0f);
        ClampPosition();
    }

    private void ClampPosition()
    {
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, panMinWorld.x, panMaxWorld.x);
        p.y = Mathf.Clamp(p.y, panMinWorld.y, panMaxWorld.y);
        transform.position = p;
    }

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        screenPos.z = -transform.position.z;
        return _cam.ScreenToWorldPoint(screenPos);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
