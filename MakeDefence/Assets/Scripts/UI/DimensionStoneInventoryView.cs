using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사용자가 디자인한 ScrollRect Content 에 부착해 차원석 인벤토리 슬롯
/// 그리드를 표시한다. DimensionStoneInventory.OnInventoryChanged 구독.
///
/// 인스펙터에서 미리 만든 자식 DimensionStoneSlot 들을 풀로 등록해 활용하고,
/// 보유 차원석이 풀보다 많으면 slotPrefab 으로 추가 인스턴스. 인벤 수만큼
/// 활성 + 나머지 비활성 (InvenUI 패턴).
/// </summary>
public class DimensionStoneInventoryView : MonoBehaviour
{
    [Tooltip("슬롯을 자식으로 만들 컨테이너. 비워두면 자기 자신.")]
    [SerializeField] private RectTransform slotContainer;

    [Tooltip("동적 추가용 슬롯 prefab. 인스펙터에서 미리 만든 자식만 쓰려면 null 도 가능.")]
    [SerializeField] private DimensionStoneSlot slotPrefab;

    private readonly List<DimensionStoneSlot> _slots = new();

    private void Awake()
    {
        if (slotContainer == null) slotContainer = GetComponent<RectTransform>();
        // 인스펙터에서 추가한 기존 자식 슬롯을 풀로 등록 (Destroy 하지 않는다)
        foreach (Transform child in slotContainer)
        {
            var slot = child.GetComponent<DimensionStoneSlot>();
            if (slot != null) _slots.Add(slot);
        }
    }

    private void OnEnable()
    {
        DimensionStoneInventory.OnInventoryChanged += Rebuild;
        Rebuild();
    }

    private void OnDisable()
    {
        DimensionStoneInventory.OnInventoryChanged -= Rebuild;
    }

    private void Rebuild()
    {
        if (slotContainer == null) return;

        var inv = DimensionStoneInventory.Instance;
        int count = inv != null ? inv.Count : 0;

        // 풀이 부족하면 slotPrefab 으로 채움 (prefab 미할당이면 풀 크기 그대로)
        while (_slots.Count < count && slotPrefab != null)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            _slots.Add(slot);
        }

        // 사용자가 인스펙터에서 만든 슬롯의 SetActive 는 만지지 않는다 (InvenUI 패턴).
        // 시각화는 Bind 가 자식 ICON Image 의 색으로만 처리 — 채움=흰색, 빈=투명.
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].Bind(i < count ? inv.Stones[i] : null);
    }
}
