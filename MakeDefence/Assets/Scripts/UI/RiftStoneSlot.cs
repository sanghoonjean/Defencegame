using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 차원석 인벤토리 슬롯 1칸.
/// - 비어있으면 어두운 배경, 차원석이 있으면 보라색 + 클릭 가능
/// </summary>
[RequireComponent(typeof(Button), typeof(Image))]
public class RiftStoneSlot : MonoBehaviour
{
    [SerializeField] private Color emptyColor  = new Color(0.18f, 0.12f, 0.08f, 1f);
    [SerializeField] private Color filledColor = new Color(0.55f, 0.2f,  0.85f, 1f);

    private Button _button;
    private Image  _image;
    private DimensionStone _bound;
    private Action<DimensionStone> _onClick;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image  = GetComponent<Image>();
        _button.onClick.AddListener(HandleClick);
    }

    public void Bind(DimensionStone stone, RiftGenerator rift, Action<DimensionStone> onClick)
    {
        _bound   = stone;
        _onClick = onClick;
        if (_image != null) _image.color = stone != null ? filledColor : emptyColor;
        if (_button != null) _button.interactable = stone != null && rift != null;
    }

    private void HandleClick()
    {
        if (_bound != null) _onClick?.Invoke(_bound);
    }
}
