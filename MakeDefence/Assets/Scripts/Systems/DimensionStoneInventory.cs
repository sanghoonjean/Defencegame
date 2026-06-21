using System;
using System.Collections.Generic;
using UnityEngine;

public class DimensionStoneInventory : MonoBehaviour
{
    public static DimensionStoneInventory Instance { get; private set; }

    public static event Action OnInventoryChanged;

    [SerializeField] private int initialStones = 1;

    private readonly List<DimensionStone> _stones = new();
    public IReadOnlyList<DimensionStone> Stones => _stones;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < initialStones; i++)
            _stones.Add(DimensionStone.CreateRandom());
        if (initialStones > 0)
            Debug.Log($"[DimensionStoneInventory] 초기 차원석 {initialStones}개 지급");
    }

    public void Add(DimensionStone stone)
    {
        if (stone == null) return;
        _stones.Add(stone);
        OnInventoryChanged?.Invoke();
    }

    public bool Remove(DimensionStone stone)
    {
        bool removed = _stones.Remove(stone);
        if (removed) OnInventoryChanged?.Invoke();
        return removed;
    }

    public int Count => _stones.Count;
}
