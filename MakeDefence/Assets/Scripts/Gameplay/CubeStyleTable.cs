/// <summary>
/// CubeType 의 표시 이름 헬퍼 (라벨/HUD 공통 사용).
/// 색·굵기 등 시각 스타일은 DroppedCubePickup 의 인스펙터에서 직접 설정.
/// </summary>
public static class CubeStyleTable
{
    public static string GetDisplayName(CubeType type) => type switch
    {
        CubeType.Lower   => "Lower",
        CubeType.Upper   => "Upper",
        CubeType.TopTier => "TopTier",
        CubeType.Delete  => "Delete",
        CubeType.Clone   => "Clone",
        _                => string.Empty,
    };
}
