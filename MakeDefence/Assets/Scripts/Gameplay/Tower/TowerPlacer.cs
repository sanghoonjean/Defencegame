using System;
using UnityEngine;

public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    [SerializeField] private Tower towerPrefab;

    private GameObject _ghost;
    private SpriteRenderer[] _ghostRenderers;
    private Color[] _originalColors;

    private bool _isMoving;
    private Tower _movingTower;
    private Vector2Int _moveOriginCoord;

    private Tower _pendingPrefab;
    private Action<Tower> _pendingOnPlaced;

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

        bool canPlace = MapTileSystem.Instance != null
            && MapTileSystem.Instance.CanPlaceTower(coord, _isMoving ? _moveOriginCoord : (Vector2Int?)null);
        SetGhostColor(canPlace ? GhostValid : GhostInvalid);

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            ExitPlacementMode();
    }

    /// 디버그 전용 진입점. 기본 프리팹(towerPrefab)으로 신규 배치 모드에 들어간다.
    public void EnterPlacementMode() => EnterPlacementMode(towerPrefab, null);

    /// 지정된 프리팹으로 신규 배치 모드에 들어간다. 기존에 배치된 타워 유무와 무관하게
    /// 항상 새 유닛을 만든다 — "이동"은 EnterMoveMode()로만 진입한다.
    public void EnterPlacementMode(Tower prefab, Action<Tower> onPlaced)
    {
        if (IsPlacingTower) return;

        IsPlacingTower = true;
        _pendingPrefab = prefab;
        _pendingOnPlaced = onPlaced;

        Tower ghostTower = Instantiate(prefab);
        ghostTower.InitAsGhost();
        _ghost = ghostTower.gameObject;
        _ghostRenderers = _ghost.GetComponentsInChildren<SpriteRenderer>();
        SetGhostColor(GhostInvalid);
    }

    /// 이미 배치된 특정 타워를 다른 칸으로 옮기는 이동 모드에 들어간다.
    public void EnterMoveMode(Tower existingTower)
    {
        if (IsPlacingTower || existingTower == null) return;

        if (!MapTileSystem.Instance.HasVacantBuildableTile(existingTower.TileCoord))
        {
            // 옮길 곳이 없으면 이동 모드 진입 자체를 취소한다.
            InputManager.Instance?.SetBuildMode(BuildMode.None);
            return;
        }

        IsPlacingTower = true;
        _isMoving = true;
        _movingTower = existingTower;
        _moveOriginCoord = existingTower.TileCoord;

        _ghost = existingTower.gameObject;
        _ghostRenderers = _ghost.GetComponentsInChildren<SpriteRenderer>();
        _originalColors = new Color[_ghostRenderers.Length];
        for (int i = 0; i < _ghostRenderers.Length; i++)
            _originalColors[i] = _ghostRenderers[i].color;

        existingTower.SetGhostVisual(true);
        SetGhostColor(GhostInvalid);
    }

    public void ExitPlacementMode()
    {
        if (!IsPlacingTower) return;
        IsPlacingTower = false;

        if (_isMoving)
        {
            // 이동 중이던 타워가 외부(삭제 버튼 등)에 의해 파괴됐을 수 있으므로 방어적으로 체크.
            if (_movingTower != null)
            {
                _movingTower.MoveTo(_moveOriginCoord);
                _movingTower.SetGhostVisual(false);
                RestoreGhostColors();
            }
        }
        else if (_ghost != null)
        {
            Destroy(_ghost);
        }

        ClearMoveState();
        InputManager.Instance?.SetBuildMode(BuildMode.None);
    }

    public bool TryPlace(Vector2Int coord)
    {
        if (_isMoving) return TryMove(coord);

        if (!MapTileSystem.Instance.CanPlaceTower(coord)) return false;
        if (!CubeSystem.Instance.TryConsume(CubeType.Lower, 1)) return false;

        PlaceTower(coord);
        return true;
    }

    private bool TryMove(Vector2Int coord)
    {
        // 이동 중이던 타워가 외부(삭제 버튼 등)에 의해 파괴됐으면 이동 실패 처리.
        // 뒤이어 호출되는 ExitPlacementMode()가 나머지 상태 정리를 담당한다.
        if (_movingTower == null) return false;

        if (!MapTileSystem.Instance.CanPlaceTower(coord, _moveOriginCoord)) return false;

        MapTileSystem.Instance.RemoveTower(_moveOriginCoord);
        _movingTower.MoveTo(coord);
        MapTileSystem.Instance.PlaceTower(coord, _movingTower);

        _movingTower.SetGhostVisual(false);
        RestoreGhostColors();
        ClearMoveState();
        return true;
    }

    private void PlaceTower(Vector2Int coord)
    {
        Vector3 worldCenter = new Vector3(coord.x + 0.5f, coord.y + 0.5f, -1f);
        Tower tower = Instantiate(_pendingPrefab, worldCenter, Quaternion.identity);
        tower.Place(coord);
        MapTileSystem.Instance.PlaceTower(coord, tower);
        _pendingOnPlaced?.Invoke(tower);
    }

    private void ClearMoveState()
    {
        _isMoving = false;
        _movingTower = null;
        _ghost = null;
        _ghostRenderers = null;
        _originalColors = null;
        _pendingPrefab = null;
        _pendingOnPlaced = null;
    }

    private void RestoreGhostColors()
    {
        if (_originalColors == null || _ghostRenderers == null) return;
        for (int i = 0; i < _ghostRenderers.Length && i < _originalColors.Length; i++)
            _ghostRenderers[i].color = _originalColors[i];
    }

    private void SetGhostColor(Color color)
    {
        if (_ghostRenderers == null) return;
        foreach (var sr in _ghostRenderers)
            sr.color = color;
    }
}
