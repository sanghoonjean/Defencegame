using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildModeToggleButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Toggle);
    }

    private void Toggle()
    {
        if (InputManager.Instance == null) return;
        var next = InputManager.Instance.CurrentBuildMode == BuildMode.Tower
            ? BuildMode.None
            : BuildMode.Tower;
        InputManager.Instance.SetBuildMode(next);

        if (next == BuildMode.Tower)
            TowerPlacer.Instance?.EnterPlacementMode();
        else
            TowerPlacer.Instance?.ExitPlacementMode();
    }
}
