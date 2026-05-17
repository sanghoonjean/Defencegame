using UnityEngine;

public class UnitPanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void OnEnable()
    {
        InventorySystem.OnTowerSelected += OnTowerSelected;
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= OnTowerSelected;
    }

    private void OnTowerSelected(Tower tower)
    {
        if (canvasGroup == null) return;
        bool show = tower != null;
        canvasGroup.alpha          = show ? 1f : 0f;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable   = show;
    }
}
