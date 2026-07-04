using System;
using UnityEngine;

/// <summary>
/// 차원석 장착 + 큐브 적용 + 웨이브 오픈 상태를 보유하는 싱글톤.
/// 기존 RiftGenerator(월드 배치/선택 오브젝트)의 상태/로직을 이식 — Rift 개념 제거 후
/// WaveGeneraterbtn 이 여는 DimesionStoneInventoryUI 패널이 직접 참조한다.
/// </summary>
public class WaveGeneratorSystem : MonoBehaviour
{
    public static WaveGeneratorSystem Instance { get; private set; }

    public DimensionStone LoadedStone { get; private set; }

    // static — Instance 생성(Awake) 이전에 구독해도 놓치지 않도록.
    // Canvas 하위 UI(OpenRiftButton 등)의 OnEnable 이 이 오브젝트의 Awake 보다
    // 먼저 실행될 수 있어(계층 순서 의존), 인스턴스 이벤트로는 구독이 누락될 수 있다.
    public static event Action OnStoneChanged;

    private void Awake() { Instance = this; }

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
            case CubeType.Clone:   ShopSystem.Instance.AddStone(LoadedStone.Clone()); success = true; break;
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
            // TopTier — upgradeable 옵션이 있어야만. (모두 max 도달이면 큐브 손실 방지)
            CubeType.TopTier => LoadedStone.CanUpgrade(),
            CubeType.Delete  => LoadedStone.Options.Count >= 2,
            CubeType.Clone   => ShopSystem.Instance != null,
            _                => false,
        };
    }

    /// <summary>
    /// 웨이브 개방 — 차원석 1개 소모 + WaveSystem.StartRiftWave 호출.
    /// </summary>
    public bool OpenRift()
    {
        if (LoadedStone == null) { Debug.Log("[WaveGeneratorSystem] 차원석 미장착"); return false; }
        if (WaveSystem.Instance == null) return false;
        if (WaveSystem.Instance.IsWaveActive) { Debug.Log("[WaveGeneratorSystem] 웨이브 진행 중 — 개방 불가"); return false; }
        // WaveResult/Defeat 등 비-Playing 상태에서 개방하면 EndWave 가 조기 return 되어 IsWaveActive 가 stuck.
        if (GameStateSystem.Current != GameState.Playing)
        {
            Debug.Log($"[WaveGeneratorSystem] GameState={GameStateSystem.Current} — Playing 아니면 개방 불가");
            return false;
        }

        var mods = RiftWaveModifiers.FromOptions(LoadedStone.Options);
        bool started = WaveSystem.Instance.StartRiftWave(mods);
        if (!started) return false;

        // LoadedStone 은 이미 EquipStone 시점에 인벤에서 제거됨 (소비만 처리).
        LoadedStone = null;
        OnStoneChanged?.Invoke();
        return true;
    }
}
