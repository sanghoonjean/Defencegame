using UnityEngine;
using UnityEngine.UI;

public class UIToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void Toggle()
    {
        if (targetPanel == null)
        {
            Debug.LogWarning($"[UIToggleButton] targetPanel이 연결되지 않았습니다 — {gameObject.name}");
            return;
        }
        targetPanel.SetActive(!targetPanel.activeSelf);
    }
}
