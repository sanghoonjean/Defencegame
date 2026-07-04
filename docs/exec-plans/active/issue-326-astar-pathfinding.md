# Issue #326 — 몬스터 이동을 웨이포인트 경로 추종에서 A* 최단경로 이동으로 변경

## 1. 시스템 구조

### 현재 흐름

```
MapTileSystem                          (Inspector)
 ├─ spawnRoutes[] : SpawnRoute[]       ← {spawnPoint, waypoints[]} 사람이 직접 배치
 └─ basePoint     : Vector2            ← 공통 종착점
        │
        ▼ GetFullPath(routeIndex) → Vector2[] (spawn + 고정 waypoints + base, 그대로 조립)
        │
WaveSystem.SpawnEnemies()
 └─ enemy.Initialize(data, stage, waypoints[], mods)
        │
        ▼
Enemy.MoveAlongPath()                  ← 고정 배열을 순서대로 MoveTowards
```

`MapTileSystem` 은 이미 `Tilemap` 2장(`buildableTilemap`, `pathTilemap`)과 `TileType`(Path/Buildable/Decoration), `Vector2Int` 좌표계, 타워 배치 딕셔너리(`_placedTowers`)를 갖고 있다. 단, 이 그리드는 현재 **타워 배치 가능 여부 검증에만** 쓰이고 몬스터 이동과는 완전히 분리돼 있다.

중요 전제: `TowerPlacer.EnterPlacementMode()` / `GetPlacedTower()` 로 볼 때 **이 게임은 한 번에 타워가 최대 1개**만 존재한다 (배치 후 다시 배치 시도하면 "이동 모드"로 전환). 따라서 이동을 막는 장애물은 항상 0개 또는 1개 타일이며, 고전 멀티타워 미로형 TD의 "복잡한 미로 재계산" 문제는 없다.

### 변경 후 흐름

```
MapTileSystem                          (Inspector)
 ├─ spawnRoutes[] : SpawnRoute[]       ← {spawnPoint} 만 남김 (중간 waypoints 제거, A*가 대체)
 ├─ basePoint     : Vector2
 └─ IsWalkable(Vector2Int cell)        ★ 신규 — Path/Buildable 타일 && 타워가 없는 셀 = true
        │
        ▼ (스폰 시 / 타워 배치·이동·제거 시)
PathfindingSystem.ComputePath(fromWorld, toWorld) → Vector2[]   ★ 신규 — AStarPathfinder 호출 + 경로 스무딩
        │
        ▼
WaveSystem.SpawnEnemies()  → enemy.Initialize(data, stage, path, mods)
TowerPlacer (배치/이동/제거 커밋 시) → PathfindingSystem.RecalculateActiveEnemyPaths() → 살아있는 모든 Enemy.SetPath(newPath)
        │
        ▼
Enemy.MoveAlongPath()                  ← 로직 동일 (Vector2[] 순서 소비), SetPath로 배열 교체만 추가
```

### 그리드 정의 (핵심 결정)

- **Walkable = `GetTileType(cell)` 가 `Path` 또는 `Buildable`, AND 해당 셀에 배치된 타워가 없음.** `Decoration` 타일/타일맵 범위 밖은 항상 이동 불가.
  - 근거: 유저 요청이 "생성된 곳에서 목표 지점까지 최단 거리"이므로 더 이상 `Path` 타일에만 국한되지 않고 맵 전체(Buildable 포함)를 활보하다가 유일한 장애물인 타워만 피해야 자연스럽다.
- **8방향 이동 + 코너 컷 금지**: 대각선 이동 허용(체감상 "직선"에 가까운 최단경로), 단 대각선의 양쪽 직교 인접 셀이 모두 막혀 있으면 그 대각선 이동은 금지(장애물이 1개뿐이라도 일반적인 A* 코너-컷 방지 규칙을 그대로 적용해 코드 재사용성 유지).
- **경로 스무딩**: A* 결과(셀 단위 촘촘한 경로)를 콜리니어(동일 방향 연속) 구간 병합으로 축소한 뒤 `Enemy` 에 넘긴다. `Enemy.MoveAlongPath` 는 `Vector2.MoveTowards` 기반 연속 이동이라 몇 개의 굴절점만 있으면 충분하고, 기존 손으로 배치한 waypoints와 동일한 소비 방식을 유지할 수 있다.

