using UnityEngine;

// 웨이포인트 기반 경로 제공. 경로 데이터는 MapTileSystem에서 관리.
public class PathfindingSystem : MonoBehaviour
{
    public static PathfindingSystem Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public Vector2[] GetWaypoints()
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetWaypoints() : new Vector2[0];
    }

    public Vector2[] GetFullPath()
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetFullPath() : new Vector2[0];
    }

    public Vector2 GetSpawnPoint()
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetSpawnPoint() : Vector2.zero;
    }

    public Vector2 GetBasePoint()
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetBasePoint() : Vector2.zero;
    }
}
