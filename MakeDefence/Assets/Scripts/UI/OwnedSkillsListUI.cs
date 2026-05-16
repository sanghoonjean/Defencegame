using UnityEngine;

/// <summary>
/// ShopSystem.OwnedSkills 목록을 동적으로 슬롯 프리팹으로 생성해 표시한다.
/// Inspector에서 slotPrefab(OwnedSkillSlotUI)과 container(스크롤 Content 등)를 연결한다.
/// </summary>
public class OwnedSkillsListUI : MonoBehaviour
{
    [SerializeField] private OwnedSkillSlotUI slotPrefab;
    [SerializeField] private Transform        container;

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
        if (container == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (ShopSystem.Instance == null || slotPrefab == null) return;

        foreach (var skill in ShopSystem.Instance.OwnedSkills)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.Setup(skill);
        }
    }
}
