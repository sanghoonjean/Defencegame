using UnityEngine;

public class TowerPlacer : MonoBehaviour
{
    [SerializeField] private Tower towerPrefab;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var coord = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

        if (!MapTileSystem.Instance.CanPlaceTower(coord)) return;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1)) return;

        PlaceTower(coord);
    }

    private void PlaceTower(Vector2Int coord)
    {
        Vector3 worldCenter = new Vector3(coord.x + 0.5f, coord.y + 0.5f, -1f);
        Tower tower = Instantiate(towerPrefab, worldCenter, Quaternion.identity);
        tower.Place(coord);
        MapTileSystem.Instance.PlaceTower(coord, tower);
    }
}
