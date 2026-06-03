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
        SelectedTower = null;
        OnTowerSelected?.Invoke(null);
    }

    /// <summary>
    /// 명시적으로 지정한 타워를 삭제하고 배치 비용(하급 큐브 1개)을 환급한다.
    /// 팝업 오픈 시점에 캡처한 타워를 그대로 전달받으므로,
    /// 그 사이 SelectedTower 가 다른 타워로 바뀌어도 안전하다.
    /// </summary>
    public bool DeleteTower(Tower target)
    {
        // Unity 의 == 오버로드가 Destroy 예약된 객체도 null 로 판별
        if (target == null) return false;

        // 캡처된 타워가 현재 선택과 동일할 때만 선택 해제
        if (SelectedTower == target)
            Deselect();

        // 배치 비용 전액 환급 (TowerPlacer 에서 Lower 1개 소비)
        CubeSystem.Instance?.Add(CubeType.Lower, 1);

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
}
