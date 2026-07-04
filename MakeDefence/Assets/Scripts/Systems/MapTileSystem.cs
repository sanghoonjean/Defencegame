using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType { Path, Buildable, Decoration }

public class MapTileSystem : MonoBehaviour
{
    public static MapTileSystem Instance { get; private set; }

    [SerializeField] private Tilemap buildableTilemap;
    [SerializeField] private Tilemap pathTilemap;

    [SerializeField] private Vector2[] waypoints;
    [SerializeField] private Vector2 spawnPoint;
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

    public bool CanPlaceTower(Vector2Int coord)
    {
        return GetTileType(coord) == TileType.Buildable
            && !_placedTowers.ContainsKey(coord);
    }

    public bool PlaceTower(Vector2Int coord, Tower tower)
    {
        if (!CanPlaceTower(coord)) return false;
        _placedTowers[coord] = tower;
        return true;
    }

    public void RemoveTower(Vector2Int coord)
    {
        _placedTowers.Remove(coord);
    }

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

    public Vector2[] GetWaypoints() => waypoints;
    public Vector2 GetSpawnPoint() => spawnPoint;
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

    public Vector2[] GetFullPath()
    {
        var full = new Vector2[waypoints.Length + 2];
        full[0] = spawnPoint + Vector2.one * 0.5f;
        for (int i = 0; i < waypoints.Length; i++)
            full[i + 1] = waypoints[i] + Vector2.one * 0.5f;
        full[full.Length - 1] = basePoint + Vector2.one * 0.5f;
        return full;
    }
}
