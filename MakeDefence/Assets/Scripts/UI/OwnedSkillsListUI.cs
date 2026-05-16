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
        if (slotPrefab == null)
            Debug.LogError("[OwnedSkillsListUI] slotPrefab이 null — Inspector에서 OwnedSkillSlot 프리팹을 연결하세요");
        if (container == null)
            Debug.LogError("[OwnedSkillsListUI] container가 null — Inspector에서 부모 Transform을 연결하세요");
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

        if (ShopSystem.Instance == null)
        {
            Debug.LogWarning("[OwnedSkillsListUI] Refresh — ShopSystem.Instance가 null");
            return;
        }

        int count = ShopSystem.Instance.OwnedSkills.Count;
        Debug.Log($"[OwnedSkillsListUI] Refresh — 보유 스킬 {count}개 슬롯 생성");
        foreach (var skill in ShopSystem.Instance.OwnedSkills)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.Setup(skill);
        }
    }
}
