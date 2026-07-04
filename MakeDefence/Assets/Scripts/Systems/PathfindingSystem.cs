using UnityEngine;

// 웨이포인트 기반 경로 제공. 경로 데이터는 MapTileSystem에서 관리.
public class PathfindingSystem : MonoBehaviour
{
    public static PathfindingSystem Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public int RouteCount => MapTileSystem.Instance != null ? MapTileSystem.Instance.RouteCount : 0;

    public Vector2[] GetWaypoints() => GetWaypoints(0);
    public Vector2[] GetWaypoints(int routeIndex)
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetWaypoints(routeIndex) : new Vector2[0];
    }

    public Vector2[] GetFullPath() => GetFullPath(0);
    public Vector2[] GetFullPath(int routeIndex)
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetFullPath(routeIndex) : new Vector2[0];
    }

    public Vector2 GetSpawnPoint() => GetSpawnPoint(0);
    public Vector2 GetSpawnPoint(int routeIndex)
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetSpawnPoint(routeIndex) : Vector2.zero;
    }

    public Vector2 GetBasePoint()
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetBasePoint() : Vector2.zero;
    }
}
