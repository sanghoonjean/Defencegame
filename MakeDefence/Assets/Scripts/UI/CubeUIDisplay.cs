using UnityEngine;
using UnityEngine.UI;

public class CubeUIDisplay : MonoBehaviour
{
    [SerializeField] private Text lowerText;
    [SerializeField] private Text upperText;
    [SerializeField] private Text topTierText;
    [SerializeField] private Text deleteText;
    [SerializeField] private Text cloneText;

    private void OnEnable()
    {
        CubeSystem.OnCubeChanged += OnCubeChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        CubeSystem.OnCubeChanged -= OnCubeChanged;
    }

    private void OnCubeChanged(CubeType type, int count)
    {
        var t = GetText(type);
        if (t != null) t.text = count.ToString();
    }

    private void RefreshAll()
    {
        if (CubeSystem.Instance == null) return;
        foreach (CubeType type in System.Enum.GetValues(typeof(CubeType)))
        {
            var t = GetText(type);
            if (t != null) t.text = CubeSystem.Instance.GetCount(type).ToString();
        }
    }

    private Text GetText(CubeType type) => type switch
    {
        CubeType.Lower   => lowerText,
        CubeType.Upper   => upperText,
        CubeType.TopTier => topTierText,
        CubeType.Delete  => deleteText,
        CubeType.Clone   => cloneText,
        _                => null,
    };
}
