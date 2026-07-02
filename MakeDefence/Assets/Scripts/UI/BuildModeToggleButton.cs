using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildModeToggleButton : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private TMP_Text tmpLabel;
    [SerializeField] private string towerLabel = "Build: Tower";
    [SerializeField] private string riftLabel  = "Build: Rift";

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Toggle);
    }

    private void OnEnable()
    {
        InputManager.OnBuildModeChanged += RefreshLabel;
        if (InputManager.Instance != null) RefreshLabel(InputManager.Instance.CurrentBuildMode);
    }

    private void OnDisable()
    {
        InputManager.OnBuildModeChanged -= RefreshLabel;
    }

    private void Toggle()
    {
        if (InputManager.Instance == null) return;
        var next = InputManager.Instance.CurrentBuildMode == BuildMode.Tower
            ? BuildMode.Rift
            : BuildMode.Tower;
        InputManager.Instance.SetBuildMode(next);

        if (next == BuildMode.Tower)
            TowerPlacer.Instance?.EnterPlacementMode();
        else
            TowerPlacer.Instance?.ExitPlacementMode();
    }

    private void RefreshLabel(BuildMode mode)
    {
        var text = mode == BuildMode.Rift ? riftLabel : towerLabel;
        if (label != null) label.text = text;
        if (tmpLabel != null) tmpLabel.text = text;
    }
}
