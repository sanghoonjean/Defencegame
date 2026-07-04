# Issue #331 — 몬스터 이동경로 시각화 (셀마다 원 마커 표시)

## 1. 시스템 구조

### 현재 흐름 (#326 이후)

```
MapTileSystem
 ├─ spawnRoutes[] : SpawnRoute[]   ← spawnPoint 만 보유
 ├─ basePoint     : Vector2
 └─ IsWalkable(Vector2Int cell)
        │
        ▼
PathfindingSystem.ComputePath(fromWorld, toWorld, includeStart)
 └─ AStarPathfinder.FindPath(start, goal, isWalkable) → 셀 단위 경로
 └─ SmoothPath()  ← 콜리니어 구간 병합, 코너(굴절점)만 남김
        │
        ▼
WaveSystem.SpawnEnemies() → Enemy.Initialize(..., path)
MapTileSystem.PlaceTower/RemoveTower → PathfindingSystem.RecalculateActiveEnemyPaths() → Enemy.SetPath(newPath)
```

`ComputePath`가 반환하는 배열은 **스무딩된 코너 경로**다. `Enemy`의 실제 이동에는 이 정도 정밀도면 충분하지만, 이번 이슈가 요구하는 "지나가는 셀 하나하나"를 표시하려면 스무딩 이전의 **셀 단위 전체 경로**가 별도로 필요하다.

### 변경 후 흐름

```
MapTileSystem.RouteCount / GetSpawnPoint(i) / GetBasePoint()
        │
        ▼ (각 routeIndex 마다)
PathfindingSystem.ComputeFullCellPath(fromWorld, toWorld) → Vector2[]   ★ 신규 — 스무딩 없이 셀 중심 좌표 그대로 반환
        │
        ▼
MonsterPathVisualizer.RefreshMarkers()   ★ 신규
 └─ 모든 route 의 셀 좌표를 모아 중복 제거 후, 각 좌표에 작은 원(SpriteRenderer) 오브젝트 배치
        │
        ▼
씬에 상시 표시되는 원 마커들 (Play 시작 시 1회 생성, 타워 배치/이동/삭제로 경로가 바뀔 때마다 갱신)
```

`ComputeFullCellPath`는 `ComputePath`와 동일하게 `AStarPathfinder.FindPath` + `MapTileSystem.IsWalkable`을 재사용하되 `SmoothPath` 단계만 생략한다 — 이동 로직(`ComputePath`)과 시각화 로직이 같은 A* 결과에서 분기하므로 두 값이 어긋날 일이 없다.

### 갱신 트리거

기존에 `MapTileSystem.PlaceTower`/`RemoveTower`가 호출하는 `PathfindingSystem.RecalculateActiveEnemyPaths()`가 "경로가 바뀔 수 있는 유일한 시점"(§1, issue-326 문서 "재계산 트리거" 참고)이므로, 이 메서드 끝에 `public static event Action OnPathsChanged`를 추가로 invoke한다. `MonsterPathVisualizer`는 이 이벤트를 구독해 마커를 다시 그린다. 별도의 새 훅을 여러 곳에 심을 필요 없이 기존 재계산 지점 하나만 재사용한다.

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Systems/PathfindingSystem.cs`
- 신규 메서드 `public Vector2[] ComputeFullCellPath(Vector2 fromWorld, Vector2 toWorld)`:
  - `WorldToCell`로 start/goal 셀 변환 → `AStarPathfinder.FindPath(start, goal, MapTileSystem.Instance.IsWalkable)` 호출.
  - 결과가 `null`이면 (경로 없음, 이론상 발생 안 함) `ComputePath`의 폴백과 동일하게 시작/끝 2점만 반환.
  - 스무딩 없이 `cellPath`의 모든 원소를 `CellCenter()`로 변환해 그대로 반환.
- `RecalculateActiveEnemyPaths()` 맨 끝에 `OnPathsChanged?.Invoke();` 추가 (살아있는 적이 0명이어도 무조건 invoke — 마커는 웨이브 진행 여부와 무관하게 항상 최신 경로를 반영해야 함).
- 신규 `public static event Action OnPathsChanged;`

### `docs/product-specs/map-system.md`
- §7 절 신규 추가: "경로 시각화 (#331)" — `MonsterPathVisualizer`가 모든 route의 셀 단위 경로를 상시 원 마커로 표시하며, 타워 배치/이동/삭제 시 갱신됨을 서술.
- 인터페이스 코드 블록에 `PathfindingSystem.ComputeFullCellPath` 추가.

### `MakeDefence/Assets/Scenes/SampleScene.unity`
- 신규 GameObject(`MonsterPathVisualizer`)를 씬에 추가하고 컴포넌트 부착 (UnityMCP `manage_gameobject`/`manage_components` 사용 — AGENTS.md §7).

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/Systems/MonsterPathVisualizer.cs`
- `MonoBehaviour`. 책임: 모든 스폰 루트의 셀 단위 경로를 상시 원 마커로 표시.
- Inspector 노출 필드:
  - `[SerializeField] private Color markerColor = new Color(1f, 1f, 1f, 0.35f);`
  - `[SerializeField] private float markerDiameter = 0.25f;` (셀 크기 1 대비 비율)
  - `[SerializeField] private int sortingOrder = -1;` (타일 위, 몬스터/타워 아래 — 실기에서 값 조정)
