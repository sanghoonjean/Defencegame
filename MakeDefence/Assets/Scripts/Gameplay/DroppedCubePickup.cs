using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 사망 위치에 떠 있는 큐브 픽업 (수확 전 상태).
/// 라벨 색은 등급별로 인스펙터에서 설정.
/// Body/Beam 의 반짝임(알파 펄스)도 인스펙터 PulseStyle 로 조정.
/// </summary>
public class DroppedCubePickup : MonoBehaviour
{
    /// <summary>
    /// 등급별 라벨 색 묶음 (Inspector 에서 직접 조정).
    /// </summary>
    [System.Serializable]
    public struct LabelStyle
    {
        public Color borderColor;
        public Color bgColor;     // alpha 포함
        public Color textColor;
    }

    /// <summary>
    /// 알파 펄스 (반짝임) 파라미터. amplitude=0 이면 비활성.
    /// 최종 알파 = base * (1 + sin(2π·f·t) · amplitude), clamp01.
    /// </summary>
    [System.Serializable]
    public struct PulseStyle
    {
        [Range(0f, 1f)] public float amplitude;  // 0~1 (0 = 비활성)
        public float                 frequency;  // Hz
    }

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer beam;
    [SerializeField] private SpriteRenderer labelBorder;
    [SerializeField] private SpriteRenderer labelBg;
    [SerializeField] private TextMesh       labelText;

    [Header("Label Style per Grade")]
    [SerializeField] private LabelStyle lowerStyle   = new() {
        borderColor = new Color(0.627f, 0.627f, 0.627f, 1f),
        bgColor     = new Color(0.227f, 0.227f, 0.227f, 0.9f),
        textColor   = new Color(0.878f, 0.878f, 0.878f, 1f),
    };
    [SerializeField] private LabelStyle upperStyle   = new() {
        borderColor = new Color(0.290f, 0.545f, 1.000f, 1f),
        bgColor     = new Color(0.102f, 0.188f, 0.376f, 0.9f),
        textColor   = new Color(0.478f, 0.702f, 1.000f, 1f),
    };
    [SerializeField] private LabelStyle topTierStyle = new() {
        borderColor = new Color(1.000f, 0.788f, 0.227f, 1f),
        bgColor     = new Color(0.361f, 0.263f, 0.086f, 0.9f),
        textColor   = new Color(1.000f, 0.847f, 0.435f, 1f),
    };
    [SerializeField] private LabelStyle deleteStyle  = new() {
        borderColor = new Color(0.898f, 0.314f, 0.314f, 1f),
        bgColor     = new Color(0.353f, 0.118f, 0.118f, 0.9f),
        textColor   = new Color(1.000f, 0.522f, 0.522f, 1f),
    };
    [SerializeField] private LabelStyle cloneStyle   = new() {
        borderColor = new Color(0.690f, 0.498f, 1.000f, 1f),
        bgColor     = new Color(0.239f, 0.165f, 0.376f, 0.9f),
        textColor   = new Color(0.816f, 0.663f, 1.000f, 1f),
    };

    [Header("Pulse (반짝임)")]
    [SerializeField] private PulseStyle bodyPulse = new() { amplitude = 0.25f, frequency = 1.5f };
    [SerializeField] private PulseStyle beamPulse = new() { amplitude = 0.20f, frequency = 2.0f };

    [Header("Spawn Effect")]
    [SerializeField] private float spawnEffectDuration = 0.25f;

    [Header("Collect Arc")]
    [SerializeField] private float collectArcApexHeight = 1.5f;

    public CubeType Type { get; private set; }

    private Vector3   _basePos;
    private bool      _movementLocked;
    private Coroutine _spawnCoroutine;
    private float     _pulseStartTime;
    private float     _bodyBaseAlpha;
    private float     _beamBaseAlpha;
    private float     _labelBorderBaseAlpha;
    private float     _labelBgBaseAlpha;
    private float     _labelTextBaseAlpha;

