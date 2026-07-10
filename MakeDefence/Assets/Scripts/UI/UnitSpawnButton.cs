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

        if (_placedTower != null)
        {
            // 재배치: 즉시 BuildMode 설정 후 이동 모드
            InputManager.Instance.SetBuildMode(BuildMode.Tower);
            TowerPlacer.Instance.EnterMoveMode(_placedTower);
        }
        else
        {
            // 첫 배치: 팝업이 닫힌 뒤에 BuildMode 설정 (팝업 중 맵 클릭 방지)
            if (JobSelectPopup.Instance != null)
                JobSelectPopup.Instance.Show(OnJobSelected);
            else
                EnterPlacement(JobClass.None);
        }
    }

    private void OnJobSelected(JobClass job)
    {
        EnterPlacement(job);
    }

    private void EnterPlacement(JobClass job)
    {
        InputManager.Instance.SetBuildMode(BuildMode.Tower);
        TowerPlacer.Instance.EnterPlacementMode(unitPrefab, tower =>
        {
            tower.SetJob(job);
            OnUnitPlaced(tower);
        });
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