### 재계산 트리거

- 스폰 시점: `WaveSystem.SpawnEnemies` 가 각 몬스터 생성 직전 `PathfindingSystem.ComputePath(route.spawnPoint, basePoint)` 호출.
- 타워 배치/이동/제거 커밋 시점(`TowerPlacer.TryPlace` 성공, `TryMove` 성공, `ExitPlacementMode`의 이동 취소 후 원위치 복귀 포함): `PathfindingSystem.RecalculateActiveEnemyPaths()` 를 호출해 `Enemy.ActiveEnemies` 전원의 **현재 위치 → basePoint** 경로를 재계산하고 `enemy.SetPath(...)` 로 교체. 드래그 중(고스트 미리보기, `Update()` 틱마다)에는 호출하지 않음 — 실제 배치/이동이 커밋된 순간에만 1회.
- 타워는 항상 0~1개이므로 재계산 빈도는 "유저가 배치 버튼을 누른 순간"뿐이며 프레임당 비용 문제 없음.

### 봉쇄 방지 (신규 요구사항)

기존 `CanPlaceTower` 는 좌표가 `Buildable` 이고 비어 있는지만 검사하며 **연결성 검사가 없다**. 지금까지는 타워가 이동을 막지 않았으므로 문제 없었지만, 이제 타워가 실제 장애물이 되므로 유저가 유일한 통로를 막아 게임을 클리어 불가능하게 만들 수 있는 신규 리스크가 생긴다. → `CanPlaceTower(coord)` 에 "이 좌표에 타워를 놓아도 모든 `spawnRoutes[].spawnPoint` → `basePoint` 경로가 여전히 존재하는가" 검사를 추가하고, 실패 시 배치를 거부한다(고스트 프리뷰는 `GhostInvalid` 로 표시).

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Systems/MapTileSystem.cs`
- `SpawnRoute` 에서 `waypoints` 필드 제거 (더 이상 사용 안 함, A*가 중간 경로를 계산):
  ```csharp
  [Serializable]
  public struct SpawnRoute
  {
      public Vector2 spawnPoint;
  }
  ```
- `GetWaypoints()` / `GetFullPath()` (배열 조립 버전) 삭제 — 대신 `PathfindingSystem.ComputePath` 가 그 역할을 대체.
- 신규: `public bool IsWalkable(Vector2Int cell)` — `GetTileType(cell)`이 `Buildable` 또는 `Path` 이고 `!_placedTowers.ContainsKey(cell)`.
- 신규: `public bool WouldSeverPath(Vector2Int coord)` — `coord`에 가상으로 타워가 있다고 가정하고 모든 `spawnRoutes[].spawnPoint` → `basePoint` 가 A*로 도달 가능한지 검사 (연결성만 필요하므로 BFS로 충분, `AStarPathfinder` 재사용 가능).
- `CanPlaceTower(coord)` 에 `&& !WouldSeverPath(coord)` 조건 추가.
- `GetSpawnPoint(routeIndex)` 는 유지.

### `MakeDefence/Assets/Scripts/Systems/PathfindingSystem.cs`
- 실제 경로탐색 책임을 이관받는다 (이름값을 하게 됨):
  - `Vector2[] ComputePath(Vector2 fromWorld, Vector2 toWorld)` — 월드 좌표 → 셀 변환 → `AStarPathfinder.FindPath(start, goal, MapTileSystem.Instance.IsWalkable)` → 셀 경로를 월드 좌표(+0.5 오프셋)로 변환 → 스무딩 → 반환. 경로 없음(이론상 발생 안 하지만 방어) 시 `Debug.LogError` + 시작/끝 2점짜리 직선 폴백.
  - `void RecalculateActiveEnemyPaths()` — `Enemy.ActiveEnemies` 순회, 각 enemy 현재 위치 → `basePoint` 로 `ComputePath` 재호출, `enemy.SetPath(newPath)`.
- 기존 `GetFullPath`/`GetWaypoints` 프록시 메서드 삭제 (호출부 없음 확인 필요, §5 R4 참고).

### `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
- `SpawnEnemies` 176~190라인 부근: `MapTileSystem.Instance.GetFullPath(routeIndex)` 호출을 `PathfindingSystem.Instance.ComputePath(MapTileSystem.Instance.GetSpawnPoint(routeIndex), MapTileSystem.Instance.GetBasePoint())` 로 교체.

