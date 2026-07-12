using TMPro;
using UnityEngine;

/// <summary>
/// UnitPanel 내 직업 이름을 표시한다. OnTowerSelected 이벤트를 구독해 자동 갱신.
/// </summary>
public class JobClassDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI jobLabel;

    private void OnEnable()
    {
        InventorySystem.OnTowerSelected += Refresh;
        Refresh(InventorySystem.Instance?.SelectedTower);
    }

    private void OnDisable()
    {
        InventorySystem.OnTowerSelected -= Refresh;
    }

private void Refresh(Tower tower)
    {
        if (jobLabel == null) return;
        if (tower == null) { jobLabel.text = ""; return; }

        jobLabel.text = tower.Job switch
        {
            JobClass.Warrior => "Warrior",
            JobClass.Mage    => "Mage",
            JobClass.Archer  => "Archer",
            _                => "",
        };
    }
}