- `Start()`: 절차적 원형 `Sprite`를 1회 생성해 캐싱 → `PathfindingSystem.OnPathsChanged += RefreshMarkers` 구독 → `RefreshMarkers()` 최초 실행.
- `OnDestroy()`: 이벤트 구독 해제.
- `RefreshMarkers()`: 기존 마커 전부 `Destroy` → `MapTileSystem.Instance.RouteCount`만큼 순회하며 `PathfindingSystem.Instance.ComputeFullCellPath(GetSpawnPoint(i), GetBasePoint())` 호출 → 좌표를 `HashSet<Vector2>`로 중복 제거(여러 route가 합류하는 셀에 마커가 겹쳐 쌓이지 않도록) → 각 좌표에 원 오브젝트 1개 생성.
- 원 스프라이트는 외부 아트 에셋 없이 런타임에 `Texture2D`로 원형 알파 마스크를 생성해 `Sprite.Create`로 만든다 (프로젝트에 아직 재사용 가능한 원형 스프라이트가 없음 — `GameUIManager`의 원형 표시는 `GL.LINES` 즉시 모드 드로잉이라 실제 GameObject가 아니어서 이번 요구사항(오브젝트 생성)과 맞지 않음).

## 4. 테스트 계획

### EditMode 단위 테스트 (신규)
- [ ] `PathfindingSystem.ComputeFullCellPath`: 장애물 없는 직선 구간에서 반환 배열 길이가 `AStarPathfinder.FindPath` 결과 셀 개수와 동일한지 (스무딩 미적용 확인)
- [ ] `ComputeFullCellPath`: 장애물을 우회하는 경로에서 반환 개수가 `ComputePath`(스무딩됨) 결과보다 크거나 같은지

### 수동 (Unity Editor)
- [ ] Play 모드 진입 직후(웨이브 시작 전)부터 모든 스폰 루트 → 본진 경로에 셀 단위 원 마커가 표시되는지
- [ ] 스폰 루트가 2개 이상인 씬에서 각 루트의 경로가 모두 표시되는지, 합류 지점에서 마커가 중복되지 않는지
- [ ] 타워를 배치해 경로를 막으면 마커가 새 우회 경로로 즉시 갱신되는지
- [ ] 타워를 이동/삭제하면 마커가 그에 맞게 다시 갱신되는지
- [ ] 마커가 몬스터/타워 스프라이트 시인성을 방해하지 않는지 (sortingOrder 값 실기 조정)
- [ ] 웨이브를 여러 번 시작/종료해도 마커 개수가 누적되지 않는지 (매 갱신마다 기존 마커 정리 확인)

## 5. 위험 요소

### R1. 마커 오브젝트 수와 생성 비용
경로 길이에 비례해 수십~수백 개의 원 오브젝트가 생성될 수 있다. 다만 `RefreshMarkers()`는 타워 배치/이동/삭제(유저 조작 시점의 드문 이벤트)에만 호출되므로 프레임당 비용은 없다 — issue-326의 "재계산은 드문 이벤트라 비용 문제 없음" 논리와 동일. 단, 시각적으로 마커가 너무 촘촘해 지저분해 보이지 않는지는 실기 확인이 필요하다.

### R2. 합류 지점 중복 마커
여러 route가 `basePoint` 근처에서 같은 셀을 공유하면 마커가 겹쳐 쌓일 수 있다. `CellCenter()`가 항상 `(x+0.5, y+0.5)` 정수 기반 값만 반환하므로 부동소수점 오차 없이 `HashSet<Vector2>`로 정확히 중복 제거된다.

### R3. 상시 표시로 인한 시각적 노이즈
게임플레이 중 항상 보이는 마커가 타워 배치나 몬스터 시인성을 방해할 위험이 있다. `markerColor`(알파)와 `sortingOrder`를 Inspector 노출 필드로 두어 실기에서 조정 가능하게 완화한다.

### R4. `OnPathsChanged` 정적 이벤트 구독 해제 누락
`OnDestroy()`에서 구독 해제를 빠뜨리면, 씬 전환/오브젝트 파괴 후에도 정적 이벤트가 파괴된 인스턴스의 메서드를 호출해 `MissingReferenceException`이 날 수 있다. 반드시 구독/해제를 쌍으로 구현한다.

### R5. 씬에 중복 배치 시 텍스처 중복 생성
`MonsterPathVisualizer`를 씬에 2개 이상 배치하면 인스턴스마다 별도의 원형 텍스처가 생성되고 마커도 두 배로 그려진다. 씬에는 1개만 배치하도록 한다 (검증 항목에 포함하지 않을 만큼 실수 방지가 쉬우므로 별도 가드 코드는 추가하지 않음).
