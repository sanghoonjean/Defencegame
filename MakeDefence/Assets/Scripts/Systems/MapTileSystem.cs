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
