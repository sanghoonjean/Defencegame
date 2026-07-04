using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AStarPathfinderTests
{
    private static Func<Vector2Int, bool> Grid(int width, int height, HashSet<Vector2Int> blocked = null)
    {
        blocked ??= new HashSet<Vector2Int>();
        return cell => cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height && !blocked.Contains(cell);
    }

    [Test]
    public void FindPath_EmptyGrid_UsesDiagonalShortestPath()
    {
        var path = AStarPathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 3), Grid(5, 5));

        Assert.IsNotNull(path);
        Assert.AreEqual(4, path.Count); // (0,0)->(1,1)->(2,2)->(3,3), 대각선 최단경로
        Assert.AreEqual(new Vector2Int(0, 0), path[0]);
        Assert.AreEqual(new Vector2Int(3, 3), path[^1]);
    }

    [Test]
    public void FindPath_SingleObstacle_DetoursAroundIt()
    {
        var blocked = new HashSet<Vector2Int> { new Vector2Int(2, 2) };
        var path = AStarPathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 4), Grid(5, 5, blocked));

        Assert.IsNotNull(path);
        CollectionAssert.DoesNotContain(path, new Vector2Int(2, 2));
        Assert.AreEqual(new Vector2Int(4, 4), path[^1]);
    }

    [Test]
    public void FindPath_CornerCut_DisallowedWhenOnlyOneOrthogonalSideBlocked()
    {
        // (1,0)만 막히고 (0,1)은 열려 있음 — "둘 다 막혔을 때만 금지"하는 느슨한 규칙이면
        // (0,0)->(1,1) 대각선 직행이 허용되지만, 강화된 규칙에서는 금지되어야 한다.
        var blocked = new HashSet<Vector2Int> { new Vector2Int(1, 0) };
        var path = AStarPathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(1, 1), Grid(3, 3, blocked));

        Assert.IsNotNull(path);
        Assert.AreEqual(3, path.Count, "대각선 직행이 금지되어 (0,1)을 거쳐가는 L자 경로가 나와야 한다");
        Assert.AreEqual(new Vector2Int(0, 1), path[1]);
    }

    [Test]
    public void FindPath_FullyBlocked_ReturnsNull()
    {
        // 1행짜리 통로에서는 대각선 이동이 불가능하므로, 가운데 칸을 막으면 완전히 단절된다.
        var blocked = new HashSet<Vector2Int> { new Vector2Int(1, 0) };
        var path = AStarPathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 0), Grid(3, 1, blocked));

        Assert.IsNull(path);
    }

    [Test]
    public void FindPath_StartCellNotWalkable_StillFindsPath()
    {
        var blocked = new HashSet<Vector2Int> { new Vector2Int(0, 0) };
        var path = AStarPathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 2), Grid(3, 3, blocked));

        Assert.IsNotNull(path, "시작 셀 자체가 walkable이 아니어도 경로를 찾아야 한다");
        Assert.AreEqual(new Vector2Int(0, 0), path[0]);
    }

    [Test]
    public void FindPath_StartEqualsGoal_ReturnsSingleElementPath()
    {
        var path = AStarPathfinder.FindPath(new Vector2Int(2, 2), new Vector2Int(2, 2), Grid(5, 5));

        Assert.IsNotNull(path);
        Assert.AreEqual(1, path.Count);
        Assert.AreEqual(new Vector2Int(2, 2), path[0]);
    }

    [Test]
    public void IsReachable_ConnectedGrid_ReturnsTrue()
    {
        Assert.IsTrue(AStarPathfinder.IsReachable(new Vector2Int(0, 0), new Vector2Int(4, 4), Grid(5, 5)));
    }

    [Test]
    public void IsReachable_SeveredCorridor_ReturnsFalse()
    {
        var blocked = new HashSet<Vector2Int> { new Vector2Int(1, 0) };
        Assert.IsFalse(AStarPathfinder.IsReachable(new Vector2Int(0, 0), new Vector2Int(2, 0), Grid(3, 1, blocked)));
    }
}
