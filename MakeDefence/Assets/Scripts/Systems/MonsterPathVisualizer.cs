using System.Collections.Generic;
using UnityEngine;

// 모든 스폰 루트의 스폰 지점 → 본진 경로를 셀 단위로 표시한다. WaveSystem 의 웨이브가
// 진행 중일 때만 표시되며, route 하나하나는 그 route에 스폰된 몬스터가 모두 죽거나
// 본진에 도달해 사라질 때(WaveSystem.OnRouteCleared) 개별적으로 숨겨진다 — 다른 route에
// 몬스터가 남아있으면 웨이브 전체가 끝날 때(OnWaveEnded)까지 그 route의 경로만 계속 표시된다.
// 그 사이 타워 배치/이동/삭제로 경로가 바뀌면 PathfindingSystem.OnPathsChanged 를 통해
// 자동 갱신된다.
public class MonsterPathVisualizer : MonoBehaviour
{
    [SerializeField] private Color markerColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float markerDiameter = 0.25f;
    // Tilemap_Buildable(3)보다는 위, Enemy/Tower(10)보다는 아래 — 타일 위에 표시되면서
    // 몬스터/타워 시인성은 가리지 않도록.
    [SerializeField] private int sortingOrder = 5;

    private readonly List<GameObject> _markers = new();
    private readonly HashSet<int> _activeRoutes = new();
    private Sprite _circleSprite;
    private bool _isWaveActive;

    private void Start()
    {
        _circleSprite = BuildCircleSprite();
        PathfindingSystem.OnPathsChanged += HandlePathsChanged;
        WaveSystem.OnWaveStarted += HandleWaveStarted;
        WaveSystem.OnWaveEnded += HandleWaveEnded;
        WaveSystem.OnRouteCleared += HandleRouteCleared;

        if (WaveSystem.Instance != null && WaveSystem.Instance.IsWaveActive && MapTileSystem.Instance != null)
        {
            _isWaveActive = true;
            int routeCount = MapTileSystem.Instance.RouteCount;
            for (int i = 0; i < routeCount; i++)
                if (WaveSystem.Instance.IsRouteActive(i)) _activeRoutes.Add(i);
            RefreshMarkers();
        }
    }

    private void OnDestroy()
    {
        PathfindingSystem.OnPathsChanged -= HandlePathsChanged;
        WaveSystem.OnWaveStarted -= HandleWaveStarted;
        WaveSystem.OnWaveEnded -= HandleWaveEnded;
        WaveSystem.OnRouteCleared -= HandleRouteCleared;
    }

    private void HandleWaveStarted(int stage)
    {
        _isWaveActive = true;
        _activeRoutes.Clear();
        if (MapTileSystem.Instance != null)
        {
            int routeCount = MapTileSystem.Instance.RouteCount;
            for (int i = 0; i < routeCount; i++)
                _activeRoutes.Add(i);
        }
        RefreshMarkers();
    }

    private void HandleWaveEnded(bool cleared)
    {
        // 웨이브 연속 생성(RepeatGenerateToggleButton) 중에는 같은 OnWaveEnded 디스패치 안에서
        // 그보다 먼저 구독된 다른 컴포넌트가 이미 다음 웨이브를 동기적으로 시작시켜(OnWaveStarted
        // → HandleWaveStarted 선호출) _isWaveActive/_activeRoutes가 새 웨이브 기준으로 세팅돼
        // 있을 수 있다. 구독 순서에 상관없이 정확히 동작하도록 실제 WaveSystem 상태를 다시 확인해,
        // 이미 새 웨이브가 시작된 상태라면 그 상태를 지우지 않는다.
        if (WaveSystem.Instance != null && WaveSystem.Instance.IsWaveActive) return;

        _isWaveActive = false;
        _activeRoutes.Clear();
        ClearMarkers();
    }

    private void HandleRouteCleared(int routeIndex)
    {
        _activeRoutes.Remove(routeIndex);
        RefreshMarkers();
    }

    private void HandlePathsChanged()
    {
        if (_isWaveActive) RefreshMarkers();
    }

    private void ClearMarkers()
    {
        foreach (var marker in _markers)
            Destroy(marker);
        _markers.Clear();
    }

    private void RefreshMarkers()
    {
        ClearMarkers();

        if (MapTileSystem.Instance == null || PathfindingSystem.Instance == null) return;

        var basePoint = MapTileSystem.Instance.GetBasePoint();
        var placed = new HashSet<Vector2>();
        foreach (var routeIndex in _activeRoutes)
        {
            var path = PathfindingSystem.Instance.ComputeFullCellPath(MapTileSystem.Instance.GetSpawnPoint(routeIndex), basePoint);
            foreach (var point in path)
            {
                if (!placed.Add(point)) continue;
                SpawnMarker(point);
            }
        }
    }

    private void SpawnMarker(Vector2 position)
    {
        var marker = new GameObject("PathMarker");
        marker.transform.SetParent(transform);
        marker.transform.position = new Vector3(position.x, position.y, 0f);
        marker.transform.localScale = Vector3.one * markerDiameter;

        var renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = _circleSprite;
        renderer.color = markerColor;
        renderer.sortingOrder = sortingOrder;

        _markers.Add(marker);
    }

    private static Sprite BuildCircleSprite()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        var center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f - 1f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
