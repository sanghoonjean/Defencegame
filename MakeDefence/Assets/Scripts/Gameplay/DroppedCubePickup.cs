using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 사망 위치에 떠 있는 큐브 픽업 (수확 전 상태).
/// 스폰 이펙트 + 부유/펄스 + 클리어 수확 / 실패 폐기 시각 효과.
/// </summary>
public class DroppedCubePickup : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer beam;
    [SerializeField] private SpriteRenderer labelBorder;
    [SerializeField] private SpriteRenderer labelBg;
    [SerializeField] private TextMesh       labelText;

    [Header("Idle Float")]
    [SerializeField] private float bobAmplitude   = 0.08f;
    [SerializeField] private float bobFrequency   = 1.5f;
    [SerializeField] private float pulseAmplitude = 0.15f;
    [SerializeField] private float pulseFrequency = 2.0f;

    [Header("Spawn Effect")]
    [SerializeField] private float spawnEffectDuration = 0.25f;
    [SerializeField] private float spawnBounceHeight   = 0.4f;

    [Header("Collect Arc")]
    [SerializeField] private float collectArcApexHeight = 1.5f;

    public CubeType Type { get; private set; }

    private Vector3 _basePos;
    private float   _spawnTime;
    private bool    _movementLocked;
    private Color   _bodyBaseColor;
    private Color   _beamBaseColor;
    private Color   _labelBorderBaseColor;
    private Color   _labelBgBaseColor;
    private Color   _labelTextBaseColor;

    public void Initialize(CubeType type, Vector2 worldPos)
    {
        Type = type;
        var style = CubeStyleTable.Get(type);
        _bodyBaseColor        = style.BodyColor;
        _beamBaseColor        = style.BeamColor;
        _labelBorderBaseColor = style.LabelBorderColor;
        _labelBgBaseColor     = style.LabelBgColor;
        _labelTextBaseColor   = style.LabelTextColor;

        if (body != null) body.color = _bodyBaseColor;
        if (beam != null)
        {
            beam.color = _beamBaseColor;
            var s = beam.transform.localScale;
            s.x = style.BeamWidth;
            beam.transform.localScale = s;
        }
        if (labelBorder != null) labelBorder.color = _labelBorderBaseColor;
        if (labelBg     != null) labelBg.color     = _labelBgBaseColor;
        if (labelText   != null)
        {
            labelText.text  = style.DisplayName;
            labelText.color = _labelTextBaseColor;
        }

        _basePos = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        transform.position = _basePos;
        _spawnTime = Time.time;
        StartCoroutine(SpawnEffect());
    }

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
            float yo = Mathf.Sin(k * Mathf.PI) * spawnBounceHeight;
            transform.position = _basePos + Vector3.up * yo;
            yield return null;
        }
        transform.localScale = Vector3.one;
        transform.position   = _basePos;
    }

    private void Update()
    {
        if (_movementLocked) return;
        float t = Time.time - _spawnTime;
        float yo = Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = _basePos + Vector3.up * yo;

        float pulse = 1f + Mathf.Sin(t * pulseFrequency * Mathf.PI * 2f) * pulseAmplitude;
        if (body != null)
        {
            var c = _bodyBaseColor;
            c.a = Mathf.Clamp01(_bodyBaseColor.a * pulse);
            body.color = c;
        }
        if (beam != null)
        {
            var c = _beamBaseColor;
            c.a = Mathf.Clamp01(_beamBaseColor.a * pulse);
            beam.color = c;
        }
    }

    public void StartCollect(Vector3 targetWorldPos, float duration, Action onArrived)
    {
        if (_movementLocked) return;
        _movementLocked = true;
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

    private void SetGroupAlpha(float a)
    {
        if (body != null)
        {
            var c = body.color; c.a = _bodyBaseColor.a * a; body.color = c;
        }
        if (beam != null)
        {
            var c = beam.color; c.a = _beamBaseColor.a * a; beam.color = c;
        }
        if (labelBorder != null)
        {
            var c = labelBorder.color; c.a = _labelBorderBaseColor.a * a; labelBorder.color = c;
        }
        if (labelBg != null)
        {
            var c = labelBg.color; c.a = _labelBgBaseColor.a * a; labelBg.color = c;
        }
        if (labelText != null)
        {
            var c = labelText.color; c.a = _labelTextBaseColor.a * a; labelText.color = c;
        }
    }

    private void OnDestroy()
    {
        if (DroppedCubeSystem.Instance != null)
            DroppedCubeSystem.Instance.UnregisterPickup(this);
    }
}
