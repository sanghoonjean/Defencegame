using UnityEngine;

public enum AoeDisplayMode { SimpleShape = 0, Animation = 1 }

public static class SettingsSystem
{
    private const string PrefKey = "settings.aoeDisplayMode";

    public static AoeDisplayMode AoeDisplayMode { get; private set; }

    public static event System.Action<AoeDisplayMode> OnAoeDisplayModeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        AoeDisplayMode = (AoeDisplayMode)PlayerPrefs.GetInt(PrefKey, 0);
    }

    public static void SetAoeDisplayMode(AoeDisplayMode mode)
    {
        AoeDisplayMode = mode;
        PlayerPrefs.SetInt(PrefKey, (int)mode);
        PlayerPrefs.Save();
        OnAoeDisplayModeChanged?.Invoke(mode);
    }
}