### `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
- 신규 메서드 `public void SetPath(Vector2[] newPath)`:
  ```csharp
  public void SetPath(Vector2[] newPath)
  {
      _waypoints = newPath;
      _waypointIndex = 0; // 새 경로는 이미 "현재 위치"를 포함하지 않으므로 0부터 MoveTowards
  }
  ```
  (`Initialize`의 기존 `_waypointIndex = 1` 스킵 로직은 "0번째 waypoint = 스폰 좌표 = 현재 위치" 라서 존재하던 것. `SetPath`로 들어오는 경로는 "현재 위치"를 0번째로 포함시키지 않고 바로 다음 목표부터 주도록 `PathfindingSystem.ComputePath`가 조립 — 자세한 조립 규칙은 구현 단계에서 결정.)
- `MoveAlongPath` / `ReachBase` 로직은 변경 없음 (여전히 `_waypoints[_waypointIndex]` 순회).

### `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`
- `TryPlace` 성공 직후, `TryMove` 성공 직후, `ExitPlacementMode`의 이동-취소 후 원위치 복귀(`_movingTower.MoveTo(_moveOriginCoord)`) 직후 — 각 지점에서 `PathfindingSystem.Instance.RecalculateActiveEnemyPaths()` 호출.

### `MakeDefence/Assets/Scenes/SampleScene.unity`
- `MapTileSystem` 컴포넌트의 `spawnRoutes[].waypoints` 값 폐기됨 (필드 제거) — `spawnPoint`/`basePoint` 는 그대로 유지되므로 데이터 손실 최소.
- ⚠️ `.unity` 직접 YAML 편집 금지 — UnityMCP `manage_components` 로 수정 ([feedback_unity_asset_edits](../../../../.claude/projects/C--Users-kalon-Documents-GitHub-Defencegame/memory/feedback_unity_asset_edits.md))

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/Systems/AStarPathfinder.cs`
- MonoBehaviour 아닌 순수 static 유틸리티 클래스.
- `public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)` — 8방향 A*, 코너컷 금지, 경로 없으면 `null`.
- `public static bool IsReachable(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)` — BFS 기반 연결성만 검사(봉쇄 방지용, `WouldSeverPath`에서 사용). `FindPath`가 null을 반환하는지로 대체 가능하지만 의미를 명확히 하기 위해 별도 메서드로 분리.
- 순수 C# (Unity API 의존 최소) → EditMode 단위 테스트 용이.

## 4. 테스트 계획

### EditMode 단위 테스트 (신규)
- [ ] `AStarPathfinder.FindPath`: 빈 그리드에서 직선/대각선 경로가 최단인지 (노드 수, 총 이동 거리)
- [ ] `AStarPathfinder.FindPath`: 중앙에 장애물 1칸 → 경로가 우회하는지, 우회 거리가 최소인지
- [ ] `AStarPathfinder.FindPath`: 코너컷 금지 확인 (대각선 양쪽이 막힌 코너를 관통하지 않는지)
- [ ] `AStarPathfinder.FindPath`: 완전히 막힌 경우 `null` 반환
- [ ] `AStarPathfinder.IsReachable`: 봉쇄/비봉쇄 케이스
- [ ] `MapTileSystem.WouldSeverPath`: 유일한 통로 타일에 타워를 놓으려 하면 true(=배치 거부되어야 함)

### 수동 (Unity Editor)
전제: SampleScene, `spawnRoutes` 2개 이상, `basePoint` 설정.
- [ ] 타워 없이 웨이브 시작 → 각 스폰 지점에서 basePoint까지 대각선 포함 최단경로로 이동 (기존처럼 지정된 좁은 통로만 도는 게 아니라 맵을 가로지르는지 육안 확인)
- [ ] 경로 중간에 타워 배치 → 신규 스폰 몬스터가 타워를 피해 우회
- [ ] 웨이브 진행 중 타워를 배치/이동 → 이미 이동 중이던 몬스터들이 순간이동 없이 자연스럽게 새 경로로 전환
- [ ] 유일한 통로를 막는 위치에 타워 배치 시도 → 배치 거부(고스트 빨간색 유지, 배치 안 됨)
- [ ] 타워 삭제/이동으로 통로가 다시 열리면 이후 배치 가능
- [ ] 다중 스폰 경로 모두 정상적으로 basePoint 합류 (#253 회귀 확인)
- [ ] 차원석 리프트 웨이브도 동일하게 동작

## 5. 위험 요소

### R1. 봉쇄 가능성 (신규 리스크)
타워가 실제 장애물이 되므로 `WouldSeverPath` 검사를 빠뜨리면 유저가 게임을 클리어 불가능한 상태로 만들 수 있음. §1 "봉쇄 방지" 및 `CanPlaceTower` 수정으로 대응. 이 검사가 없으면 이번 이슈는 완료로 볼 수 없음(선택 사항 아님).

### R2. 기존 `spawnRoutes[].waypoints` 데이터 폐기
필드 제거 시 SampleScene에 손으로 배치했던 중간 경유점 데이터가 사라짐. 다만 `spawnPoint`/`basePoint`는 유지되고 A*가 중간을 대체하므로 기능적 손실은 없음 — 단, 커밋 전 SampleScene을 실기 로드해 각 route의 `spawnPoint`가 필드 제거 후에도 유지되는지 확인 필요 (이슈 #253 R1과 동일 패턴).

### R3. 이동 중 경로 교체 시 시각적 튐
`SetPath`가 `_waypointIndex = 0`으로 리셋할 때, 새 경로의 첫 좌표가 몬스터의 "현재 위치"와 정확히 일치하지 않으면 순간적으로 살짝 당겨지는 느낌이 날 수 있음. `ComputePath`가 "현재 위치의 셀"부터 경로를 잡되 반환 배열 첫 원소를 현재 월드 좌표 그대로 넣거나, `MoveAlongPath`가 첫 프레임에 아주 짧은 거리만 이동하도록 하면 체감상 무시 가능한 수준 — 구현 단계에서 실기 확인.

### R4. `PathfindingSystem`/`MapTileSystem`의 구 API 삭제 영향
`GetFullPath()`/`GetWaypoints()` 삭제 전에 다른 호출부(스폰 지점 아이콘 표시, 디버그 툴, 에디터 확장 등)가 없는지 전수 검색 필요. 있다면 해당 호출부도 `ComputePath` 기반으로 같이 수정.

### R5. 8방향 대각선 이동과 스프라이트 방향 전환
`Enemy.MoveAlongPath`의 `_spriteRenderer.flipX = dx < 0f` 로직은 좌우 반전만 처리 — 대각선 이동이 늘어나도 로직 변경 불필요(기존 그대로 좌우만 보고 판단). 다만 대각선 구간이 많아지면 몬스터가 "빗금"으로 걷는 것처럼 보일 수 있어 시각적으로 어색하지 않은지 실기 확인.

### R6. 코너컷 금지 규칙과 "장애물 1개" 전제
장애물이 항상 최대 1개라는 전제가 깨지는 미래 변경(예: 다중 타워 지원)이 생기면 코너컷 로직의 중요성이 커짐 — 지금은 낮은 리스크지만 `AStarPathfinder`를 일반적인 그리드 A*로 작성해두면 향후 확장에도 그대로 재사용 가능.

## 6. 오픈 이슈 (Plan PR 에서 확정)

- **Q1**: `Decoration` 타일을 완전히 이동 불가로 볼지, 아니면 "타일이 아예 없는 셀"만 이동 불가로 보고 `Decoration`은 통과 가능하게 할지? (초안: `Decoration` = 이동 불가 — 배경/장식으로 간주)
- **Q2**: 대각선 이동 허용 여부 (8방향) vs 4방향만 허용. (초안: 8방향 — "최단 거리" 체감에 더 부합)
- **Q3**: 봉쇄 방지 검사(`WouldSeverPath`)를 이번 이슈 범위에 포함할지, 별도 이슈로 분리할지. (초안: **포함** — 타워가 실제 장애물이 되는 순간 발생하는 필수 안전장치라 분리 시 게임이 깨질 수 있음)
- **Q4**: 경로 재계산 시 살아있는 몬스터가 "지나온 길"로 되돌아가는 경로가 나올 수 있는가 (예: 타워가 몬스터 바로 앞에 생기면). (초안: 허용 — 현재 위치 기준 A*라 항상 최단이며 부자연스러운 역주행은 발생하지 않음, 실기로 확인)
