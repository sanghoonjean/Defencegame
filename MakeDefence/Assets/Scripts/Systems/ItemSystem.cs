using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemSystem : MonoBehaviour
{
    public static ItemSystem Instance { get; private set; }

    public static event Action<Tower> OnItemChanged;

    private static readonly int[] SlotUnlockCost = { 10, 20, 30 };

    private readonly Dictionary<Tower, ItemData[]> _items = new();
    private readonly Dictionary<Tower, int> _unlockedSlots = new();

    private void Awake() { Instance = this; }

    public void RegisterTower(Tower tower)
    {
        _items[tower]         = new ItemData[3];
        _unlockedSlots[tower] = 0;
    }

    public void UnregisterTower(Tower tower)
    {
        _items.Remove(tower);
        _unlockedSlots.Remove(tower);
    }

    public bool UnlockSlot(Tower tower)
    {
        int next = _unlockedSlots[tower];
        if (next >= 3) return false;
        int cost = SlotUnlockCost[next];
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, cost)) return false;
        _items[tower][next] = ItemData.CreateRandom();
        _unlockedSlots[tower]++;
        tower.RefreshStats();
        OnItemChanged?.Invoke(tower);
        return true;
    }

    public int GetUnlockedSlotCount(Tower tower) =>
        _unlockedSlots.TryGetValue(tower, out int v) ? v : 0;

    public ItemData GetItem(Tower tower, int slot) =>
        _items.TryGetValue(tower, out var arr) ? arr[slot] : null;

    public bool ApplyCube(CubeType cube, Tower tower, int slot)
    {
        if (!_items.TryGetValue(tower, out var arr)) return false;
        if (slot >= _unlockedSlots[tower])            return false;
        var item = arr[slot];
        if (item == null)                             return false;

        bool success = cube switch
        {
            CubeType.Lower   => TryConsume(CubeType.Lower,   1, () => { item.Reroll(); return true; }),
            CubeType.Upper   => TryConsume(CubeType.Upper,   1, () => item.AddRandomOption()),
            CubeType.TopTier => TryConsume(CubeType.TopTier, 1, () => item.RemoveRandomOption() && item.UpgradeRandomOption()),
            CubeType.Delete  => TryConsume(CubeType.Delete,  1, () => item.RemoveRandomOption()),
            CubeType.Clone   => ApplyClone(tower, slot, arr),
            _                => false
        };

        if (success)
        {
            tower.RefreshStats();
            OnItemChanged?.Invoke(tower);
        }
        return success;
    }

    private bool TryConsume(CubeType type, int amount, Func<bool> action)
    {
        if (!CubeSystem.Instance.TryConsume(type, amount)) return false;
        return action();
    }

    private bool ApplyClone(Tower tower, int srcSlot, ItemData[] arr)
    {
        int emptySlot = -1;
        for (int i = 0; i < _unlockedSlots[tower]; i++)
        {
            if (i != srcSlot && arr[i] == null) { emptySlot = i; break; }
        }
        if (emptySlot < 0) return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Clone, 1)) return false;
        arr[emptySlot] = arr[srcSlot].Clone();
        return true;
    }
}
