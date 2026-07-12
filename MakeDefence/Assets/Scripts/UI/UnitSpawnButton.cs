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
                EnterPlacement(unitPrefab, null); // 팝업 미연결 시 프리팹의 직업 그대로 유지
        }
    }

    private void OnJobSelected(JobClass job)
    {
        // 팝업에서 고른 직업 전용 프리팹으로 배치. 매핑이 없으면 버튼 기본 프리팹 + SetJob 폴백.
        Tower jobPrefab = JobSelectPopup.Instance != null
            ? JobSelectPopup.Instance.ResolvePrefab(job)
            : null;

        if (jobPrefab != null)
            EnterPlacement(jobPrefab, null);   // 프리팹에 직업이 고정돼 있어 SetJob 불필요
        else
            EnterPlacement(unitPrefab, job);   // 폴백: 기본 프리팹에 직업 스탯만 적용
    }

    // job 이 null 이면 프리팹에 지정된 직업을 덮어쓰지 않는다.
    private void EnterPlacement(Tower prefab, JobClass? job)
    {
        InputManager.Instance.SetBuildMode(BuildMode.Tower);
        TowerPlacer.Instance.EnterPlacementMode(prefab, tower =>
        {
            if (job.HasValue) tower.SetJob(job.Value);
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
