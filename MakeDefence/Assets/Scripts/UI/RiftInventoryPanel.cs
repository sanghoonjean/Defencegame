using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 균열 생성기 클릭 시 화면에 표시되는 차원석 인벤토리 패널.
/// - 선택된 RiftGenerator 의 월드 좌표 → 스크린 좌표 변환, 오브젝트 오른쪽에 배치
/// - DimensionStoneInventory 의 보유 차원석을 슬롯 그리드로 표시
/// - 슬롯 클릭 시 해당 차원석을 Rift 에 장착 (이미 장착 중이면 해제 후 장착)
/// </summary>
public class RiftInventoryPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform slotGridContainer;
    [SerializeField] private RiftStoneSlot slotPrefab;

    [Header("배치")]
    [Tooltip("RiftGenerator 의 월드 위치에서 우측으로 떨어진 픽셀 offset")]
    [SerializeField] private Vector2 screenOffset = new Vector2(64f, 0f);

    [Header("슬롯")]
    [Tooltip("표시할 총 슬롯 수 (보유 차원석이 적으면 빈 슬롯, 많으면 잘림)")]
    [SerializeField] private int totalSlots = 11;

    private Camera _mainCam;
    private RiftGenerator _current;
    private readonly System.Collections.Generic.List<RiftStoneSlot> _slots = new();

    private void Awake()
    {
        _mainCam = Camera.main;
        ApplyVisibility(false);
    }

    private void OnEnable()
    {
        InventorySystem.OnRiftSelected += HandleRiftSelected;
        DimensionStoneInventory.OnInventoryChanged += RefreshSlots;
        HandleRiftSelected(InventorySystem.Instance?.SelectedRift);
    }

    private void OnDisable()
    {
        InventorySystem.OnRiftSelected -= HandleRiftSelected;
        DimensionStoneInventory.OnInventoryChanged -= RefreshSlots;
    }

    private void LateUpdate()
    {
        if (_current == null) return;
        UpdatePanelPosition();
    }

    private void HandleRiftSelected(RiftGenerator rift)
    {
        _current = rift;
        bool show = rift != null;
        ApplyVisibility(show);
        if (show)
        {
            UpdatePanelPosition();
            RefreshSlots();
        }
    }

    private void ApplyVisibility(bool show)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = show ? 1f : 0f;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable   = show;
    }

    private void UpdatePanelPosition()
    {
        if (panelRoot == null) return;
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam == null) return;

        Vector3 worldPos = _current.transform.position;
        Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);
        // Screen Space - Overlay 캔버스 기준 RectTransform.position 은 screen 좌표
        panelRoot.position = new Vector3(screenPos.x + screenOffset.x, screenPos.y + screenOffset.y, 0f);
    }

    private void RefreshSlots()
    {
        if (slotGridContainer == null || slotPrefab == null) return;
        if (_current == null) return;

        var inv = DimensionStoneInventory.Instance;
        int ownedCount = inv != null ? inv.Count : 0;

        // 필요한 만큼 슬롯 인스턴스 풀 확보
        while (_slots.Count < totalSlots)
        {
            var slot = Instantiate(slotPrefab, slotGridContainer);
            _slots.Add(slot);
        }
        // 초과분 비활성
        for (int i = totalSlots; i < _slots.Count; i++)
            _slots[i].gameObject.SetActive(false);

        for (int i = 0; i < totalSlots; i++)
        {
            var slot = _slots[i];
            slot.gameObject.SetActive(true);
            DimensionStone stone = (inv != null && i < ownedCount) ? inv.Stones[i] : null;
            slot.Bind(stone, _current, OnSlotClicked);
        }
    }

    private void OnSlotClicked(DimensionStone stone)
    {
        if (_current == null || stone == null) return;
        // 이미 장착된 차원석은 인벤으로 되돌리고 새 차원석을 장착
        if (_current.LoadedStone != null)
        {
            DimensionStoneInventory.Instance?.Add(_current.LoadedStone);
            _current.ClearStone();
        }
        DimensionStoneInventory.Instance?.Remove(stone);
        _current.SetStone(stone);
        RefreshSlots();
    }
}
