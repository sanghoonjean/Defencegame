using System.Collections;
using UnityEngine;

/// <summary>
/// 차원석 픽업 비주얼. 큐브 픽업과 동일한 라벨 패턴 (Border + Bg + Text).
/// "차원석" 이름만 표시. 수확 아크 없이 그 자리에서 fade out.
/// </summary>
public class DroppedStonePickup : MonoBehaviour
{
    [System.Serializable]
    public struct LabelStyle
    {
        public Color borderColor;
        public Color bgColor;
        public Color textColor;
    }

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer labelBorder;
    [SerializeField] private SpriteRenderer labelBg;
    [SerializeField] private TextMesh       labelText;

    [Header("Label Style")]
    [SerializeField] private LabelStyle style = new() {
        borderColor = new Color(0.690f, 0.498f, 1.000f, 1f),
        bgColor     = new Color(0.239f, 0.165f, 0.376f, 0.9f),
        textColor   = new Color(0.816f, 0.663f, 1.000f, 1f),
    };

    [SerializeField] private string displayName = "차원석";

    [Header("Spawn Effect")]
    [SerializeField] private float spawnEffectDuration = 0.25f;

    private bool      _movementLocked;
    private Coroutine _spawnCoroutine;
    private float     _labelBorderBaseAlpha;
    private float     _labelBgBaseAlpha;
    private float     _labelTextBaseAlpha;

    public void Initialize(Vector2 worldPos)
    {
        if (labelBorder != null) labelBorder.color = style.borderColor;
        if (labelBg     != null) labelBg.color     = style.bgColor;
        if (labelText   != null)
        {
            labelText.text  = displayName;
            labelText.color = style.textColor;
        }

        _labelBorderBaseAlpha = labelBorder != null ? labelBorder.color.a : 0f;
        _labelBgBaseAlpha     = labelBg     != null ? labelBg.color.a     : 0f;
        _labelTextBaseAlpha   = labelText   != null ? labelText.color.a   : 0f;

        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
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
            SetGroupAlpha(k);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void SetGroupAlpha(float a)
    {
        if (labelBorder != null) { var c = labelBorder.color; c.a = _labelBorderBaseAlpha * a; labelBorder.color = c; }
        if (labelBg     != null) { var c = labelBg.color;     c.a = _labelBgBaseAlpha     * a; labelBg.color     = c; }
        if (labelText   != null) { var c = labelText.color;   c.a = _labelTextBaseAlpha   * a; labelText.color   = c; }
    }

    private void OnDestroy()
    {
        if (DroppedStoneSystem.Instance != null)
            DroppedStoneSystem.Instance.UnregisterPickup(this);
    }
}
