using System;
using UnityEngine;

/// <summary>
/// 선택된 타워의 스킬/보조옵션/아이템 슬롯을 통합 관리한다.
/// UI는 이 시스템을 통해 현재 선택 타워에 접근한다.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    public static event Action<Tower> OnTowerSelected;

    public Tower SelectedTower { get; private set; }

    private void Awake() { Instance = this; }

    public void SelectTower(Tower tower)
    {
        SelectedTower = tower;
        OnTowerSelected?.Invoke(tower);
    }

    public void Deselect()
    {
        bool changedTower = SelectedTower != null;
        SelectedTower = null;
        if (changedTower) OnTowerSelected?.Invoke(null);
    }

    /// <summary>
    /// 타워 삭제 시 환급/회수 카운트.
    /// LowerCubes = 배치비(1) + 아이템 슬롯당 1.
    /// </summary>
    public readonly struct DeleteRefundSummary
    {
        public readonly bool SkillReturned;
        public readonly int  SupportReturned;
        public readonly int  ItemsSold;
        public readonly int  LowerCubes;

        public DeleteRefundSummary(bool skillReturned, int supportReturned, int itemsSold)
        {
            SkillReturned   = skillReturned;
            SupportReturned = supportReturned;
            ItemsSold       = itemsSold;
            LowerCubes      = itemsSold + 1;
        }
    }

    /// <summary>
    /// 삭제 시 회수/판매될 항목을 미리 카운트한다. (팝업 메시지 구성용)
    /// </summary>
    public static DeleteRefundSummary BuildDeleteSummary(Tower target)
    {
        if (target == null) return new DeleteRefundSummary(false, 0, 0);

        bool skill = target.EquippedSkill != null && !target.IsDefaultSkillEquipped;

        int supportCount = 0;
        int unlocked = target.UnlockedSupportSlots;
        for (int i = 0; i < unlocked; i++)
        {
            if (target.SupportOptions[i] != null) supportCount++;
        }

        int items = 0;
        if (ItemSystem.Instance != null)
        {
            int unlockedItemSlots = ItemSystem.Instance.GetUnlockedSlotCount(target);
            for (int i = 0; i < unlockedItemSlots; i++)
            {
                if (ItemSystem.Instance.GetItem(target, i) != null) items++;
            }
        }

        return new DeleteRefundSummary(skill, supportCount, items);
    }

    /// <summary>
    /// 명시적으로 지정한 타워를 삭제하고 장착물을 회수/판매한다.
    /// - 스킬 / 보조 옵션 → ShopSystem 인벤토리 복귀
    /// - 아이템 슬롯 → 슬롯당 Lower 1개 자동 판매
    /// - 배치 비용 → Lower 1개 환급
    /// 팝업 오픈 시점에 캡처한 타워를 그대로 전달받으므로,
    /// 그 사이 SelectedTower 가 다른 타워로 바뀌어도 안전하다.
    /// </summary>
    public bool DeleteTower(Tower target)
    {
        // Unity 의 == 오버로드가 Destroy 예약된 객체도 null 로 판별
        if (target == null) return false;

        var summary = BuildDeleteSummary(target);

        // 스킬 인벤 복귀
        if (summary.SkillReturned && ShopSystem.Instance != null)
            ShopSystem.Instance.ReturnSkill(target.EquippedSkill);

        // 보조 옵션 인벤 복귀 (UnlockedSupportSlots 범위까지만)
        if (ShopSystem.Instance != null)
        {
            int unlocked = target.UnlockedSupportSlots;
            for (int i = 0; i < unlocked; i++)
            {
                var opt = target.SupportOptions[i];
                if (opt != null) ShopSystem.Instance.ReturnSupportOption(opt);
            }
        }

        // 캡처된 타워가 현재 선택과 동일할 때만 선택 해제
        if (SelectedTower == target)
            Deselect();

        // 아이템 자동 판매 + 배치비 환급 (Lower = ItemsSold + 1)
        CubeSystem.Instance?.Add(CubeType.Lower, summary.LowerCubes);

        // Tower.OnDestroy() 가 ItemSystem/MapTileSystem 정리를 자동 수행
        UnityEngine.Object.Destroy(target.gameObject);
        return true;
    }

    // --- 스킬 ---
    public bool EquipSkill(SkillData skill)
    {
        if (SelectedTower == null) return false;
        SelectedTower.EquipSkill(skill);
        OnTowerSelected?.Invoke(SelectedTower);
        return true;
    }

    public bool UnequipSkill()
    {
        if (SelectedTower == null) return false;
        SelectedTower.UnequipSkill();
        OnTowerSelected?.Invoke(SelectedTower);
        return true;
    }

    // --- 보조 옵션 ---
    public bool UnlockSupportSlot()
    {
        if (SelectedTower == null) return false;
        bool result = SelectedTower.UnlockSupportSlot();
        if (result) OnTowerSelected?.Invoke(SelectedTower);
        return result;
    }

    public bool UnlockSupportSlot(Tower tower)
    {
        if (tower == null) return false;
        bool result = tower.UnlockSupportSlot();
        if (result)
        {
            SelectedTower = tower;
            OnTowerSelected?.Invoke(tower);
        }
        return result;
    }

    public bool SetSupportOption(int slot, SupportOptionData option)
    {
        if (SelectedTower == null) return false;
        bool result = SelectedTower.SetSupportOption(slot, option);
        if (result) OnTowerSelected?.Invoke(SelectedTower);
        return result;
    }

    // --- 아이템 ---
    public bool UnlockItemSlot()
    {
        if (SelectedTower == null) return false;
        return ItemSystem.Instance.UnlockSlot(SelectedTower);
    }

    public bool ApplyCube(CubeType cube, int slot)
    {
        if (SelectedTower == null) return false;
        return ItemSystem.Instance.ApplyCube(cube, SelectedTower, slot);
    }

    /// <summary>
    /// 인벤(ShopSystem)에서 stone 을 빼서 WaveGeneratorSystem 에 장착. 기존 LoadedStone 은 인벤으로 회수 (swap).
    /// 클릭/드래그-드롭 양쪽에서 공유. (구 DimensionStoneSlot.EquipToRift 후속)
    /// </summary>
    public static bool EquipStone(DimensionStone stone)
    {
        var generator = WaveGeneratorSystem.Instance;
        if (generator == null || stone == null) return false;
        if (ShopSystem.Instance == null) return false;

        // swap 패턴 — 기존 stone 인벤 회수 후 새 stone 장착 (소실 방지)
        if (generator.LoadedStone != null)
        {
            ShopSystem.Instance.AddStone(generator.LoadedStone);
            generator.ClearStone();
        }
        ShopSystem.Instance.RemoveStone(stone);
        generator.SetStone(stone);
        return true;
    }

    /// <summary>
    /// GenerateSlot 에서 시작한 드래그로 장착된 stone 을 인벤으로 회수.
    /// 인벤 패널 배경(InvenDropHandler) 과 인벤 슬롯(InvenSlotDragHandler) 양쪽 드롭 경로에서 공유.
    /// 드래그 중 stone 이 바뀌었을 race 회피용 캐시(`source.DraggingStone`) 검사 포함.
    /// </summary>
    public static bool TryUnloadStone(GenerateSlotDropTarget source)
    {
        if (source == null) return false;
        var generator = WaveGeneratorSystem.Instance;
        if (generator == null || generator.LoadedStone == null) return false;
        if (source.DraggingStone != null && source.DraggingStone != generator.LoadedStone) return false;

        var stone = generator.LoadedStone;
        ShopSystem.Instance?.AddStone(stone);
        generator.ClearStone();
        return true;
    }
}
