using UnityEngine;

public class UnitPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panel;

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
        if (panel != null)
            panel.SetActive(tower != null);
    }
}
