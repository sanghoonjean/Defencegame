using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType { Path, Buildable, Decoration }

public class MapTileSystem : MonoBehaviour
{
    public static MapTileSystem Instance { get; private set; }

    [SerializeField] private Tilemap buildableTilemap;
    [SerializeField] private Tilemap pathTilemap;

    // 웨이포인트 좌표는 Unity Inspector에서 설정 (tech-debt: 정확한 좌표 미확정)
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
        return GetTileType(coord) == TileType.Buildable && !_placedTowers.ContainsKey(coord);
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

    public Vector2[] GetWaypoints() => waypoints;
    public Vector2 GetSpawnPoint() => spawnPoint;
    public Vector2 GetBasePoint() => basePoint;
}
