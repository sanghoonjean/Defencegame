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

    [Tooltip("이 버튼이 유닛을 배치했을 때만 보이는 전용 삭제 버튼. 유닛이 없으면 SetActive(false).")]
    [SerializeField] private GameObject deleteButton;

    [Tooltip("배치된 유닛의 아이콘을 표시하는 전용 자식 Image. 유닛이 없으면 빈 슬롯 아이콘을 표시한다.")]
    [SerializeField] private Image iconImage;

    [Tooltip("유닛 미생성(빈 슬롯) 상태에서 표시할 '추가' 아이콘. 미지정 시 빈 슬롯에는 아무것도 표시하지 않는다.")]
    [SerializeField] private Sprite emptyIcon;

    private Button _button;
    private Tower _placedTower;

    /// <summary>이 버튼이 현재 배치해 둔 유닛. 없으면 null. (짝 삭제 버튼이 삭제 대상으로 사용)</summary>
    public Tower PlacedTower => _placedTower;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);

        // 시작 시 유닛이 없으므로 전용 삭제 버튼은 숨기고 빈 슬롯(추가) 아이콘을 표시한다.
        if (deleteButton != null) deleteButton.SetActive(false);
        ApplyIcon(null, fallbackToEmpty: true);
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

        // 유닛이 생성된 뒤에만 전용 삭제 버튼과 유닛 아이콘을 노출한다.
        // 유닛 아이콘 미설정 시 emptyIcon 으로 대체하지 않고 숨긴다 (점유 슬롯 오인 방지).
        if (deleteButton != null) deleteButton.SetActive(true);
        ApplyIcon(tower.UnitIcon, fallbackToEmpty: false);
    }

    private void HandleTowerRemoved()
    {
        if (_placedTower != null)
            _placedTower.OnRemoved -= HandleTowerRemoved;
        _placedTower = null;

        // 유닛이 삭제되면 전용 삭제 버튼을 숨기고 빈 슬롯(추가) 아이콘으로 되돌린다.
        if (deleteButton != null) deleteButton.SetActive(false);
        ApplyIcon(null, fallbackToEmpty: true);
    }

    // 유닛 아이콘을 설정한다. fallbackToEmpty 가 true(빈 슬롯)면 icon 이 null 일 때 emptyIcon 으로 대체하고,
    // false(유닛 배치됨)면 대체 없이 그대로 둔다 — 점유된 슬롯이 '추가 가능'처럼 보이는 것을 방지.
    // 최종 스프라이트가 없으면 아이콘 Image 를 숨긴다. iconImage 미연결(null) 시에도 안전하게 no-op.
    private void ApplyIcon(Sprite icon, bool fallbackToEmpty)
    {
        if (iconImage == null) return;
        Sprite sprite     = icon != null ? icon : (fallbackToEmpty ? emptyIcon : null);
        iconImage.sprite  = sprite;
        iconImage.enabled = sprite != null;
    }
}
