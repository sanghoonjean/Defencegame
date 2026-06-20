using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeUIDisplay : MonoBehaviour
{
    [SerializeField] private Text lowerText;
    [SerializeField] private Text upperText;
    [SerializeField] private Text topTierText;
    [SerializeField] private Text deleteText;
    [SerializeField] private Text cloneText;

    [Header("Punch Tween")]
    [SerializeField] private float punchDuration  = 0.15f;
    [SerializeField] private float punchAmplitude = 0.3f;

    private void OnEnable()
    {
        CubeSystem.OnCubeChanged += OnCubeChanged;
        RefreshAll();
    }

    private void Start()
    {
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

    /// <summary>
    /// 큐브 타입별 카운터 Text 의 화면 위치를 카메라 평면 월드 좌표로 변환.
    /// DroppedCubeSystem 의 수확 이동 타겟으로 사용.
    /// </summary>
    public Vector3 GetCounterWorldPoint(CubeType type, Camera cam)
    {
        if (cam == null) return transform.position;
        var t = GetText(type);
        if (t == null) return transform.position;

        var rt = t.rectTransform;
        var canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null) return rt.position;

        Vector3 screenPos;
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            return rt.position;
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(null, rt.position);
        }
        else // ScreenSpaceCamera
        {
            Camera canvasCam = canvas.worldCamera != null ? canvas.worldCamera : cam;
            screenPos = RectTransformUtility.WorldToScreenPoint(canvasCam, rt.position);
        }

        float z = Mathf.Abs(cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        world.z = 0f;
        return world;
    }

    // 진행 중인 punch 추적 + 원본 스케일 캐싱 (다수 punch 겹침 대비)
    private readonly Dictionary<CubeType, Coroutine> _activePunches = new();
    private readonly Dictionary<CubeType, Vector3>   _baseScales    = new();

    /// <summary>
    /// 큐브 카운터에 punch 애니메이션 (scale 1 → 1.3 → 1).
    /// 기존 punch 진행 중이면 중단 후 재시작, 원본 스케일은 최초 1회만 캐싱.
    /// </summary>
    public void PlayPunch(CubeType type)
    {
        var t = GetText(type);
        if (t == null) return;

        if (!_baseScales.ContainsKey(type))
            _baseScales[type] = t.transform.localScale;

        if (_activePunches.TryGetValue(type, out var existing) && existing != null)
            StopCoroutine(existing);

        _activePunches[type] = StartCoroutine(PunchRoutine(type, t.transform));
    }

    private IEnumerator PunchRoutine(CubeType type, Transform target)
    {
        Vector3 baseScale = _baseScales[type];
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / punchDuration);
            float scale = 1f + Mathf.Sin(k * Mathf.PI) * punchAmplitude;
            target.localScale = baseScale * scale;
            yield return null;
        }
        target.localScale = baseScale;
        _activePunches[type] = null;
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