    public void Initialize(CubeType type, Vector2 worldPos)
    {
        Type = type;
        var style = GetStyle(type);

        if (labelBorder != null) labelBorder.color = style.borderColor;
        if (labelBg     != null) labelBg.color     = style.bgColor;
        if (labelText   != null)
        {
            labelText.text  = CubeStyleTable.GetDisplayName(type);
            labelText.color = style.textColor;
        }

        // collect/discard 페이드에 곱할 기준 알파 캐싱
        _bodyBaseAlpha        = body        != null ? body.color.a        : 0f;
        _beamBaseAlpha        = beam        != null ? beam.color.a        : 0f;
        _labelBorderBaseAlpha = labelBorder != null ? labelBorder.color.a : 0f;
        _labelBgBaseAlpha     = labelBg     != null ? labelBg.color.a     : 0f;
        _labelTextBaseAlpha   = labelText   != null ? labelText.color.a   : 0f;

        _basePos = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        transform.position = _basePos;
        DroppedPickupRegistry.Register(transform);
        _pulseStartTime = Time.time;
        _spawnCoroutine = StartCoroutine(SpawnEffect());
    }

    private void Update()
    {
        if (_movementLocked) return;
        float t = Time.time - _pulseStartTime;
        if (body != null && bodyPulse.amplitude > 0f)
        {
            float m = 1f + Mathf.Sin(t * bodyPulse.frequency * Mathf.PI * 2f) * bodyPulse.amplitude;
            var c = body.color; c.a = Mathf.Clamp01(_bodyBaseAlpha * m); body.color = c;
        }
        if (beam != null && beamPulse.amplitude > 0f)
        {
            float m = 1f + Mathf.Sin(t * beamPulse.frequency * Mathf.PI * 2f) * beamPulse.amplitude;
            var c = beam.color; c.a = Mathf.Clamp01(_beamBaseAlpha * m); beam.color = c;
        }
    }

    private LabelStyle GetStyle(CubeType type) => type switch
    {
        CubeType.Lower   => lowerStyle,
        CubeType.Upper   => upperStyle,
        CubeType.TopTier => topTierStyle,
        CubeType.Delete  => deleteStyle,
        CubeType.Clone   => cloneStyle,
        _                => lowerStyle,
    };

    private IEnumerator SpawnEffect()
    {
        float t = 0f;
        transform.localScale = Vector3.zero;
        while (t < spawnEffectDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / spawnEffectDuration);
            float s = k < 0.6f
                ? Mathf.Lerp(0f, 1.2f, k / 0.6f)
                : Mathf.Lerp(1.2f, 1.0f, (k - 0.6f) / 0.4f);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        _spawnCoroutine = null;
    }

    public void StartCollect(Vector3 targetWorldPos, float duration, Action onArrived)
    {
        if (_movementLocked) return;
        _movementLocked = true;
        StopSpawnIfRunning();
        transform.localScale = Vector3.one;
        StartCoroutine(CollectRoutine(targetWorldPos, duration, onArrived));
    }

    private IEnumerator CollectRoutine(Vector3 target, float duration, Action onArrived)
    {
        Vector3 start = transform.position;
        Vector3 mid   = (start + target) * 0.5f + Vector3.up * collectArcApexHeight;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            Vector3 a = Vector3.Lerp(start, mid, k);
            Vector3 b = Vector3.Lerp(mid, target, k);
            transform.position = Vector3.Lerp(a, b, k);
            if (k > 0.7f)
            {
                float fade = Mathf.InverseLerp(1f, 0.7f, k);
                SetGroupAlpha(fade);
            }
            yield return null;
        }
        onArrived?.Invoke();
        Destroy(gameObject);
    }

    public void StartDiscard(float fadeDuration)
    {
        if (_movementLocked) return;
        _movementLocked = true;
        StopSpawnIfRunning();
        StartCoroutine(DiscardRoutine(fadeDuration));
    }

    private IEnumerator DiscardRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            SetGroupAlpha(k);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void StopSpawnIfRunning()
    {
        if (_spawnCoroutine == null) return;
        StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = null;
    }

    private void SetGroupAlpha(float a)
    {
        if (body != null)
        {
            var c = body.color; c.a = _bodyBaseAlpha * a; body.color = c;
        }
        if (beam != null)
        {
            var c = beam.color; c.a = _beamBaseAlpha * a; beam.color = c;
        }
        if (labelBorder != null)
        {
            var c = labelBorder.color; c.a = _labelBorderBaseAlpha * a; labelBorder.color = c;
        }
        if (labelBg != null)
        {
            var c = labelBg.color; c.a = _labelBgBaseAlpha * a; labelBg.color = c;
        }
        if (labelText != null)
        {
            var c = labelText.color; c.a = _labelTextBaseAlpha * a; labelText.color = c;
        }
    }

    private void OnDestroy()
    {
        DroppedPickupRegistry.Unregister(transform);
        if (DroppedCubeSystem.Instance != null)
            DroppedCubeSystem.Instance.UnregisterPickup(this);
    }
}
