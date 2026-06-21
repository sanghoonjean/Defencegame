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
    private readonly Dictionary<Vector2Int, RiftGenerator> _placedRifts = new();

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
            && !_placedTowers.ContainsKey(coord)
            && !_placedRifts.ContainsKey(coord);
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

    public bool CanPlaceRift(Vector2Int coord)
    {
        return GetTileType(coord) == TileType.Buildable
            && !_placedTowers.ContainsKey(coord)
            && !_placedRifts.ContainsKey(coord);
    }

    public bool PlaceRift(Vector2Int coord, RiftGenerator rift)
    {
        if (!CanPlaceRift(coord)) return false;
        _placedRifts[coord] = rift;
        return true;
    }

    public void RemoveRift(Vector2Int coord)
    {
        _placedRifts.Remove(coord);
    }

    public RiftGenerator GetRiftAt(Vector2Int coord)
        => _placedRifts.TryGetValue(coord, out var r) ? r : null;

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
