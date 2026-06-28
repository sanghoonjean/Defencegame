using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// GenerateSlot 에 부착. 두 가지 역할:
/// - 드롭 존: 인벤 슬롯(Stone 페이로드) 드래그를 받아 SelectedRift 에 swap 장착
/// - 드래그 소스: 이미 장착된 stone 을 인벤토리 영역으로 끌어 회수
///
/// 시각화: 자식 ICON Image 에 sprite + 색을 채워 표시. sprite 는 드롭한 슬롯에서 이어받아 캐시.
///
/// 이 GO 에는 raycastTarget = true 인 Graphic 이 있어야 OnDrop / OnBeginDrag 가 호출된다.
/// </summary>
public class GenerateSlotDropTarget : MonoBehaviour,
    IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("장착된 차원석 아이콘 표시 Image. 비워두면 자식 'ICON' Image 를 자동 탐색.")]
    [SerializeField] private Image iconImage;

    [Tooltip("기본 차원석 sprite. 드롭으로 한 번이라도 sprite 가 들어오면 이후엔 캐시 사용.")]
    [SerializeField] private Sprite defaultStoneSprite;

    [SerializeField] private Color emptyColor  = new(1f, 1f, 1f, 0f);
    [SerializeField] private Color filledColor = new(1f, 1f, 1f, 1f);

    [Tooltip("드래그 ghost 의 크기.")]
    [SerializeField] private Vector2 dragGhostSize = new(60f, 60f);

    private RiftGenerator _current;
    private Sprite _cachedSprite;

    private Canvas _rootCanvas;
    private Image  _ghost;
    private DimensionStone _draggingStone;

    /// <summary>인벤 드롭 타깃이 페이로드(현재 장착 stone)를 읽기 위해 노출.</summary>
    public DimensionStone DraggingStone => _draggingStone;

    private void Awake()
    {
        if (iconImage == null)
        {
            var iconTr = transform.Find("ICON");
            if (iconTr != null) iconImage = iconTr.GetComponent<Image>();
        }
        if (_cachedSprite == null && iconImage != null && iconImage.sprite != null)
            _cachedSprite = iconImage.sprite;
        if (_cachedSprite == null && defaultStoneSprite != null)
            _cachedSprite = defaultStoneSprite;
    }

    private void OnEnable()
    {
        InventorySystem.OnRiftSelected += HandleRiftSelected;
        HandleRiftSelected(InventorySystem.Instance?.SelectedRift);
    }

    private void OnDisable()
    {
        InventorySystem.OnRiftSelected -= HandleRiftSelected;
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

    private void Refresh()
    {
        if (iconImage == null) return;
        var stone = _current != null ? _current.LoadedStone : null;
        if (stone != null)
        {
            if (_cachedSprite != null) iconImage.sprite = _cachedSprite;
            iconImage.color = filledColor;
        }
        else
        {
            iconImage.color = emptyColor;
        }
    }

    // --- Drop (InvenSlot[Stone payload] → GenerateSlot) ---

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var source = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (source == null || source.Stone == null) return;

        var stone = source.Stone;

        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null)
        {
            Debug.Log("[GenerateSlotDropTarget] SelectedRift 없음 — 드롭 무시");
            return;
        }

        var srcSprite = source.Icon;
        if (srcSprite != null) _cachedSprite = srcSprite;

        InventorySystem.EquipStoneToRift(rift, stone);
    }

    // --- Drag (GenerateSlot → 인벤 패널 회수) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null || rift.LoadedStone == null) { eventData.pointerDrag = null; return; }

        if (_rootCanvas == null)
        {
            var c = GetComponentInParent<Canvas>();
            if (c != null) _rootCanvas = c.rootCanvas;
        }
        if (_rootCanvas == null) { eventData.pointerDrag = null; return; }

        _draggingStone = rift.LoadedStone;

        var go = new GameObject("DimensionStoneDragGhost");
        go.transform.SetParent(_rootCanvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghost               = go.AddComponent<Image>();
        _ghost.sprite        = _cachedSprite != null
            ? _cachedSprite
            : (iconImage != null ? iconImage.sprite : null);
        _ghost.color         = filledColor;
        _ghost.raycastTarget = false;
        _ghost.rectTransform.sizeDelta = dragGhostSize;

        MoveGhost(eventData);

        if (iconImage != null)
        {
            var c = iconImage.color; c.a *= 0.3f;
            iconImage.color = c;
        }
    }

    public void OnDrag(PointerEventData eventData) => MoveGhost(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghost != null) { Destroy(_ghost.gameObject); _ghost = null; }
        _draggingStone = null;
        // 드롭이 성공해서 stone 이 빠졌다면 OnStoneChanged → Refresh 가 emptyColor 로 정리.
        // 실패 (drop 무효 영역) 면 여기서 시각 복구.
        Refresh();
    }

    private void MoveGhost(PointerEventData eventData)
    {
        if (_ghost == null || _rootCanvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var local);
        _ghost.rectTransform.localPosition = local;
    }
}
