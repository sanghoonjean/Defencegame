using UnityEngine;

/// <summary>
/// 인벤토리 패널 내 보유 스킬 목록 컨테이너에 부착.
/// ShopSystem.OnInventoryChanged 구독 후 OwnedSkills 목록을
/// 슬롯 프리팹으로 동적 생성한다.
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
        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (ShopSystem.Instance == null) return;

        foreach (var skill in ShopSystem.Instance.OwnedSkills)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.Setup(skill);
        }
    }
}
