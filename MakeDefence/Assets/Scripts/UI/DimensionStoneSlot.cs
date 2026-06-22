using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 1칸. 클릭 시 SelectedRift 가 있으면 그 균열에 장착.
/// 기존 LoadedStone 이 있으면 swap (회수 후 새 stone 장착) — Codex P2 반영.
///
/// 슬롯의 시각화는 자식 ICON Image 로 처리 (InvenUI 패턴).
/// 슬롯 GO 자체의 Image (배경) 는 사용자 디자인 그대로 두고 만지지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
public class DimensionStoneSlot : MonoBehaviour
{
    [Tooltip("차원석 아이콘 표시 Image. 비워두면 자식 'ICON' Image 를 자동 탐색.")]
    [SerializeField] private Image iconImage;

    [SerializeField] private Color emptyColor  = new(1f, 1f, 1f, 0f); // 빈 슬롯은 투명
    [SerializeField] private Color filledColor = new(0.55f, 0.2f, 0.85f, 1f); // 진단용 보라 — 채워진 슬롯이 시각적으로 두드러지게

    private Button _button;
    private DimensionStone _bound;

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
        if (DimensionStoneInventory.Instance == null) return;

        // swap 패턴 — 기존 stone 인벤 회수 후 새 stone 장착 (소실 방지)
        if (rift.LoadedStone != null)
        {
            DimensionStoneInventory.Instance.Add(rift.LoadedStone);
            rift.ClearStone();
        }
        DimensionStoneInventory.Instance.Remove(clicked);
        rift.SetStone(clicked);
    }
}
