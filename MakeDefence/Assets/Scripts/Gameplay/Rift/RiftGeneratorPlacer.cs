using UnityEngine;

public class RiftGeneratorPlacer : MonoBehaviour
{
    public static RiftGeneratorPlacer Instance { get; private set; }

    [SerializeField] private RiftGenerator riftPrefab;
    [SerializeField] private int placementCostLower = 10;

    [Header("자동 배치 (게임 시작 시 큐브 소비 없이 설치)")]
    [SerializeField] private bool autoPlaceOnStart = true;
    [SerializeField] private Vector2Int autoPlaceCoord;

    private void Awake() { Instance = this; }

    private void Start()
    {
        if (!autoPlaceOnStart) return;
        if (MapTileSystem.Instance == null) { Debug.LogWarning("[RiftGeneratorPlacer] MapTileSystem 미초기화 — 자동 배치 skip"); return; }
        if (riftPrefab == null) { Debug.LogWarning("[RiftGeneratorPlacer] riftPrefab 미할당 — 자동 배치 skip"); return; }
        if (!MapTileSystem.Instance.CanPlaceRift(autoPlaceCoord))
        {
            Debug.LogWarning($"[RiftGeneratorPlacer] autoPlaceCoord {autoPlaceCoord} 가 배치 불가 — Inspector 에서 Buildable 셀로 조정 필요");
            return;
        }
        PlaceRift(autoPlaceCoord);
        Debug.Log($"[RiftGeneratorPlacer] 자동 배치 완료 — coord={autoPlaceCoord}");
    }

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
