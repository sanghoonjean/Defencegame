using UnityEngine;

public class RiftGeneratorPlacer : MonoBehaviour
{
    public static RiftGeneratorPlacer Instance { get; private set; }

    [SerializeField] private RiftGenerator riftPrefab;
    [SerializeField] private int placementCostLower = 10;

    private void Awake() { Instance = this; }

    public int PlacementCost => placementCostLower;

    public bool TryPlace(Vector2Int coord)
    {
        if (MapTileSystem.Instance == null) return false;
        if (!MapTileSystem.Instance.CanPlaceRift(coord)) return false;
        if (CubeSystem.Instance == null) return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, placementCostLower)) return false;

        PlaceRift(coord);
        return true;
    }

    private void PlaceRift(Vector2Int coord)
    {
        Vector3 worldCenter = new Vector3(coord.x + 0.5f, coord.y + 0.5f, -1f);
        RiftGenerator rift = Instantiate(riftPrefab, worldCenter, Quaternion.identity);
        rift.Place(coord);
        MapTileSystem.Instance.PlaceRift(coord, rift);
    }
}
