using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사용자가 디자인한 ScrollRect Content 에 부착해 차원석 인벤토리 슬롯
/// 그리드를 표시한다. DimensionStoneInventory.OnInventoryChanged 구독.
/// 보유 차원석 1개당 슬롯 1개 (1:1). 초과 슬롯은 비활성.
/// </summary>
public class DimensionStoneInventoryView : MonoBehaviour
{
    [Tooltip("슬롯을 자식으로 만들 컨테이너. 비워두면 자기 자신.")]
    [SerializeField] private RectTransform slotContainer;

    [Tooltip("슬롯 1칸 prefab.")]
    [SerializeField] private DimensionStoneSlot slotPrefab;

    private readonly List<DimensionStoneSlot> _slots = new();

    private bool _designTimeChildrenCleaned;

    private void Awake()
    {
        if (slotContainer == null) slotContainer = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        DimensionStoneInventory.OnInventoryChanged += Rebuild;
        CleanupDesignTimeChildren();
        Rebuild();
    }

    private void CleanupDesignTimeChildren()
    {
        if (_designTimeChildrenCleaned || slotContainer == null) return;
        _designTimeChildrenCleaned = true;
        // OnEnable 시점에 _slots 는 비어있으므로 자식 전체가 디자인 시점 placeholder
        for (int i = slotContainer.childCount - 1; i >= 0; i--)
            Destroy(slotContainer.GetChild(i).gameObject);
    }

    private void OnDisable()
    {
        DimensionStoneInventory.OnInventoryChanged -= Rebuild;
    }

    private void Rebuild()
    {
        if (slotContainer == null || slotPrefab == null) return;

        var inv = DimensionStoneInventory.Instance;
        int count = inv != null ? inv.Count : 0;

        while (_slots.Count < count)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            _slots.Add(slot);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < count)
            {
                _slots[i].gameObject.SetActive(true);
                _slots[i].Bind(inv.Stones[i]);
            }
            else
            {
                _slots[i].gameObject.SetActive(false);
            }
        }
    }
}
