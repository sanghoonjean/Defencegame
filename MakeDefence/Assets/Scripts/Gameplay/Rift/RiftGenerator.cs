using System;
using UnityEngine;

public class RiftGenerator : MonoBehaviour
{
    public static event Action<RiftGenerator> OnRiftPlaced;
    public static event Action<RiftGenerator> OnRiftOpened;

    public Vector2Int TileCoord { get; private set; }
    public DimensionStone LoadedStone { get; private set; }

    public event Action OnStoneChanged;

    public void Place(Vector2Int coord)
    {
        TileCoord = coord;
        OnRiftPlaced?.Invoke(this);
    }

    private void OnDestroy()
    {
        MapTileSystem.Instance?.RemoveRift(TileCoord);
    }

    public void SetStone(DimensionStone stone)
    {
        LoadedStone = stone;
        OnStoneChanged?.Invoke();
    }

    public void ClearStone()
    {
        LoadedStone = null;
        OnStoneChanged?.Invoke();
    }

    /// <summary>
    /// 큐브로 현재 장착된 차원석의 옵션을 조작. ItemSystem.ApplyCube 와 동일 패턴.
    /// </summary>
    public bool ApplyCube(CubeType cube)
    {
        if (LoadedStone == null) return false;

        bool success = cube switch
        {
            CubeType.Lower   => TryConsume(CubeType.Lower,   1, () => { LoadedStone.Reroll(); return true; }),
            CubeType.Upper   => TryConsume(CubeType.Upper,   1, () => LoadedStone.AddRandomOption()),
            CubeType.TopTier => TryConsume(CubeType.TopTier, 1, () => LoadedStone.RemoveRandomOption() && LoadedStone.UpgradeRandomOption()),
            CubeType.Delete  => TryConsume(CubeType.Delete,  1, () => LoadedStone.RemoveRandomOption()),
            CubeType.Clone   => ApplyClone(),
            _                => false,
        };

        if (success) OnStoneChanged?.Invoke();
        return success;
    }

    private bool TryConsume(CubeType type, int amount, Func<bool> action)
    {
        if (CubeSystem.Instance == null) return false;
        if (!CubeSystem.Instance.TryConsume(type, amount)) return false;
        return action();
    }

    /// <summary>
    /// Clone — 현재 차원석을 복제해 인벤토리에 추가.
    /// </summary>
    private bool ApplyClone()
    {
        if (LoadedStone == null) return false;
        if (CubeSystem.Instance == null || DimensionStoneInventory.Instance == null) return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Clone, 1)) return false;
        DimensionStoneInventory.Instance.Add(LoadedStone.Clone());
        return true;
    }

    /// <summary>
    /// 균열 개방 — 차원석 1개 소모 + WaveSystem.StartRiftWave 호출.
    /// </summary>
    public bool OpenRift()
    {
        if (LoadedStone == null) { Debug.Log("[RiftGenerator] 차원석 미장착"); return false; }
        if (WaveSystem.Instance == null) return false;
        if (WaveSystem.Instance.IsWaveActive) { Debug.Log("[RiftGenerator] 웨이브 진행 중 — 균열 개방 불가"); return false; }

        var mods = RiftWaveModifiers.FromOptions(LoadedStone.Options);
        bool started = WaveSystem.Instance.StartRiftWave(mods);
        if (!started) return false;

        DimensionStoneInventory.Instance?.Remove(LoadedStone);
        LoadedStone = null;
        OnStoneChanged?.Invoke();
        OnRiftOpened?.Invoke(this);
        return true;
    }
}
