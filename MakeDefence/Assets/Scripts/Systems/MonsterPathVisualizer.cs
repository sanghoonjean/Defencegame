using System.Collections.Generic;
using UnityEngine;

// 모든 스폰 루트의 스폰 지점 → 본진 경로를 셀 단위로 상시 표시한다. 타워 배치/이동/삭제로
// 경로가 바뀌면 PathfindingSystem.OnPathsChanged 를 통해 자동 갱신된다.
public class MonsterPathVisualizer : MonoBehaviour
{
    [SerializeField] private Color markerColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float markerDiameter = 0.25f;
    // Tilemap_Buildable(3)보다는 위, Enemy/Tower(10)보다는 아래 — 타일 위에 표시되면서
    // 몬스터/타워 시인성은 가리지 않도록.
    [SerializeField] private int sortingOrder = 5;

    private readonly List<GameObject> _markers = new();
    private Sprite _circleSprite;

    private void Start()
    {
        _circleSprite = BuildCircleSprite();
        PathfindingSystem.OnPathsChanged += RefreshMarkers;
        RefreshMarkers();
    }

    private void OnDestroy()
    {
        PathfindingSystem.OnPathsChanged -= RefreshMarkers;
    }

    private void RefreshMarkers()
    {
        foreach (var marker in _markers)
            Destroy(marker);
        _markers.Clear();

        if (MapTileSystem.Instance == null || PathfindingSystem.Instance == null) return;

        var basePoint = MapTileSystem.Instance.GetBasePoint();
        var placed = new HashSet<Vector2>();
        int routeCount = MapTileSystem.Instance.RouteCount;
        for (int i = 0; i < routeCount; i++)
        {
            var path = PathfindingSystem.Instance.ComputeFullCellPath(MapTileSystem.Instance.GetSpawnPoint(i), basePoint);
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
