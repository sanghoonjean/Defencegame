using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 1칸. 클릭/드래그-드롭 두 가지 경로로 SelectedRift 에 장착한다.
/// 기존 LoadedStone 이 있으면 swap (회수 후 새 stone 장착) — Codex P2 반영.
///
/// 슬롯의 시각화는 자식 ICON Image 로 처리 (InvenUI 패턴).
/// 슬롯 GO 자체의 Image (배경) 는 사용자 디자인 그대로 두고 만지지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
public class DimensionStoneSlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("차원석 아이콘 표시 Image. 비워두면 자식 'ICON' Image 를 자동 탐색.")]
    [SerializeField] private Image iconImage;

    [SerializeField] private Color emptyColor  = new(1f, 1f, 1f, 0f); // 빈 슬롯은 투명
    [SerializeField] private Color filledColor = new(0.55f, 0.2f, 0.85f, 1f); // 진단용 보라 — 채워진 슬롯이 시각적으로 두드러지게

    [Tooltip("드래그 ghost 의 크기.")]
    [SerializeField] private Vector2 dragGhostSize = new(60f, 60f);

    private Button _button;
    private DimensionStone _bound;
    private Canvas _rootCanvas;
    private Image  _ghost;

    /// <summary>드롭 타깃이 페이로드를 읽기 위해 노출. 드래그 중에만 의미 있음.</summary>
    public DimensionStone Stone => _bound;

    /// <summary>드롭 타깃에 sprite 를 이어주기 위해 노출.</summary>
    public Sprite IconSprite => iconImage != null ? iconImage.sprite : null;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
        if (iconImage == null)
        {
            var iconTr = transform.Find("ICON");
            if (iconTr != null) iconImage = iconTr.GetComponent<Image>();
        }
        if (iconImage == null)
            Debug.LogWarning($"[DimensionStoneSlot] iconImage 자동 탐색 실패 — '{name}' 에 자식 'ICON' Image 없음");
    }

    public void Bind(DimensionStone stone)
    {
        _bound = stone;
        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.color   = stone != null ? filledColor : emptyColor;
        }
        if (_button != null)
            _button.interactable = stone != null;
    }

    private void OnClick()
    {
        // 클릭한 stone 을 local 로 캐싱 — Add/Remove 가 OnInventoryChanged 를 발행하면
        // DimensionStoneInventoryView.Rebuild 가 같은 슬롯에 다른 stone 을 Bind 할 수 있어
        // _bound 가 바뀐 채로 SetStone 이 잘못된 stone 을 장착하는 race 회피 (Codex P1).
        var clicked = _bound;
        if (clicked == null) return;
        var rift = InventorySystem.Instance?.SelectedRift;
        if (rift == null) return;
        EquipToRift(rift, clicked);
    }

    /// <summary>
    /// 인벤에서 stone 을 빼서 rift 에 장착. 기존 LoadedStone 은 인벤으로 회수 (swap).
    /// 클릭/드래그-드롭 양쪽에서 공유.
    /// </summary>
    public static bool EquipToRift(RiftGenerator rift, DimensionStone stone)
    {
        if (rift == null || stone == null) return false;
        if (DimensionStoneInventory.Instance == null) return false;

        // swap 패턴 — 기존 stone 인벤 회수 후 새 stone 장착 (소실 방지)
        if (rift.LoadedStone != null)
        {
            DimensionStoneInventory.Instance.Add(rift.LoadedStone);
            rift.ClearStone();
        }
        DimensionStoneInventory.Instance.Remove(stone);
        rift.SetStone(stone);
        return true;
    }

    // --- Drag (DimensionStoneSlot → GenerateSlot) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_bound == null) { eventData.pointerDrag = null; return; }

        if (_rootCanvas == null)
        {
            var c = GetComponentInParent<Canvas>();
            if (c != null) _rootCanvas = c.rootCanvas;
        }
        if (_rootCanvas == null) { eventData.pointerDrag = null; return; }

        var go = new GameObject("DimensionStoneDragGhost");
        go.transform.SetParent(_rootCanvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghost               = go.AddComponent<Image>();
        _ghost.sprite        = iconImage != null ? iconImage.sprite : null;
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
        // 시각 복구는 Bind 가 다시 호출되면 알아서 정리되지만, drop 이 실패하면 Rebuild 가
        // 안 일어날 수 있어 여기서 명시적으로 색을 원상복구.
        if (iconImage != null)
            iconImage.color = _bound != null ? filledColor : emptyColor;
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
