using System.Collections;
using UnityEngine;

/// <summary>
/// 차원석 픽업 비주얼 (수확 아크 없는 단순 spawn pop + pulse + fade out).
/// Sprite 는 사용자가 인스펙터에서 직접 설정 — 본 작업은 placeholder color 만.
/// </summary>
public class DroppedStonePickup : MonoBehaviour
{
    [System.Serializable]
    public struct PulseStyle
    {
        [Range(0f, 1f)] public float amplitude;
        public float                 frequency;
    }

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer body;

    [Header("Placeholder Color (sprite 미할당 시 색만 표시)")]
    [SerializeField] private Color placeholderColor = new(0.55f, 0.2f, 0.85f, 1f);

    [Header("Pulse (반짝임)")]
    [SerializeField] private PulseStyle bodyPulse = new() { amplitude = 0.25f, frequency = 1.5f };

    [Header("Spawn Effect")]
    [SerializeField] private float spawnEffectDuration = 0.25f;

    private float _baseAlpha;
    private float _pulseStartTime;
    private bool  _movementLocked;
    private Coroutine _spawnCoroutine;

    private void Awake()
    {
        if (body == null) body = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Vector2 worldPos)
    {
        if (body != null)
        {
            body.color = placeholderColor;
            _baseAlpha = placeholderColor.a;
        }
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        _pulseStartTime = Time.time;
        _spawnCoroutine = StartCoroutine(SpawnEffect());
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
                : Mathf.Lerp(1.2f, 1f, (k - 0.6f) / 0.4f);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        _spawnCoroutine = null;
    }

    private void Update()
    {
        if (_movementLocked) return;
        if (body == null || bodyPulse.amplitude <= 0f) return;
        float t = Time.time - _pulseStartTime;
        float m = 1f + Mathf.Sin(t * bodyPulse.frequency * Mathf.PI * 2f) * bodyPulse.amplitude;
        var c = body.color; c.a = Mathf.Clamp01(_baseAlpha * m); body.color = c;
    }

    public void StartCollectFade(float duration) => StartFade(duration);
    public void StartDiscardFade(float duration) => StartFade(duration);

    private void StartFade(float duration)
    {
        if (_movementLocked) return;
        _movementLocked = true;
        if (_spawnCoroutine != null) { StopCoroutine(_spawnCoroutine); _spawnCoroutine = null; }
        transform.localScale = Vector3.one;
        StartCoroutine(FadeRoutine(duration));
    }

    private IEnumerator FadeRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            if (body != null)
            {
                var c = body.color; c.a = _baseAlpha * k; body.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (DroppedStoneSystem.Instance != null)
            DroppedStoneSystem.Instance.UnregisterPickup(this);
    }
}
