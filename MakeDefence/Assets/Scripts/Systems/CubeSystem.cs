using System;
using System.Collections.Generic;
using UnityEngine;

public enum CubeType { Lower, Upper, TopTier, Delete, Clone }

public class CubeSystem : MonoBehaviour
{
    public static CubeSystem Instance { get; private set; }

    public static event Action<CubeType, int> OnCubeChanged;

    // 드롭 가중치 (tech-debt: 수치 미확정 — Inspector에서 조정)
    [SerializeField] private int lowerWeight  = 50;
    [SerializeField] private int upperWeight  = 25;
    [SerializeField] private int topTierWeight = 10;
    [SerializeField] private int deleteWeight  = 10;
    [SerializeField] private int cloneWeight   = 5;

    // 웨이브당 드롭 수 범위
    [SerializeField] private int dropMin = 3;
    [SerializeField] private int dropMax = 6;

    [SerializeField] private int initialLowerCubes = 5;

    private readonly Dictionary<CubeType, int> _counts = new()
    {
        { CubeType.Lower,   0 },
        { CubeType.Upper,   0 },
        { CubeType.TopTier, 0 },
        { CubeType.Delete,  0 },
        { CubeType.Clone,   0 },
    };

    private void Awake()
    {
        Instance = this;
        if (initialLowerCubes > 0)
            Add(CubeType.Lower, initialLowerCubes);
    }

    private void OnEnable()
    {
        WaveSystem.OnWaveEnded += HandleWaveEnded;
    }

    private void OnDisable()
    {
        WaveSystem.OnWaveEnded -= HandleWaveEnded;
    }

    public int GetCount(CubeType type) => _counts[type];

    public bool TryConsume(CubeType type, int amount)
    {
        if (_counts[type] < amount) return false;
        _counts[type] -= amount;
        OnCubeChanged?.Invoke(type, _counts[type]);
        return true;
    }

    public void Add(CubeType type, int amount)
    {
        _counts[type] += amount;
        OnCubeChanged?.Invoke(type, _counts[type]);
    }

    public void DropReward(int stage)
    {
        int count = UnityEngine.Random.Range(dropMin, dropMax + 1);
        for (int i = 0; i < count; i++)
            Add(RollDrop(), 1);
    }

    private CubeType RollDrop()
    {
        int total = lowerWeight + upperWeight + topTierWeight + deleteWeight + cloneWeight;
        int roll  = UnityEngine.Random.Range(0, total);

        if (roll < lowerWeight)                  return CubeType.Lower;
        roll -= lowerWeight;
        if (roll < upperWeight)                  return CubeType.Upper;
        roll -= upperWeight;
        if (roll < topTierWeight)                return CubeType.TopTier;
        roll -= topTierWeight;
        if (roll < deleteWeight)                 return CubeType.Delete;
        return CubeType.Clone;
    }

    private void HandleWaveEnded(bool cleared)
    {
        if (!cleared) return;
        DropReward(WaveSystem.Instance.CurrentStage);
    }
}
