using System;
using System.Collections.Generic;
using UnityEngine;

// 8방향 그리드 A*. MonoBehaviour 의존 없는 순수 유틸리티라 EditMode 테스트가 가능하다.
public static class AStarPathfinder
{
    private const float StraightCost = 1f;
    private const float DiagonalCost = 1.41421356f;
    private const int MaxExploredNodes = 20000; // 60x33 맵 기준 충분한 여유. 안전장치 목적.

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public float G;
        public float H;
        public float F => G + H;
    }

    /// <summary>
    /// start에서 goal까지 8방향 최단경로를 찾는다. start와 goal 노드 자체는 isWalkable 검사에서
    /// 제외하고 항상 진입/도달 가능으로 취급한다(이웃으로 확장할 때만 검사).
    /// - start 제외 이유: 재계산 시점에 시작 셀에 막 타워가 놓인 경우에도 항상 탈출 경로를 계산할 수 있어야 함.
    /// - goal 제외 이유: 스폰 지점/본진(basePoint)은 손으로 배치한 고정 좌표라 Path/Buildable 타일 위에
    ///   있으리라는 보장이 없음(예: Decoration 타일 위에 있는 경우) — 목적지 자체는 타일 분류와 무관하게
    ///   항상 도달 가능해야 한다.
    /// 대각선 이동은 인접한 두 직교 셀 중 하나라도 막혀 있으면 금지한다(코너컷 금지).
    /// 반환 경로는 start를 포함한다. 경로가 없으면 null.
    /// </summary>
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)
    {
        if (start == goal) return new List<Vector2Int> { start };

        var startNode = new Node { Position = start, G = 0f, H = Heuristic(start, goal) };
        var open = new List<Node> { startNode };
        var openLookup = new Dictionary<Vector2Int, Node> { [start] = startNode };
        var closed = new HashSet<Vector2Int>();

        int explored = 0;
        while (open.Count > 0)
        {
            if (++explored > MaxExploredNodes) return null;

            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].F < open[bestIndex].F ||
                    (Mathf.Approximately(open[i].F, open[bestIndex].F) && open[i].H < open[bestIndex].H))
                    bestIndex = i;
            }

            Node current = open[bestIndex];
            if (current.Position == goal)
                return ReconstructPath(current);

            open.RemoveAt(bestIndex);
            openLookup.Remove(current.Position);
            closed.Add(current.Position);

            foreach (var dir in Directions)
            {
                Vector2Int neighborPos = current.Position + dir;
                if (closed.Contains(neighborPos)) continue;

                bool isDiagonal = dir.x != 0 && dir.y != 0;
                if (isDiagonal)
                {
                    var side1 = current.Position + new Vector2Int(dir.x, 0);
                    var side2 = current.Position + new Vector2Int(0, dir.y);
                    if (!IsWalkableOrEndpoint(side1, start, goal, isWalkable) || !IsWalkableOrEndpoint(side2, start, goal, isWalkable))
                        continue;
                }

                if (!IsWalkableOrEndpoint(neighborPos, start, goal, isWalkable)) continue;

                float tentativeG = current.G + (isDiagonal ? DiagonalCost : StraightCost);

                if (openLookup.TryGetValue(neighborPos, out var existing))
                {
                    if (tentativeG < existing.G)
                    {
                        existing.G = tentativeG;
                        existing.Parent = current;
                    }
                }
                else
                {
                    var neighborNode = new Node
                    {
                        Position = neighborPos,
                        G = tentativeG,
                        H = Heuristic(neighborPos, goal),
                        Parent = current,
                    };
                    open.Add(neighborNode);
                    openLookup[neighborPos] = neighborNode;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// start에서 goal로 도달 가능한지만 검사한다(BFS). FindPath와 동일한 코너컷/시작·목표셀 예외
    /// 규칙을 적용해 실제 이동 가능성과 일치하는 결과를 보장한다. 봉쇄 방지(WouldSeverPath) 용도.
    /// </summary>
    public static bool IsReachable(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)
    {
        if (start == goal) return true;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        int explored = 0;
        while (queue.Count > 0)
        {
            if (++explored > MaxExploredNodes) return false;

            var current = queue.Dequeue();
            foreach (var dir in Directions)
            {
                var neighbor = current + dir;
                if (visited.Contains(neighbor)) continue;

                bool isDiagonal = dir.x != 0 && dir.y != 0;
                if (isDiagonal)
                {
                    var side1 = current + new Vector2Int(dir.x, 0);
                    var side2 = current + new Vector2Int(0, dir.y);
                    if (!IsWalkableOrEndpoint(side1, start, goal, isWalkable) || !IsWalkableOrEndpoint(side2, start, goal, isWalkable))
                        continue;
                }

                if (!IsWalkableOrEndpoint(neighbor, start, goal, isWalkable)) continue;
                if (neighbor == goal) return true;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return false;
    }

    // start/goal 노드 자체는 isWalkable 검사 대상에서 제외한다(둘 다 손으로 배치한 고정 좌표라
    // 타일 분류와 무관하게 항상 진입/도달 가능해야 함). 이웃으로 확장할 때만 실제 검사한다.
    private static bool IsWalkableOrEndpoint(Vector2Int pos, Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)
        => pos == start || pos == goal || isWalkable(pos);

    // Octile distance — 8방향 이동에서 admissible한 휴리스틱.
    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return StraightCost * (dx + dy) + (DiagonalCost - 2f * StraightCost) * Mathf.Min(dx, dy);
    }

    private static List<Vector2Int> ReconstructPath(Node node)
    {
        var path = new List<Vector2Int>();
        while (node != null)
        {
            path.Add(node.Position);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }
}
