using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType { Path, Buildable, Decoration }

public class MapTileSystem : MonoBehaviour
{
    // 스폰 지점. 중간 경유점은 더 이상 손으로 배치하지 않고 A*(PathfindingSystem)가 계산한다.
    // 필드에 접근 지정자를 생략하면 C# 기본 private 가 되어 Unity 가 각 원소 내부를 저장/노출하지
    // 못하므로 반드시 public (또는 [SerializeField]) 로 명시.
    [Serializable]
    public struct SpawnRoute
    {
        public Vector2 spawnPoint;
    }

    public static MapTileSystem Instance { get; private set; }

    [SerializeField] private Tilemap buildableTilemap;
    [SerializeField] private Tilemap pathTilemap;

    [SerializeField] private SpawnRoute[] spawnRoutes;
    [SerializeField] private Vector2 basePoint;

    private readonly Dictionary<Vector2Int, Tower> _placedTowers = new();

    private void Awake()
    {
        Instance = this;
    }

    public TileType GetTileType(Vector2Int coord)
    {
        var cell = new Vector3Int(coord.x, coord.y, 0);
        if (buildableTilemap != null && buildableTilemap.HasTile(cell)) return TileType.Buildable;
        if (pathTilemap != null && pathTilemap.HasTile(cell)) return TileType.Path;
        return TileType.Decoration;
    }

    /// 해당 셀이 몬스터가 지나갈 수 있는 셀인지: Path/Buildable 타일이고 타워가 없어야 한다.
    public bool IsWalkable(Vector2Int cell)
    {
        var tileType = GetTileType(cell);
        return (tileType == TileType.Buildable || tileType == TileType.Path)
            && !_placedTowers.ContainsKey(cell);
    }

    public bool CanPlaceTower(Vector2Int coord) => CanPlaceTower(coord, null);

    /// <summary>
    /// coord에 타워를 놓을 수 있는지 검사한다. ignoreCoord가 지정되면 그 좌표는 점유·연결성
    /// 검사 양쪽 모두에서 "타워 없음"으로 간주한다 — 타워 이동 시 원위치를 제외하기 위함
    /// (아직 RemoveTower가 호출되지 않은 시점에도 원위치로 되돌리는 이동이 막히지 않도록).
    /// </summary>
    public bool CanPlaceTower(Vector2Int coord, Vector2Int? ignoreCoord)
    {
        return GetTileType(coord) == TileType.Buildable
            && (!_placedTowers.ContainsKey(coord) || coord == ignoreCoord)
            && !WouldSeverPath(coord, ignoreCoord);
    }

    /// <summary>
    /// coord에 타워가 있다고 가정했을 때(단 ignoreCoord는 타워가 없다고 간주) 모든
    /// spawnRoutes[].spawnPoint 에서 basePoint 까지 여전히 도달 가능한지 검사한다.
    /// 도달 불가능한 route가 하나라도 있으면 true(=봉쇄됨).
    /// </summary>
    public bool WouldSeverPath(Vector2Int coord, Vector2Int? ignoreCoord = null)
    {
        if (spawnRoutes == null || spawnRoutes.Length == 0) return false;

        var baseCell = WorldToCell(basePoint);

        // AStarPathfinder.IsReachable/FindPath는 goal 셀 자체를 항상 도달 가능으로 예외 처리한다
        // (basePoint가 Decoration 타일 위에 있을 수 있어 실제 이동에는 필요한 규칙). 하지만 그 예외
        // 때문에 "본진 셀 자체를 막는 가상 배치"는 이 도달성 검사로는 절대 감지되지 않는다 — goal이
        // coord와 같아도 isWalkable(goal) 호출 자체가 생략되기 때문(Codex 리뷰 지적). 본진 셀에
        // 타워를 놓는 것은 항상 봉쇄로 간주해 여기서 직접 차단한다.
        if (coord == baseCell) return true;

        bool HypotheticalWalkable(Vector2Int cell)
        {
            if (cell == coord) return false;
            if (ignoreCoord.HasValue && cell == ignoreCoord.Value) return true;
            return IsWalkable(cell);
        }

        foreach (var route in spawnRoutes)
        {
            var spawnCell = WorldToCell(route.spawnPoint);
            if (!AStarPathfinder.IsReachable(spawnCell, baseCell, HypotheticalWalkable))
                return true;
        }
        return false;
    }

