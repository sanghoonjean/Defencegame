using System;
using System.Collections.Generic;
using UnityEngine;

// 실제 A* 경로 계산 담당. 그리드/타워 데이터는 MapTileSystem에서 가져온다.
public class PathfindingSystem : MonoBehaviour
{
    public static PathfindingSystem Instance { get; private set; }

    // 타워 배치/이동/삭제로 경로가 재계산될 때마다 invoke (RecalculateActiveEnemyPaths 참고).
    // 살아있는 적이 0명이어도 무조건 invoke — 경로 시각화(MonsterPathVisualizer)는 웨이브
    // 진행 여부와 무관하게 항상 최신 경로를 반영해야 하므로.
    public static event Action OnPathsChanged;

    private void Awake()
    {
        Instance = this;
    }

    public int RouteCount => MapTileSystem.Instance != null ? MapTileSystem.Instance.RouteCount : 0;

    public Vector2 GetSpawnPoint() => GetSpawnPoint(0);
    public Vector2 GetSpawnPoint(int routeIndex)
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetSpawnPoint(routeIndex) : Vector2.zero;
    }

    public Vector2 GetBasePoint()
    {
        return MapTileSystem.Instance != null ? MapTileSystem.Instance.GetBasePoint() : Vector2.zero;
    }

    /// <summary>
    /// fromWorld → toWorld 사이를 타워를 피해가는 A* 최단경로로 계산해 반환한다. 결과 좌표는
    /// 각 셀 중심(+0.5)으로 정렬된다.
    /// includeStart가 false면 시작 셀을 결과에서 제거한다(이미 그 위치에 있는 살아있는 몬스터의
    /// 실시간 재계산용 — Enemy.SetPath는 인덱스 0부터 바로 다음 목표를 향해 이동해야 하므로).
    /// 단, 시작 셀과 목표 셀이 같으면 includeStart 값과 무관하게 목표 웨이포인트 1개는 항상
    /// 보존한다 — 그렇지 않으면 빈 배열이 반환되어 Enemy.MoveAlongPath가 멈추고 ReachBase가
    /// 영원히 호출되지 않는 웨이브 stuck 버그가 생긴다.
    /// </summary>
    public Vector2[] ComputePath(Vector2 fromWorld, Vector2 toWorld, bool includeStart)
    {
        if (MapTileSystem.Instance == null) return Array.Empty<Vector2>();

        var startCell = WorldToCell(fromWorld);
        var goalCell = WorldToCell(toWorld);

        var cellPath = AStarPathfinder.FindPath(startCell, goalCell, MapTileSystem.Instance.IsWalkable);
        if (cellPath == null)
        {
            Debug.LogError($"[PathfindingSystem] ComputePath: {startCell} → {goalCell} 경로를 찾지 못함. 직선 폴백 사용.");
            return includeStart
                ? new[] { CellCenter(startCell), CellCenter(goalCell) }
                : new[] { CellCenter(goalCell) };
        }

        var smoothed = SmoothPath(cellPath);
        bool sameCell = startCell == goalCell;
        int fromIndex = (includeStart || sameCell) ? 0 : 1;

        var result = new Vector2[smoothed.Count - fromIndex];
        for (int i = 0; i < result.Length; i++)
            result[i] = CellCenter(smoothed[fromIndex + i]);
        return result;
    }

    /// <summary>
    /// 살아있는 모든 적의 경로를 현재 위치 기준으로 재계산한다. 타워 배치/이동/삭제가
    /// 커밋될 때(MapTileSystem.PlaceTower/RemoveTower)마다 호출된다.
    /// </summary>
    public void RecalculateActiveEnemyPaths()
    {
        var basePoint = GetBasePoint();
        foreach (var enemy in Enemy.ActiveEnemies)
        {
            var path = ComputePath(enemy.transform.position, basePoint, includeStart: false);
            enemy.SetPath(path);
        }
        OnPathsChanged?.Invoke();
    }

    /// <summary>
    /// fromWorld → toWorld 사이의 A* 경로를 셀 단위로(스무딩 없이) 그대로 반환한다. 이동에는
    /// ComputePath(스무딩된 코너 경로)를 쓰고, 이 메서드는 경로 시각화(MonsterPathVisualizer)처럼
    /// 지나가는 셀 하나하나가 필요한 경우에만 사용한다.
    /// </summary>
    public Vector2[] ComputeFullCellPath(Vector2 fromWorld, Vector2 toWorld)
    {
        if (MapTileSystem.Instance == null) return Array.Empty<Vector2>();

        var startCell = WorldToCell(fromWorld);
        var goalCell = WorldToCell(toWorld);

        var cellPath = AStarPathfinder.FindPath(startCell, goalCell, MapTileSystem.Instance.IsWalkable);
        if (cellPath == null)
            return new[] { CellCenter(startCell), CellCenter(goalCell) };

        var result = new Vector2[cellPath.Count];
        for (int i = 0; i < cellPath.Count; i++)
            result[i] = CellCenter(cellPath[i]);
        return result;
    }

    // 콜리니어(동일 방향 연속) 구간을 병합해 굴절점만 남긴다. 시작/끝 지점은 항상 보존.
    private static List<Vector2Int> SmoothPath(List<Vector2Int> path)
    {
        if (path.Count <= 2) return path;

        var result = new List<Vector2Int> { path[0] };
        Vector2Int prevDir = path[1] - path[0];

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int dir = path[i + 1] - path[i];
            if (dir != prevDir)
            {
                result.Add(path[i]);
                prevDir = dir;
            }
        }
        result.Add(path[^1]);
        return result;
    }

    private static Vector2 CellCenter(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.5f);

    private static Vector2Int WorldToCell(Vector2 point)
        => new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
}
