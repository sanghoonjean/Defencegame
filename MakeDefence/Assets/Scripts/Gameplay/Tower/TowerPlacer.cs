using UnityEngine;

public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    [SerializeField] private Tower towerPrefab;

    private GameObject _ghost;
    private SpriteRenderer[] _ghostRenderers;

    public bool IsPlacingTower { get; private set; }

    private static readonly Color GhostValid   = new Color(0f, 1f, 0f, 0.5f);
    private static readonly Color GhostInvalid = new Color(1f, 0f, 0f, 0.5f);

    private void Awake() { Instance = this; }

    private void Update()
    {
        if (!IsPlacingTower || _ghost == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var coord = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        _ghost.transform.position = new Vector3(coord.x + 0.5f, coord.y + 0.5f, -1f);

        bool canPlace = MapTileSystem.Instance != null && MapTileSystem.Instance.CanPlaceTower(coord);
        SetGhostColor(canPlace ? GhostValid : GhostInvalid);

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            ExitPlacementMode();
    }

    public void EnterPlacementMode()
    {
        if (IsPlacingTower) return;
        IsPlacingTower = true;

        Tower ghostTower = Instantiate(towerPrefab);
        ghostTower.InitAsGhost();
        _ghost = ghostTower.gameObject;
        _ghostRenderers = _ghost.GetComponentsInChildren<SpriteRenderer>();
        SetGhostColor(GhostInvalid);
    }

    public void ExitPlacementMode()
    {
        if (!IsPlacingTower) return;
        IsPlacingTower = false;
        if (_ghost != null) { Destroy(_ghost); _ghost = null; }
        _ghostRenderers = null;
    }

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

    private void SetGhostColor(Color color)
    {
        if (_ghostRenderers == null) return;
        foreach (var sr in _ghostRenderers)
            sr.color = color;
    }
}
