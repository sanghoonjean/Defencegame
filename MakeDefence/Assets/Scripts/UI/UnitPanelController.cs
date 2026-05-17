using UnityEngine;

public class UnitPanelController : MonoBehaviour
{
    private void OnEnable()
    {
        InventorySystem.OnTowerSelected += OnTowerSelected;
        gameObject.SetActive(InventorySystem.Instance?.SelectedTower != null);
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= OnTowerSelected;
    }

    private void OnTowerSelected(Tower tower)
    {
        gameObject.SetActive(tower != null);
    }
}
