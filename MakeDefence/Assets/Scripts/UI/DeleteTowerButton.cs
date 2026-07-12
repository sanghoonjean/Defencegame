using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특정 <see cref="UnitSpawnButton"/> 과 1:1 로 짝지어진 전용 삭제 버튼.
/// 짝 스폰 버튼이 유닛을 배치했을 때만 GameObject 가 활성화되며(활성/비활성 토글은
/// <see cref="UnitSpawnButton"/> 이 담당), 클릭 시 그 스폰 버튼이 배치한 유닛만
/// 삭제 확인 팝업으로 넘긴다. 현재 선택 타워와 무관하게 항상 자기 유닛만 삭제한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class DeleteTowerButton : MonoBehaviour
{
    [Tooltip("이 삭제 버튼이 담당하는 스폰 버튼. 이 버튼이 배치한 유닛만 삭제한다.")]
    [SerializeField] private UnitSpawnButton spawnButton;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (spawnButton == null) return;

        Tower target = spawnButton.PlacedTower;
        if (target == null) return; // 유닛이 없으면(버튼이 노출됐어도) 무시

        if (TowerDeleteConfirmPopup.Instance != null)
            TowerDeleteConfirmPopup.Instance.Show(target);
    }
}
