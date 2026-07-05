using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼별로 고유한 유닛(Tower) 프리팹을 배치한다. 처음 클릭하면 신규 배치,
/// 이 버튼으로 만든 유닛이 이미 맵에 있으면 재클릭 시 그 유닛만 이동 모드로 전환한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class UnitSpawnButton : MonoBehaviour
{
    [SerializeField] private Tower unitPrefab;

    private Button _button;
    private Tower _placedTower;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (InputManager.Instance == null || TowerPlacer.Instance == null) return;

        if (TowerPlacer.Instance.IsPlacingTower)
            TowerPlacer.Instance.ExitPlacementMode();

        InputManager.Instance.SetBuildMode(BuildMode.Tower);

        if (_placedTower != null)
            TowerPlacer.Instance.EnterMoveMode(_placedTower);
        else
            TowerPlacer.Instance.EnterPlacementMode(unitPrefab, OnUnitPlaced);
    }

    private void OnUnitPlaced(Tower tower)
    {
        _placedTower = tower;
        _placedTower.OnRemoved += HandleTowerRemoved;
    }

    private void HandleTowerRemoved()
    {
        if (_placedTower != null)
            _placedTower.OnRemoved -= HandleTowerRemoved;
        _placedTower = null;
    }
}
