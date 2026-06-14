using UnityEngine;

/// <summary>
/// 인벤토리 표시/식별 공통 추상화 (#236).
/// SkillData / SupportOptionData 가 구현해 ShopSystem 및 인벤 UI 가
/// 단일 시퀀스로 순회 가능하도록 한다.
/// </summary>
public interface IInventoryItem
{
    string DisplayName { get; }
    Sprite Icon        { get; }
    InventoryItemKind Kind { get; }
}
