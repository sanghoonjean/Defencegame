using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Toggle     aoeAnimationToggle;
    [SerializeField] private Button     openButton;
    [SerializeField] private Button     closeButton;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (aoeAnimationToggle != null)
        {
            aoeAnimationToggle.SetIsOnWithoutNotify(
                SettingsSystem.AoeDisplayMode == AoeDisplayMode.Animation);
            aoeAnimationToggle.onValueChanged.AddListener(OnAoeToggleChanged);
        }

        if (openButton  != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnDestroy()
    {
        if (aoeAnimationToggle != null)
            aoeAnimationToggle.onValueChanged.RemoveListener(OnAoeToggleChanged);
        if (openButton  != null) openButton.onClick.RemoveListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
    }

    private void OnAoeToggleChanged(bool isOn)
    {
        SettingsSystem.SetAoeDisplayMode(isOn ? AoeDisplayMode.Animation : AoeDisplayMode.SimpleShape);
    }

    private void OpenPanel()
    {
        if (panel != null) panel.SetActive(true);
    }

    private void ClosePanel()
    {
        if (panel != null) panel.SetActive(false);
    }
}
