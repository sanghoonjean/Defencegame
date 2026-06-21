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
    /// 큐브로 현재 장착된 차원석의 옵션을 조작.
    /// 큐브 소비 전에 가능 여부를 사전 검증해, 거부된 액션이 큐브를 잃거나
    /// 차원석을 부분 변경하지 않도록 한다 (#286 PR 코드 리뷰 반영).
    /// </summary>
    public bool ApplyCube(CubeType cube)
    {
        if (LoadedStone == null) return false;
        if (CubeSystem.Instance == null) return false;
        if (!CanApply(cube)) return false;
        if (CubeSystem.Instance.GetCount(cube) <= 0) return false;

        if (!CubeSystem.Instance.TryConsume(cube, 1)) return false;

        bool success;
        switch (cube)
        {
            case CubeType.Lower:   LoadedStone.Reroll(); success = true; break;
            case CubeType.Upper:   success = LoadedStone.AddRandomOption(); break;
            // TopTier: AGENTS.md — 옵션 1개를 "상위 옵션으로 교체". 옵션 수는 유지.
            // in-place 업그레이드로 의미를 살린다 (#286 PR 리뷰 반영).
            case CubeType.TopTier: success = LoadedStone.UpgradeRandomOption(); break;
            case CubeType.Delete:  success = LoadedStone.RemoveRandomOption(); break;
            case CubeType.Clone:   DimensionStoneInventory.Instance.Add(LoadedStone.Clone()); success = true; break;
            default:               success = false; break;
        }

        if (success) OnStoneChanged?.Invoke();
        return success;
    }

    /// <summary>
    /// 큐브 종류별 적용 가능 여부 사전 검증. 큐브 소비 전에 호출.
    /// - Lower: Reroll 은 항상 가능
    /// - Upper: 옵션이 MaxOptions 미만일 때
    /// - TopTier: Remove + Upgrade 둘 다 성공해야 하므로 ≥3
    /// - Delete: 최소 1개는 남겨야 하므로 ≥2
    /// - Clone: 인벤토리 존재
    /// </summary>
    private bool CanApply(CubeType cube)
    {
        if (LoadedStone == null) return false;
        return cube switch
        {
            CubeType.Lower   => true,
            CubeType.Upper   => LoadedStone.Options.Count < DimensionStone.MaxOptions,
            // TopTier 는 in-place 업그레이드. UpgradeRandomOption 가드(>= 1)와 일치.
            CubeType.TopTier => LoadedStone.Options.Count >= 1,
            CubeType.Delete  => LoadedStone.Options.Count >= 2,
            CubeType.Clone   => DimensionStoneInventory.Instance != null,
            _                => false,
        };
    }

    /// <summary>
    /// 균열 개방 — 차원석 1개 소모 + WaveSystem.StartRiftWave 호출.
    /// </summary>
    public bool OpenRift()
    {
        if (LoadedStone == null) { Debug.Log("[RiftGenerator] 차원석 미장착"); return false; }
        if (WaveSystem.Instance == null) return false;
        if (WaveSystem.Instance.IsWaveActive) { Debug.Log("[RiftGenerator] 웨이브 진행 중 — 균열 개방 불가"); return false; }
        // WaveResult/Defeat 등 비-Playing 상태에서 개방하면 EndWave 가 조기 return 되어 IsWaveActive 가 stuck.
        if (GameStateSystem.Current != GameState.Playing)
        {
            Debug.Log($"[RiftGenerator] GameState={GameStateSystem.Current} — Playing 아니면 개방 불가");
            return false;
        }

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
