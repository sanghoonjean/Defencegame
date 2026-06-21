using UnityEngine;

/// <summary>
/// 균열 생성기 선택 시 차원석 인벤토리 패널을 보이게 한다.
/// 사용자가 직접 디자인한 Canvas/DimensionStonInventoryUI 패널에 부착.
/// SetActive 대신 CanvasGroup.alpha 로 가시성을 토글해 OnEnable/OnDisable
/// 구독 흐름을 잃지 않는다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RiftPanelToggle : MonoBehaviour
{
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        ApplyVisibility(false);
    }

    private void OnEnable()
    {
        InventorySystem.OnRiftSelected += HandleRiftSelected;
        HandleRiftSelected(InventorySystem.Instance?.SelectedRift);
    }

    private void OnDisable()
    {
        InventorySystem.OnRiftSelected -= HandleRiftSelected;
    }

    private void HandleRiftSelected(RiftGenerator rift)
    {
        ApplyVisibility(rift != null);
    }

    private void ApplyVisibility(bool show)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha          = show ? 1f : 0f;
        _canvasGroup.blocksRaycasts = show;
        _canvasGroup.interactable   = show;
    }
}