    public bool PlaceTower(Vector2Int coord, Tower tower)
    {
        if (!CanPlaceTower(coord)) return false;
        _placedTowers[coord] = tower;
        PathfindingSystem.Instance?.RecalculateActiveEnemyPaths();
        return true;
    }

    public void RemoveTower(Vector2Int coord)
    {
        _placedTowers.Remove(coord);
        PathfindingSystem.Instance?.RecalculateActiveEnemyPaths();
    }

    private static Vector2Int WorldToCell(Vector2 point)
        => new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));

    /// 현재 배치된 타워(설계상 항상 최대 1개)를 반환. 없으면 null.
    public Tower GetPlacedTower()
    {
        foreach (var kv in _placedTowers)
            return kv.Value;
        return null;
    }

    /// excludeCoord를 제외하고 비어있는 Buildable 타일이 하나라도 있으면 true.
    public bool HasVacantBuildableTile(Vector2Int excludeCoord)
    {
        if (buildableTilemap == null) return false;

        buildableTilemap.CompressBounds();
        foreach (var cell in buildableTilemap.cellBounds.allPositionsWithin)
        {
            if (!buildableTilemap.HasTile(cell)) continue;

            var coord = new Vector2Int(cell.x, cell.y);
            if (coord == excludeCoord) continue;
            if (_placedTowers.ContainsKey(coord)) continue;
            return true;
        }
        return false;
    }

    public int RouteCount => spawnRoutes != null ? spawnRoutes.Length : 0;

    public Vector2 GetSpawnPoint() => GetSpawnPoint(0);
    public Vector2 GetSpawnPoint(int routeIndex)
    {
        if (spawnRoutes == null || routeIndex < 0 || routeIndex >= spawnRoutes.Length)
            return Vector2.zero;
        return spawnRoutes[routeIndex].spawnPoint;
    }

    public Vector2 GetBasePoint() => basePoint;

    /// <summary>
    /// buildable + path 두 Tilemap 의 셀 바운드를 합쳐 월드 Bounds 로 반환.
    /// 두 Tilemap 모두 null 이거나 빈 경우 hasValue=false.
    /// </summary>
    public bool TryGetMapWorldBounds(out Bounds worldBounds)
    {
        worldBounds = default;
        bool any = false;

        BoundsInt cellBounds = default;
        if (buildableTilemap != null)
        {
            buildableTilemap.CompressBounds();
            var b = buildableTilemap.cellBounds;
            if (b.size.x > 0 && b.size.y > 0)
            {
                cellBounds = b;
                any = true;
            }
        }
        if (pathTilemap != null)
        {
            pathTilemap.CompressBounds();
            var b = pathTilemap.cellBounds;
            if (b.size.x > 0 && b.size.y > 0)
            {
                if (!any) { cellBounds = b; any = true; }
                else
                {
                    var min = Vector3Int.Min(cellBounds.min, b.min);
                    var max = Vector3Int.Max(cellBounds.max, b.max);
                    cellBounds = new BoundsInt(min, max - min);
                }
            }
        }
        if (!any) return false;

        var tm = buildableTilemap != null ? buildableTilemap : pathTilemap;
        Vector3 worldMin = tm.CellToWorld(cellBounds.min);
        Vector3 worldMax = tm.CellToWorld(cellBounds.max);
        worldBounds = new Bounds((worldMin + worldMax) * 0.5f, worldMax - worldMin);
        return true;
    }
}
