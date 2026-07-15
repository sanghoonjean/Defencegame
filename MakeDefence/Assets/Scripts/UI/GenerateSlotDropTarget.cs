using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// GenerateSlot 에 부착. 두 가지 역할:
/// - 드롭 존: 인벤 슬롯(Stone 페이로드) 드래그를 받아 WaveGeneratorSystem 에 swap 장착
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

        // 장착 차원석 호버 툴팁 (#402) — 호버 시점에 라이브 조회
        var tooltip = gameObject.GetComponent<ItemTooltipTrigger>()
                   ?? gameObject.AddComponent<ItemTooltipTrigger>();
        tooltip.TextSource = () =>
        {
            var stone = WaveGeneratorSystem.Instance != null ? WaveGeneratorSystem.Instance.LoadedStone : null;
            return stone != null ? ItemTooltipTrigger.BuildStoneText(stone) : null;
        };
    }

    private void OnEnable()
    {
        WaveGeneratorSystem.OnStoneChanged += Refresh;
    }

    private void OnDisable()
    {
        WaveGeneratorSystem.OnStoneChanged -= Refresh;
    }

    // Start 는 씬 전체의 Awake 가 끝난 뒤 실행되므로, 계층 순서와 무관하게
    // WaveGeneratorSystem.Instance 가 이미 준비된 상태에서 최초 평가가 이뤄진다.
    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (iconImage == null) return;
        var generator = WaveGeneratorSystem.Instance;
        var stone = generator != null ? generator.LoadedStone : null;
        if (stone != null)
        {
            if (_cachedSprite != null) iconImage.sprite = _cachedSprite;
            iconImage.color = filledColor;
        }
        else
        {
            iconImage.color = emptyColor;
        }

        // 차원석 등급 배지 (#394) — 비어 있으면 숨김
        StoneGradeBadge.Set(iconImage, stone);
    }

    // --- Drop (InvenSlot[Stone payload] → GenerateSlot) ---

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var source = eventData.pointerDrag.GetComponent<InvenSlotDragHandler>();
        if (source == null || source.Stone == null) return;

        var stone = source.Stone;

        if (WaveGeneratorSystem.Instance == null)
        {
            Debug.Log("[GenerateSlotDropTarget] WaveGeneratorSystem 없음 — 드롭 무시");
            return;
        }

        var srcSprite = source.Icon;
        if (srcSprite != null) _cachedSprite = srcSprite;

        InventorySystem.EquipStone(stone);
    }

    // --- Drag (GenerateSlot → 인벤 패널 회수) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        var generator = WaveGeneratorSystem.Instance;
        if (generator == null || generator.LoadedStone == null) { eventData.pointerDrag = null; return; }

        if (_rootCanvas == null)
        {
            var c = GetComponentInParent<Canvas>();
            if (c != null) _rootCanvas = c.rootCanvas;
        }
        if (_rootCanvas == null) { eventData.pointerDrag = null; return; }

        _draggingStone = generator.LoadedStone;

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
