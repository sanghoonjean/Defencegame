using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템 호버 툴팁 패널. 씬 수정 없이 루트 캔버스 아래에 런타임 생성/캐싱한다
/// (StoneGradeBadge #394 패턴). 캔버스당 1개만 만들어 재사용하며,
/// raycastTarget 을 모두 꺼서 호버 플리커를 방지한다.
/// </summary>
public static class ItemTooltipUI
{
    private const float   Padding   = 10f;
    private const float   MaxWidth  = 340f;
    private const int     FontSize  = 20;
    private static readonly Vector2 AnchorGap = new(8f, 0f);

    private static RectTransform _panel;
    private static Text          _text;
    private static RectTransform _canvasRt;

    /// <summary>anchor 슬롯의 우상단 옆에 툴팁을 표시한다. 캔버스 밖으로 나가지 않게 클램프.</summary>
    public static void Show(RectTransform anchor, string content)
    {
        if (anchor == null || string.IsNullOrEmpty(content)) return;

        var canvas = anchor.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var rootRt = (RectTransform)canvas.rootCanvas.transform;

        if (_panel == null || _canvasRt != rootRt)
            Create(rootRt);

        _text.text = content;

        // 텍스트 크기에 맞춰 패널 리사이즈 (MaxWidth 초과 시 줄바꿈)
        float w = Mathf.Min(_text.preferredWidth, MaxWidth);
        _text.rectTransform.sizeDelta = new Vector2(w, 0f);
        float h = _text.preferredHeight;
        _text.rectTransform.sizeDelta = new Vector2(w, h);

        var size = new Vector2(w + Padding * 2f, h + Padding * 2f);
        _panel.sizeDelta = size;

        // 슬롯 우상단 기준 배치 후 캔버스 rect 안으로 클램프
        var corners = new Vector3[4];
        anchor.GetWorldCorners(corners); // 0:BL 1:TL 2:TR 3:BR
        Vector2 pos = (Vector2)_canvasRt.InverseTransformPoint(corners[2]) + AnchorGap;

        Rect cr = _canvasRt.rect;
        pos.x = Mathf.Clamp(pos.x, cr.xMin, cr.xMax - size.x);
        pos.y = Mathf.Clamp(pos.y, cr.yMin + size.y, cr.yMax);

        _panel.localPosition = pos;
        _panel.SetAsLastSibling();
        _panel.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (_panel != null) _panel.gameObject.SetActive(false);
    }

    private static void Create(RectTransform canvasRt)
    {
        _canvasRt = canvasRt;

        var go = new GameObject("ItemTooltip", typeof(RectTransform));
        _panel = (RectTransform)go.transform;
        _panel.SetParent(canvasRt, false);
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot     = new Vector2(0f, 1f);

        var bg = go.AddComponent<Image>();
        bg.color         = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        var textRt = (RectTransform)textGo.transform;
        textRt.SetParent(_panel, false);
        textRt.anchorMin        = textRt.anchorMax = new Vector2(0f, 1f);
        textRt.pivot            = new Vector2(0f, 1f);
        textRt.anchoredPosition = new Vector2(Padding, -Padding);

        _text = textGo.AddComponent<Text>();
        _text.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize           = FontSize;
        _text.color              = Color.white;
        _text.raycastTarget      = false;
        _text.supportRichText    = true;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow   = VerticalWrapMode.Overflow;
    }
}
