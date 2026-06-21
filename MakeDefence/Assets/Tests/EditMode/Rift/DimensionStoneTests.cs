using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DimensionStoneTests
{
    [Test]
    public void CreateRandom_StartsWithOneOption()
    {
        var stone = DimensionStone.CreateRandom();
        Assert.AreEqual(1, stone.Options.Count);
    }

    [Test]
    public void AddRandomOption_FiveSuccessThenFalse_AndFillsToMax()
    {
        var stone = DimensionStone.CreateRandom();
        // CreateRandom 이 1개를 채웠으므로 추가는 5번까지만 성공
        for (int i = 0; i < 5; i++)
            Assert.IsTrue(stone.AddRandomOption(), $"AddRandomOption #{i + 1} 가 true 여야 함");

        Assert.AreEqual(DimensionStone.MaxOptions, stone.Options.Count);
        Assert.IsFalse(stone.AddRandomOption(), "MaxOptions 도달 후에는 false");

        // 옵션 타입은 중복되지 않음
        var seen = new HashSet<DimensionStoneOptionType>();
        foreach (var opt in stone.Options)
            Assert.IsTrue(seen.Add(opt.Type), $"옵션 타입 중복: {opt.Type}");
    }

    [Test]
    public void RemoveRandomOption_LastOneReturnsFalse()
    {
        var stone = DimensionStone.CreateRandom();
        Assert.AreEqual(1, stone.Options.Count);
        Assert.IsFalse(stone.RemoveRandomOption(), "최소 1개는 유지");
        Assert.AreEqual(1, stone.Options.Count);
    }

    [Test]
    public void UpgradeRandomOption_MultipliesBy1_5_ClampedToMax()
    {
        var stone = DimensionStone.CreateRandom();
        // 업그레이드 대상이 되려면 옵션이 2개 이상이어야 함
        stone.AddRandomOption();
        Assert.GreaterOrEqual(stone.Options.Count, 2);

        // 모든 옵션의 (현재값, 타입) 스냅샷
        var before = new List<(DimensionStoneOptionType type, float value)>();
        foreach (var opt in stone.Options) before.Add((opt.Type, opt.Value));

        Assert.IsTrue(stone.UpgradeRandomOption());

        // 정확히 1개만 변경되었고, 변경된 옵션은 기존값의 1.5배(또는 max clamp)
        int diffCount = 0;
        for (int i = 0; i < stone.Options.Count; i++)
        {
            var cur = stone.Options[i];
            var prev = before[i];
            if (cur.Type != prev.type) { Assert.Fail("옵션 타입 순서/구성이 바뀌면 안 됨"); }
            if (!Mathf.Approximately(cur.Value, prev.value))
            {
                diffCount++;
                float expected = prev.value * 1.5f;
                // clamp 가 일어났을 수 있으니 <= expected 도 허용
                Assert.LessOrEqual(cur.Value, expected + 0.001f, "업그레이드값은 1.5배 이하 (clamp)");
                Assert.Greater(cur.Value, prev.value, "업그레이드 후 값은 기존보다 커야 함");
            }
        }
        Assert.AreEqual(1, diffCount, "정확히 1개 옵션만 업그레이드되어야 함");
    }

    [Test]
    public void CanUpgrade_TrueWhenAnyOptionBelowMax()
    {
        var stone = DimensionStone.CreateRandom();
        // 초기 옵션은 5~30 범위에서 라운딩되므로 거의 항상 max(30) 미만
        Assert.IsTrue(stone.CanUpgrade(), "초기 옵션은 보통 max 미만");
    }

    [Test]
    public void UpgradeRandomOption_AllAtMax_ReturnsFalse()
    {
        var stone = DimensionStone.CreateRandom();
        // 1개 옵션을 반복 업그레이드 → 5*1.5^n 으로 빠르게 max(30) 도달
        for (int i = 0; i < 20 && stone.CanUpgrade(); i++)
            Assert.IsTrue(stone.UpgradeRandomOption());

        Assert.IsFalse(stone.CanUpgrade(), "max 도달 후 CanUpgrade false");
        Assert.IsFalse(stone.UpgradeRandomOption(), "max 도달 후 UpgradeRandomOption false (큐브 환불 트리거)");
    }

    [Test]
    public void Clone_ProducesEqualOptionsButIndependentList()
    {
        var stone = DimensionStone.CreateRandom();
        stone.AddRandomOption();
        stone.AddRandomOption();

        var copy = stone.Clone();
        Assert.AreEqual(stone.Options.Count, copy.Options.Count);
        for (int i = 0; i < stone.Options.Count; i++)
        {
            Assert.AreEqual(stone.Options[i].Type,  copy.Options[i].Type);
            Assert.AreEqual(stone.Options[i].Value, copy.Options[i].Value);
        }

        // 독립성 — 원본 변경이 복제본에 영향 없음
        stone.AddRandomOption();
        Assert.AreNotEqual(stone.Options.Count, copy.Options.Count);
    }
}
