# 기능 스펙: MapSystem

## 개요
60x33 타일 기반의 단일 고정 맵을 관리한다.
타일 종류(경로/배치가능/장식)를 구분하고,
A* 기반 최단경로를 PathfindingSystem이 실시간 계산해 제공한다 (#326).

---

## 기능 요구사항

### 1. 맵 기본 정보
- 크기: 60x33 타일 (1920x1080, PPU 32)
- 카메라 고정 (스크롤 없음)
- 단일 고정 맵 (런타임 생성 없음)

### 2. 맵 구조
3x3 구역 그리드. 구역 사이 통로가 적의 이동 경로.

```
[스폰]   [본진]
  │    ←←←←←←←←←←←←←←│
  ↓  ┌──────┬──────┬──────┤
     │  구역 │  구역 │  구역 │
  ↓  ├──────┼──────┼──────┤  ↑
     │  구역 │  구역 │  구역 │
  ↓  ├──────┼──────┼──────┤  ↑
     │  구역 │  구역 │  구역 │
  └→→→→→→→→→→→→→→→→→→→→┘
```

### 3. 타일 종류

| 타일 타입 | 설명 | 타워 배치 |
|-----------|------|-----------|
| `Path` | 구역 사이 통로, 적 이동 경로 | 불가 |
| `Buildable` | 9개 구역 내부 | 가능 |
| `Decoration` | 배경/구조물 시각 요소 | 불가 |

### 4. 주요 지점

| 지점 | 위치 |
|------|------|
| 스폰 포인트 | **여러 개** (`SpawnRoute[]`) — 각 route 마다 별도 스폰 좌표 |
| 본진 (기지) | 중상단 — 모든 route 가 공유하는 공통 종착점 |

### 5. A* 최단경로 (#326)
각 route 는 자기 스폰 지점만 갖고, `PathfindingSystem`이 스폰 지점 → 공통 `basePoint` 사이를 8방향 A*로 실시간 계산한다(타워를 장애물로 회피, 코너컷 금지). `WaveSystem.SpawnEnemies` 는 스폰 순번마다 다음 route 를 라운드로빈으로 사용한다 (총 스폰 수/간격은 route 개수와 무관). 타워가 배치·이동·삭제될 때마다 살아있는 적의 경로도 현재 위치 기준으로 재계산된다.

```
스폰 A ──▶ A* 최단경로 (타워 회피) ──┐
스폰 B ──▶ A* 최단경로 (타워 회피) ──┼──▶ 본진 (basePoint)
스폰 N ──▶ A* 최단경로 (타워 회피) ──┘
```

### 6. 타워 배치 검증
- 타워 배치 시 해당 타일이 `Buildable` 인지 확인
- 이미 타워가 있는 타일에는 중복 배치 불가
- 타워 배치가 모든 route의 스폰 지점 → 본진 연결성을 끊지 않는지 검사 (유일한 통로를 막는 배치는 거부) (#326)
- 배치 성공/실패 결과 반환

### 7. 경로 시각화 (#331)
`MonsterPathVisualizer`가 모든 route의 스폰 지점 → 본진 경로를 **스무딩 이전 셀 단위 전체 경로**(`PathfindingSystem.ComputeFullCellPath`)로 조회해, 각 셀에 작은 원 오브젝트를 상시 표시한다. 타워가 배치·이동·삭제될 때(`PathfindingSystem.OnPathsChanged`)마다 마커도 함께 갱신된다.

---

## 인터페이스

```csharp
public class MapTileSystem
{
    public TileType GetTileType(Vector2Int coord);
    public bool PlaceTower(Vector2Int coord, Tower tower);
    public void RemoveTower(Vector2Int coord);

    // 다중 route 지원
    public int RouteCount { get; }
    public Vector2 GetSpawnPoint();                     // route 0 (하위 호환)
    public Vector2 GetSpawnPoint(int routeIndex);
    public Vector2 GetBasePoint();                      // 공통 종착점

    // A* 경로탐색 (#326)
    public bool IsWalkable(Vector2Int cell);
    public bool WouldSeverPath(Vector2Int coord, Vector2Int? ignoreCoord = null);
    public bool CanPlaceTower(Vector2Int coord, Vector2Int? ignoreCoord = null);
}

public class PathfindingSystem
{
    // 경로 시각화 (#331) — 스무딩 없는 셀 단위 전체 경로
    public Vector2[] ComputeFullCellPath(Vector2 fromWorld, Vector2 toWorld);
    public static event Action OnPathsChanged;
}

public enum TileType
{
    Path, Buildable, Decoration
}
```

---

## 검증 조건
- [ ] 60x33 타일 맵 정상 로드
- [ ] 구역 내부 타일 Buildable 확인
- [ ] 경로 타일 배치 불가 확인
- [ ] A* 최단경로가 타워를 우회해 정확히 반환 (#326)
- [ ] 스폰/기지 위치 정확히 반환
- [ ] 중복 배치 방지
