using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 1칸. 클릭 시 SelectedRift 가 있으면 그 균열에 장착.
/// 기존 LoadedStone 이 있으면 swap (회수 후 새 stone 장착) — Codex P2 반영.
/// Bind() 가 색/활성을 매번 덮어써 prefab instance override 의 alpha 0
/// 같은 잔존 값도 안정적으로 무시한다.
/// </summary>
[RequireComponent(typeof(Button), typeof(Image))]
public class DimensionStoneSlot : MonoBehaviour
{
    [SerializeField] private Color emptyColor  = new(0.2f,  0.2f, 0.2f,  1f);
    [SerializeField] private Color filledColor = new(0.55f, 0.2f, 0.85f, 1f);

    private Button _button;
    private Image  _image;
    private DimensionStone _bound;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image  = GetComponent<Image>();
        _button.onClick.AddListener(OnClick);
    }

    public void Bind(DimensionStone stone)
    {
        _bound = stone;
        if (_image != null)
        {
            _image.enabled = true;
            _image.color   = stone != null ? filledColor : emptyColor;
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
