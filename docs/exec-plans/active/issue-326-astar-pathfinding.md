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
- **8방향 이동 + 코너 컷 금지**: 대각선 이동 허용(체감상 "직선"에 가까운 최단경로), 단 대각선의 **양쪽 직교 인접 셀 중 하나라도 막혀 있으면** 그 대각선 이동은 금지한다 (Codex #327 지적, 4차 리뷰 — 둘 다 막힌 경우에만 금지하는 "느슨한" 규칙은 여기서 틀리다. 몬스터는 그리드에 스냅되지 않고 `Vector2.MoveTowards`로 셀 중심-중심을 잇는 연속 이동을 하므로, 직교 셀 중 하나만 막혀 있어도(=타워 1개만 있어도) 대각선 이동 궤적이 그 타워의 모서리를 스치듯 통과해 사실상 타워를 무시하고 지나가게 된다. 타워가 실제 장애물이 되어야 한다는 이번 이슈의 목적과 충돌하므로 더 엄격한 규칙을 쓴다).
- **경로 스무딩**: A* 결과(셀 단위 촘촘한 경로)를 콜리니어(동일 방향 연속) 구간 병합으로 축소한 뒤 `Enemy` 에 넘긴다. `Enemy.MoveAlongPath` 는 `Vector2.MoveTowards` 기반 연속 이동이라 몇 개의 굴절점만 있으면 충분하고, 기존 손으로 배치한 waypoints와 동일한 소비 방식을 유지할 수 있다.

### 재계산 트리거

- 스폰 시점: `WaveSystem.SpawnEnemies` 가 각 몬스터 생성 직전 `PathfindingSystem.ComputePath(route.spawnPoint, basePoint)` 호출.
- 타워 배치/이동/제거가 **실제로 커밋되는 지점은 `TowerPlacer.TryPlace` 성공, `TryMove` 성공, `ExitPlacementMode`의 이동 취소 후 원위치 복귀뿐만이 아니다** — 삭제 확인 팝업(`TowerDeleteConfirmPopup` → `InventorySystem.DeleteTower` → `Destroy(tower.gameObject)` → `Tower.OnDestroy()` → `MapTileSystem.RemoveTower`)도 `TowerPlacer`를 거치지 않고 독립적으로 타워를 제거한다 (Codex #327 지적). 따라서 재계산 호출은 `TowerPlacer`가 아니라 **`MapTileSystem.PlaceTower` / `RemoveTower` 자체의 마지막 줄**에 넣어, 어느 경로로 호출되든(배치/이동/삭제) 한 곳에서 빠짐없이 `PathfindingSystem.RecalculateActiveEnemyPaths()` 가 실행되도록 한다.
  - `TryMove`는 `RemoveTower(origin)` → `PlaceTower(dest)` 두 번 호출하므로 재계산이 한 번의 이동에 2회 실행되지만, 재계산은 유저 조작 시점에만 발생하는 드문 이벤트라 비용 문제 없음(중복 실행 자체를 최적화하지 않음).
- `Enemy.ActiveEnemies` 전원의 **현재 위치 → basePoint** 경로를 재계산하고 `enemy.SetPath(...)` 로 교체. 드래그 중(고스트 미리보기, `Update()` 틱마다)에는 호출하지 않음 — `PlaceTower`/`RemoveTower`가 실제로 호출되는 커밋 순간에만 실행됨.
- 타워는 항상 0~1개이므로 재계산 빈도는 "유저가 배치/이동/삭제를 확정한 순간"뿐이며 프레임당 비용 문제 없음.

### 이동(Move) 검증 시 원위치 제외 (Codex #327 지적)

`TowerPlacer.TryMove`는 `MapTileSystem.Instance.CanPlaceTower(coord)` 를 **`RemoveTower(_moveOriginCoord)` 호출 이전에** 검사한다. 이 시점엔 원래 타워가 여전히 `_placedTowers`에 남아 있으므로, `CanPlaceTower` 가 내부적으로 `WouldSeverPath(coord)` 를 호출하면 "원위치 타워 + 이동 후보 좌표" **두 칸이 동시에 막힌 것**으로 연결성을 검사하게 된다. 최종 상태(원위치엔 타워 없음, 후보 좌표에만 1개)라면 통과했을 이동이, 두 칸을 동시에 막으면 통로가 끊기는 경우 부당하게 거부될 수 있다.

→ `WouldSeverPath` 와 `CanPlaceTower` 에 `ignoreCoord` (nullable `Vector2Int?`) 오버로드를 추가해 "이 좌표는 타워가 없다고 간주" 하도록 한다. `TowerPlacer`의 이동 관련 검사(미리보기 `Update()`의 고스트 색상 판정, `TryMove`의 실제 커밋 검사) 양쪽 모두 `CanPlaceTower(coord, ignoreCoord: _moveOriginCoord)` 를 사용한다. 신규 배치(`TryPlace`)는 `ignoreCoord` 없이 기존 `CanPlaceTower(coord)` 그대로.

**주의 (Codex #327 지적, 5차 리뷰)**: `ignoreCoord`는 `WouldSeverPath` 연결성 검사뿐 아니라 **점유 여부 검사(`_placedTowers.ContainsKey(coord)`)에도 동일하게 적용**해야 한다. 원위치(`_moveOriginCoord`)는 이 시점에 아직 `_placedTowers`에 남아 있으므로, 점유 검사에 `ignoreCoord`를 안 넘기면 "원래 자리로 다시 옮기기"(사실상 이동 취소) 클릭이 부당하게 거부되는 회귀가 생긴다. 기존 `TryMove`의 `coord == _moveOriginCoord ||` 특례 분기를 별도로 유지하는 대신, `CanPlaceTower` 내부에서 점유 검사 자체를 `!_placedTowers.ContainsKey(coord) || coord == ignoreCoord` 로 작성해 `ignoreCoord`가 점유·연결성 검사 모두에 일관되게 적용되도록 한다.

### 시작 셀은 항상 통과 가능 (Codex #327 지적, 2차 리뷰)

타워를 몬스터가 현재 서 있는 셀에 배치/이동할 수 있다는 전제(`CanPlaceTower`는 몬스터 점유 여부를 검사하지 않으며, 웨이브 진행 중 배치를 막지 않음 — 기존부터 허용되던 동작)를 유지하면, 재계산 시점에 그 몬스터의 "현재 위치 셀"이 방금 막 `IsWalkable == false` 가 되어버린 상태일 수 있다. 이 상태에서 그 셀을 시작점으로 `FindPath`를 돌리면 시작 노드 자체가 walkable 이 아니라서 경로 탐색이 실패(→ 직선 폴백)할 위험이 있다.

→ `AStarPathfinder.FindPath(start, goal, isWalkable)` 는 **`start` 노드 자체는 `isWalkable` 검사 대상에서 제외**하고 항상 진입 가능한 것으로 취급한다 (이웃 노드로 확장할 때만 `isWalkable` 검사). "이미 그 자리에 서 있다"는 사실 자체가 곧 통행 가능성의 증거이므로, 타워가 마침 그 자리에 놓였더라도 그 지점에서 빠져나가는 경로는 항상 계산 가능해야 한다. §3 `AStarPathfinder` 명세에 반영.

### 스폰 경로 vs 실시간 재계산 경로의 모양 구분 (Codex #327 지적, 2차 리뷰)

`ComputePath` 결과를 두 군데(`WaveSystem.SpawnEnemies` → `Enemy.Initialize`, `RecalculateActiveEnemyPaths` → `Enemy.SetPath`)에서 그대로 재사용하려 했으나, 두 소비처의 기대 형태가 다르다:
- `Enemy.Initialize`: `waypoints[0]`로 순간이동 후 인덱스 1부터 시작 — **시작점(spawnPoint) 포함**된 배열을 기대.
- `Enemy.SetPath`: 이미 그 자리에 있으므로 인덱스 0부터 시작 — **현재 위치 미포함**된 배열을 기대.

같은 `ComputePath(from, to)` 를 두 곳에 그대로 넘기면 스폰 시 첫 구간이 스킵되거나, 재계산 시 제자리를 향하는 불필요한 웨이포인트가 생긴다.

→ `PathfindingSystem.ComputePath(Vector2 fromWorld, Vector2 toWorld, bool includeStart)` 로 옵션을 명시한다:
- `WaveSystem.SpawnEnemies` → `ComputePath(spawnPoint, basePoint, includeStart: true)`
- `RecalculateActiveEnemyPaths` → `ComputePath(currentPos, basePoint, includeStart: false)`

내부적으로 A* 결과(셀 경로)의 첫 원소를 포함시킬지 여부만 다르고, 나머지(스무딩, 오프셋 변환) 로직은 공유한다.

### 시작 셀 == 목표 셀일 때 base 웨이포인트 보존 (Codex #327 지적, 3차 리뷰)

`RecalculateActiveEnemyPaths`가 이미 `basePoint`와 같은 셀 안에 들어와 있지만(`ReachBase` 판정 거리 0.05f보다는 아직 먼) 몬스터에 대해 실행되면, `AStarPathfinder.FindPath`는 시작=목표인 셀 1개짜리 경로를 반환한다. 여기에 `includeStart: false`를 그대로 적용해 그 유일한 원소(시작 셀)를 제거하면 **빈 배열**이 나온다. `Enemy.MoveAlongPath`는 `_waypoints.Length == 0`이면 `_waypointIndex(0) >= Length(0)`이 참이 되어 즉시 return — `ReachBase()`가 영원히 호출되지 않고 그 몬스터가 그 자리에 멈춰버린다(#253에서 겪은 것과 같은 유형의 "웨이브 stuck" 버그).

→ `ComputePath`는 **`start`와 `goal`이 같은 셀이면 `includeStart` 값과 무관하게 목표 좌표(`basePoint`) 1개짜리 배열을 반환**한다 (제거할 "시작점"과 "목표점"이 사실상 같은 지점이므로 완전히 비우지 않고 목표 웨이포인트는 항상 보존). 이렇게 하면 `SetPath` 이후에도 `MoveAlongPath`가 그 한 지점을 향해 이동을 계속하다 `ReachBase()`를 정상적으로 호출한다.

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
- 신규: `public bool WouldSeverPath(Vector2Int coord, Vector2Int? ignoreCoord = null)` — `coord`에 가상으로 타워가 있다고 가정하고(단 `ignoreCoord`는 타워가 없는 것으로 간주) 모든 `spawnRoutes[].spawnPoint` → `basePoint` 가 A*로 도달 가능한지 검사 (연결성만 필요하므로 BFS로 충분, `AStarPathfinder` 재사용 가능).
- `CanPlaceTower(Vector2Int coord, Vector2Int? ignoreCoord = null)` 오버로드 추가, 기존 무인자 호출부는 `ignoreCoord: null`로 위임. 점유 검사와 연결성 검사 **양쪽 모두**에 `ignoreCoord`를 적용: `GetTileType(coord) == Buildable && (!_placedTowers.ContainsKey(coord) || coord == ignoreCoord) && !WouldSeverPath(coord, ignoreCoord)` (Codex #327 지적, 5차 리뷰 — `ignoreCoord`를 연결성 검사에만 적용하면 원위치로 되돌리는 이동이 점유 검사에서 부당하게 거부됨, §1 "이동 검증 시 원위치 제외" 참고).
- `GetSpawnPoint(routeIndex)` 는 유지.
- `PlaceTower` / `RemoveTower` 마지막 줄에 `PathfindingSystem.Instance?.RecalculateActiveEnemyPaths()` 호출 추가 — 배치/이동/삭제 등 호출 경로에 관계없이 이 두 메서드만 거치면 항상 재계산되도록 함 (Codex #327 지적, §1 "재계산 트리거" 참고).

### `MakeDefence/Assets/Scripts/Systems/PathfindingSystem.cs`
- 실제 경로탐색 책임을 이관받는다 (이름값을 하게 됨):
  - `Vector2[] ComputePath(Vector2 fromWorld, Vector2 toWorld, bool includeStart)` — 월드 좌표 → 셀 변환 → `AStarPathfinder.FindPath(start, goal, MapTileSystem.Instance.IsWalkable)` → `includeStart`가 false면 셀 경로 첫 원소(시작 셀) 제거하되 **`start == goal`이면 제거하지 않고 목표 좌표 1개짜리 배열 그대로 반환** (Codex #327 지적, 3차 리뷰 — §1 "시작 셀 == 목표 셀일 때 base 웨이포인트 보존" 참고) → 월드 좌표(+0.5 오프셋)로 변환 → 스무딩 → 반환. 경로 없음(이론상 발생 안 하지만 방어) 시 `Debug.LogError` + 시작/끝 2점짜리 직선 폴백 (Codex #327 지적, 2차 리뷰 — §1 "스폰 경로 vs 실시간 재계산 경로의 모양 구분" 참고).
  - `void RecalculateActiveEnemyPaths()` — `Enemy.ActiveEnemies` 순회, 각 enemy 현재 위치 → `basePoint` 로 `ComputePath(currentPos, basePoint, includeStart: false)` 재호출, `enemy.SetPath(newPath)`.
- 기존 `GetFullPath`/`GetWaypoints` 프록시 메서드 삭제 (호출부 없음 확인 필요, §5 R4 참고).

### `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
- `SpawnEnemies` 176~190라인 부근: `MapTileSystem.Instance.GetFullPath(routeIndex)` 호출을 `PathfindingSystem.Instance.ComputePath(MapTileSystem.Instance.GetSpawnPoint(routeIndex), MapTileSystem.Instance.GetBasePoint(), includeStart: true)` 로 교체 (Codex #327 지적, 2차 리뷰).

### `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
- 신규 메서드 `public void SetPath(Vector2[] newPath)`:
  ```csharp
  public void SetPath(Vector2[] newPath)
  {
      _waypoints = newPath;
      _waypointIndex = 0; // 새 경로는 이미 "현재 위치"를 포함하지 않으므로 0부터 MoveTowards
  }
  ```
  (`Initialize`의 기존 `_waypointIndex = 1` 스킵 로직은 "0번째 waypoint = 스폰 좌표 = 현재 위치" 라서 존재하던 것. `SetPath`로 들어오는 경로는 `ComputePath(..., includeStart: false)` 로 조립되어 "현재 위치"를 포함하지 않고 바로 다음 목표부터 시작 — §1 "스폰 경로 vs 실시간 재계산 경로의 모양 구분" 참고.)
- `MoveAlongPath` / `ReachBase` 로직은 변경 없음 (여전히 `_waypoints[_waypointIndex]` 순회).

### `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`
- 재계산 호출은 §2 `MapTileSystem.PlaceTower`/`RemoveTower`에서 처리하므로 이 파일에는 별도 재계산 훅 불필요.
- 이동 모드 검증 두 곳을 `ignoreCoord: _moveOriginCoord` 를 넘기도록 수정 (Codex #327 지적):
  - `Update()` 34라인 고스트 색상 판정: `MapTileSystem.Instance.CanPlaceTower(coord, _moveOriginCoord)`
  - `TryMove` 120라인 커밋 검증: 기존 `coord == _moveOriginCoord || MapTileSystem.Instance.CanPlaceTower(coord)` 를 `MapTileSystem.Instance.CanPlaceTower(coord, _moveOriginCoord)` **로만** 교체한다. `CanPlaceTower`가 이제 `ignoreCoord`를 점유 검사에도 적용하므로(§2 `MapTileSystem.cs` 참고) 원위치 클릭이 자동으로 허용되며, 별도로 `coord == _moveOriginCoord` 특례 분기를 남겨둘 필요가 없다 (Codex #327 지적, 5차 리뷰 — 특례 분기를 지우면서 점유 검사 쪽에 `ignoreCoord`를 안 넣으면 원위치 복귀가 막히는 회귀가 생기므로 반드시 §2 수정과 함께 적용).
  - `TryPlace`(신규 배치, 107라인)는 그대로 `CanPlaceTower(coord)` (ignoreCoord 없음)

### `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
- 변경 없음 — `OnDestroy()`가 호출하는 `MapTileSystem.RemoveTower(TileCoord)`가 위 §2 변경으로 자동 재계산을 트리거하게 됨. 이 경로(삭제 확인 팝업 → `InventorySystem.DeleteTower` → `Destroy` → `OnDestroy`)가 `TowerPlacer`를 거치지 않아 기존 초안에서 누락됐던 지점 (Codex #327 지적).

### `MakeDefence/Assets/Scenes/SampleScene.unity`
- `MapTileSystem` 컴포넌트의 `spawnRoutes[].waypoints` 값 폐기됨 (필드 제거) — `spawnPoint`/`basePoint` 는 그대로 유지되므로 데이터 손실 최소.
- ⚠️ `.unity` 직접 YAML 편집 금지 — UnityMCP `manage_components` 로 수정 ([feedback_unity_asset_edits](../../../../.claude/projects/C--Users-kalon-Documents-GitHub-Defencegame/memory/feedback_unity_asset_edits.md))

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/Systems/AStarPathfinder.cs`
- MonoBehaviour 아닌 순수 static 유틸리티 클래스.
- `public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)` — 8방향 A*, 경로 없으면 `null`. **`start` 노드 자체는 `isWalkable` 검사에서 제외**하고 항상 진입 가능으로 취급(이웃으로 확장할 때만 검사) — 재계산 시점에 시작 셀에 막 타워가 놓인 경우를 대비 (Codex #327 지적, 2차 리뷰, §1 "시작 셀은 항상 통과 가능" 참고). **코너컷 규칙: 대각선 이동 시 인접한 두 직교 셀 중 하나라도 막혀 있으면 금지** (둘 다 막힌 경우에만 금지하는 느슨한 규칙 아님 — Codex #327 지적, 4차 리뷰, §1 "8방향 이동 + 코너 컷 금지" 참고).
- `public static bool IsReachable(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)` — BFS 기반 연결성만 검사(봉쇄 방지용, `WouldSeverPath`에서 사용). `FindPath`가 null을 반환하는지로 대체 가능하지만 의미를 명확히 하기 위해 별도 메서드로 분리.
- 순수 C# (Unity API 의존 최소) → EditMode 단위 테스트 용이.

## 4. 테스트 계획

### EditMode 단위 테스트 (신규)
- [ ] `AStarPathfinder.FindPath`: 빈 그리드에서 직선/대각선 경로가 최단인지 (노드 수, 총 이동 거리)
- [ ] `AStarPathfinder.FindPath`: 중앙에 장애물 1칸 → 경로가 우회하는지, 우회 거리가 최소인지
- [ ] `AStarPathfinder.FindPath`: 코너컷 금지 확인 — 대각선 양쪽 직교 셀 중 **하나만** 막혀 있어도 그 대각선 이동이 금지되는지 (둘 다 막힌 경우만 금지하는 느슨한 구현이 되지 않았는지 명시적으로 검증, Codex #327 4차 리뷰)
- [ ] `AStarPathfinder.FindPath`: 완전히 막힌 경우 `null` 반환
- [ ] `AStarPathfinder.FindPath`: `start` 좌표 자체가 `isWalkable(start) == false` 인 경우에도 경로를 정상적으로 찾는지 (시작 셀 예외 규칙 검증)
- [ ] `AStarPathfinder.IsReachable`: 봉쇄/비봉쇄 케이스
- [ ] `MapTileSystem.WouldSeverPath`: 유일한 통로 타일에 타워를 놓으려 하면 true(=배치 거부되어야 함)
- [ ] `PathfindingSystem.ComputePath`: `includeStart: true`/`false` 각각 결과 배열에 시작 좌표 포함 여부가 정확한지
- [ ] `PathfindingSystem.ComputePath`: `fromWorld`와 `toWorld`가 같은 셀일 때 `includeStart: false`여도 빈 배열이 아니라 목표 좌표 1개짜리 배열을 반환하는지

### 수동 (Unity Editor)
전제: SampleScene, `spawnRoutes` 2개 이상, `basePoint` 설정.
- [ ] 타워 없이 웨이브 시작 → 각 스폰 지점에서 basePoint까지 대각선 포함 최단경로로 이동 (기존처럼 지정된 좁은 통로만 도는 게 아니라 맵을 가로지르는지 육안 확인)
- [ ] 경로 중간에 타워 배치 → 신규 스폰 몬스터가 타워를 피해 우회
- [ ] 웨이브 진행 중 타워를 배치/이동 → 이미 이동 중이던 몬스터들이 순간이동 없이 자연스럽게 새 경로로 전환
- [ ] 유일한 통로를 막는 위치에 타워 배치 시도 → 배치 거부(고스트 빨간색 유지, 배치 안 됨)
- [ ] 타워 삭제/이동으로 통로가 다시 열리면 이후 배치 가능
- [ ] 다중 스폰 경로 모두 정상적으로 basePoint 합류 (#253 회귀 확인)
- [ ] 차원석 리프트 웨이브도 동일하게 동작
- [ ] 몬스터가 밟고 지나가는 셀에 정확히 타워를 배치/이동 → 그 몬스터가 즉시 우회 경로로 전환하는지 (직선 폴백으로 깨지지 않는지)
- [ ] 신규 스폰 몬스터가 스폰 지점부터 정상적으로 첫 구간을 이동하는지(스킵 없음), 재계산된 몬스터가 제자리 방향으로 튀지 않는지 (includeStart 분기 검증)
- [ ] 좁은 길목 모서리에 타워 1개를 놓았을 때, 몬스터가 대각선으로 그 모서리를 스치듯 지나가지 않고 실제로 우회하는지 육안 확인 (코너컷 강화 규칙 검증)
- [ ] 타워 이동 모드 진입 후 드래그하다가 원래 자리(`_moveOriginCoord`)를 다시 클릭하면 정상적으로 커밋(no-op 이동)되는지 — 고스트가 계속 빨간색으로 남아 거부되지 않는지

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

### R7. 타워 이동 검증 시 원위치 이중 차단 (Codex #327 지적)
`TowerPlacer.TryMove`는 `RemoveTower(_moveOriginCoord)` 호출 **이전에** `CanPlaceTower(coord)`를 검사한다. `WouldSeverPath`가 원위치 타워를 여전히 장애물로 간주한 채 연결성을 검사하면, 원위치+이동후보 두 칸이 동시에 막혔을 때만 끊기는 통로를 "이동 불가"로 오판할 수 있다. → `CanPlaceTower`/`WouldSeverPath`에 `ignoreCoord` 파라미터를 추가해 이동 관련 검증(고스트 미리보기, `TryMove` 커밋)엔 항상 `_moveOriginCoord`를 제외하고 검사한다 (§1, §2 참고).

### R8. 타워 삭제 경로가 `TowerPlacer`를 거치지 않음 (Codex #327 지적)
삭제 확인 팝업(`TowerDeleteConfirmPopup`) → `InventorySystem.DeleteTower` → `Destroy(tower.gameObject)` → `Tower.OnDestroy()` → `MapTileSystem.RemoveTower` 경로는 `TowerPlacer`의 배치/이동 성공 콜백을 전혀 거치지 않는다. 재계산 훅을 `TowerPlacer`에만 넣으면 삭제 시 살아있는 몬스터가 옛 회피 경로를 계속 따라간다. → 재계산 호출을 `TowerPlacer`가 아니라 `MapTileSystem.PlaceTower`/`RemoveTower` 내부로 옮겨 모든 호출 경로(배치/이동/삭제)에서 자동 실행되도록 한다 (§1, §2 참고).

### R9. 몬스터가 서 있는 셀에 타워가 놓이는 경우 (Codex #327 지적, 2차 리뷰)
`CanPlaceTower`는 몬스터 점유 여부를 검사하지 않고, 웨이브 진행 중 배치 자체는 기존부터 허용되던 동작이라 막지 않는다. 따라서 몬스터 바로 밑에 타워가 놓이면 그 몬스터의 재계산 시작 셀이 순간적으로 `IsWalkable == false` 가 된다. → `AStarPathfinder.FindPath`가 시작 노드를 `isWalkable` 검사에서 제외하도록 해서 항상 탈출 경로를 계산할 수 있게 한다 (§1, §3 참고). 이 규칙이 없으면 해당 몬스터만 직선 폴백(타워 관통)으로 빠지는 시각적 버그가 남는다.

### R10. `ComputePath` 재사용 시 스폰/재계산 경로 모양 불일치 (Codex #327 지적, 2차 리뷰)
같은 `ComputePath(from, to)` 를 스폰(`Enemy.Initialize`, 시작점 포함 필요)과 실시간 재계산(`Enemy.SetPath`, 시작점 미포함 필요) 양쪽에 그대로 재사용하면 스폰 시 첫 구간이 스킵되거나 재계산 시 제자리 웨이포인트가 남는다. → `includeStart` 파라미터로 호출부별 기대 형태를 명시 (§1, §2 참고).

### R11. 시작=목표 셀일 때 재계산 결과가 빈 배열이 되는 문제 (Codex #327 지적, 3차 리뷰)
몬스터가 이미 `basePoint`와 같은 셀 안에 있지만 `ReachBase` 판정 거리(0.05f)보다는 아직 먼 상태에서 재계산이 발생하면, A*는 셀 1개짜리 경로를 반환한다. `includeStart: false`가 이 유일한 원소까지 제거해버리면 `Enemy.MoveAlongPath`가 빈 배열을 받아 즉시 return하고 `ReachBase()`가 영원히 안 불려 웨이브가 stuck된다 (#253에서 겪은 것과 동일 유형). → `ComputePath`는 `start == goal`이면 `includeStart`와 무관하게 목표 좌표 1개짜리 배열을 반환해야 한다 (§1, §2 참고). 이 케이스를 놓치면 이번 이슈는 완료로 볼 수 없음(선택 사항 아님, R1과 동급의 필수 안전장치).

### R12. 느슨한 코너컷 규칙으로 인한 타워 무력화 (Codex #327 지적, 4차 리뷰)
"대각선 양쪽 직교 셀이 **모두** 막혔을 때만 금지"하는 일반적인 그리드 A* 코너컷 규칙은, 몬스터가 그리드에 스냅되지 않고 셀 중심 사이를 `Vector2.MoveTowards`로 연속 이동하는 이 게임에는 맞지 않는다. 직교 셀 중 하나만 막혀 있어도(=타워 1개) 대각선 이동 궤적이 그 타워의 모서리를 스치며 지나가 사실상 타워를 무력화시킨다. → 코너컷 규칙을 "**둘 중 하나라도** 막혀 있으면 대각선 금지"로 강화한다 (§1, §3 참고). 이 규칙이 없으면 좁은 길목 모서리에 놓인 타워가 몬스터를 전혀 막지 못하는 핵심 기능 결함으로 남는다(선택 사항 아님).

### R13. `ignoreCoord`를 점유 검사에 빠뜨리면 타워 원위치 복귀가 막힘 (Codex #327 지적, 5차 리뷰)
`TryMove`의 기존 코드는 `coord == _moveOriginCoord || CanPlaceTower(coord)` 로, 원위치 클릭 시 무조건 유효 처리하는 특례 분기가 있다. 이 분기를 지우고 `CanPlaceTower(coord, _moveOriginCoord)` 하나로 교체하려면, `ignoreCoord`가 `WouldSeverPath` 연결성 검사뿐 아니라 `_placedTowers.ContainsKey(coord)` 점유 검사에도 적용돼야 한다. 점유 검사 쪽을 빠뜨리면 원위치 좌표는 이 시점에 여전히 `_placedTowers`에 등록돼 있으므로 "원래 자리로 되돌리기"(사실상 이동 취소) 클릭이 부당하게 거부되는 회귀가 생긴다. → `CanPlaceTower`의 점유 검사를 `!_placedTowers.ContainsKey(coord) || coord == ignoreCoord` 로 작성해 두 검사 모두 `ignoreCoord`를 일관되게 반영한다 (§1, §2 참고).

## 6. 오픈 이슈 (Plan PR 에서 확정)

- **Q1**: `Decoration` 타일을 완전히 이동 불가로 볼지, 아니면 "타일이 아예 없는 셀"만 이동 불가로 보고 `Decoration`은 통과 가능하게 할지? (초안: `Decoration` = 이동 불가 — 배경/장식으로 간주)
- **Q2**: 대각선 이동 허용 여부 (8방향) vs 4방향만 허용. (초안: 8방향 — "최단 거리" 체감에 더 부합)
- **Q3**: 봉쇄 방지 검사(`WouldSeverPath`)를 이번 이슈 범위에 포함할지, 별도 이슈로 분리할지. (초안: **포함** — 타워가 실제 장애물이 되는 순간 발생하는 필수 안전장치라 분리 시 게임이 깨질 수 있음)
- **Q4**: 경로 재계산 시 살아있는 몬스터가 "지나온 길"로 되돌아가는 경로가 나올 수 있는가 (예: 타워가 몬스터 바로 앞에 생기면). (초안: 허용 — 현재 위치 기준 A*라 항상 최단이며 부자연스러운 역주행은 발생하지 않음, 실기로 확인)
