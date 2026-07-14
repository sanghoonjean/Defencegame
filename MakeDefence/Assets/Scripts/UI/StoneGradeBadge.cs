using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 ICON Image 우하단에 차원석 등급 숫자(1~4)를 표시하는 배지 (#394).
/// 씬 수정 없이 코드에서 Text 자식을 런타임 생성/캐싱한다.
/// stone 이 null 이면 숨김 — 스킬/서포트 슬롯에는 표시되지 않는다.
/// </summary>
public static class StoneGradeBadge
{
    private const string BadgeName = "StoneGradeBadge";
    private const int    FontSize  = 28;
    private static readonly Vector2 BadgeSize   = new(34f, 32f);
    private static readonly Vector2 BadgeOffset = new(-2f, 2f);

    public static void Set(Image iconImage, DimensionStone stone)
    {
        if (iconImage == null) return;

        var badgeTr = iconImage.transform.Find(BadgeName);
        if (stone == null)
        {
            if (badgeTr != null) badgeTr.gameObject.SetActive(false);
            return;
        }

        Text text;
        if (badgeTr == null)
            text = Create(iconImage);
        else
            text = badgeTr.GetComponent<Text>();
        if (text == null) return;

        text.text = ((int)stone.Grade + 1).ToString();
        text.gameObject.SetActive(true);
    }

    private static Text Create(Image iconImage)
    {
        var go = new GameObject(BadgeName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(iconImage.transform, false);
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = BadgeOffset;
        rt.sizeDelta        = BadgeSize;

        var text = go.AddComponent<Text>();
        text.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize      = FontSize;
        text.fontStyle     = FontStyle.Bold;
        text.alignment     = TextAnchor.LowerRight;
        text.color         = Color.white;
        text.raycastTarget = false;

        // 아이콘과 겹쳐도 읽히도록 외곽선
        var outline = go.AddComponent<Outline>();
        outline.effectColor    = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);

        return text;
    }
}
