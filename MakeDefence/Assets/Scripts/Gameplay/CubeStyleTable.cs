using UnityEngine;

/// <summary>
/// CubeType -> 시각 스타일 매핑 헬퍼.
/// DroppedCubePickup / 향후 인벤·숍 UI 에서 공통 사용.
/// </summary>
public static class CubeStyleTable
{
    public readonly struct CubeStyle
    {
        public readonly Color  BodyColor;
        public readonly Color  BeamColor;        // alpha 포함
        public readonly float  BeamWidth;
        public readonly Color  LabelBorderColor;
        public readonly Color  LabelBgColor;     // alpha 포함
        public readonly Color  LabelTextColor;
        public readonly string DisplayName;

        public CubeStyle(Color body, Color beam, float beamWidth,
                         Color labelBorder, Color labelBg, Color labelText,
                         string displayName)
        {
            BodyColor        = body;
            BeamColor        = beam;
            BeamWidth        = beamWidth;
            LabelBorderColor = labelBorder;
            LabelBgColor     = labelBg;
            LabelTextColor   = labelText;
            DisplayName      = displayName;
        }
    }

    public static CubeStyle Get(CubeType type) => type switch
    {
        CubeType.Lower   => new CubeStyle(
            Hex("#A0A0A0"), HexA("#A0A0A0", 0.25f), 0.06f,
            Hex("#A0A0A0"), HexA("#3A3A3A", 0.90f), Hex("#E0E0E0"),
            "Lower"),
        CubeType.Upper   => new CubeStyle(
            Hex("#4A8BFF"), HexA("#4A8BFF", 0.35f), 0.07f,
            Hex("#4A8BFF"), HexA("#1A3060", 0.90f), Hex("#7AB3FF"),
            "Upper"),
        CubeType.TopTier => new CubeStyle(
            Hex("#FFC93A"), HexA("#FFC93A", 0.55f), 0.10f,
            Hex("#FFC93A"), HexA("#5C4316", 0.90f), Hex("#FFD86F"),
            "TopTier"),
        CubeType.Delete  => new CubeStyle(
            Hex("#E55050"), HexA("#E55050", 0.35f), 0.07f,
            Hex("#E55050"), HexA("#5A1E1E", 0.90f), Hex("#FF8585"),
            "Delete"),
        CubeType.Clone   => new CubeStyle(
            Hex("#B07FFF"), HexA("#B07FFF", 0.55f), 0.10f,
            Hex("#B07FFF"), HexA("#3D2A60", 0.90f), Hex("#D0A9FF"),
            "Clone"),
        _ => default
    };

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    private static Color HexA(string hex, float alpha)
    {
        var c = Hex(hex);
        c.a = alpha;
        return c;
    }
}
