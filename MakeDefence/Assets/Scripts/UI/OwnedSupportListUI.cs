using UnityEngine;

public class OwnedSupportListUI : MonoBehaviour
{
    [SerializeField] private OwnedSupportSlotUI slotPrefab;
    [SerializeField] private Transform          container;

    private void OnEnable()
    {
        ShopSystem.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        ShopSystem.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (container == null || slotPrefab == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (ShopSystem.Instance == null) return;

        foreach (var option in ShopSystem.Instance.OwnedSupports)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.Setup(option);
        }
    }
}
