using UnityEngine;

// 마우스 클릭으로 Buildable 타일에 타워를 배치한다.
public class TowerPlacer : MonoBehaviour
{
    [SerializeField] private Tower towerPrefab;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (WaveSystem.Instance != null && WaveSystem.Instance.IsWaveActive == false)
        {
            // 웨이브 외 배치 허용 — 필요 시 웨이브 중 배치도 가능하도록 조건 제거
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var coord = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

        if (!MapTileSystem.Instance.CanPlaceTower(coord)) return;

        // TODO Phase 2: CubeSystem.TryConsume(CubeType.Lower, 1) 확인
        PlaceTower(coord);
    }

    private void PlaceTower(Vector2Int coord)
    {
        Vector3 worldCenter = new Vector3(coord.x + 0.5f, coord.y + 0.5f, 0f);
        Tower tower = Instantiate(towerPrefab, worldCenter, Quaternion.identity);
        tower.Place(coord);
        MapTileSystem.Instance.PlaceTower(coord, tower);
    }
}
