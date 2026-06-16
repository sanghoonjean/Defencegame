using UnityEngine;

public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    [SerializeField] private Tower towerPrefab;

    private void Awake() { Instance = this; }

    public bool TryPlace(Vector2Int coord)
    {
        if (!MapTileSystem.Instance.CanPlaceTower(coord)) return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1)) return false;

        PlaceTower(coord);
        return true;
    }

    private void PlaceTower(Vector2Int coord)
    {
        Vector3 worldCenter = new Vector3(coord.x + 0.5f, coord.y + 0.5f, -1f);
        Tower tower = Instantiate(towerPrefab, worldCenter, Quaternion.identity);
        tower.Place(coord);
        MapTileSystem.Instance.PlaceTower(coord, tower);
    }
}
