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

    // --- 스킬 ---
    public bool EquipSkill(SkillData skill)
    {
        if (SelectedTower == null) return false;
        SelectedTower.EquipSkill(skill);
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
