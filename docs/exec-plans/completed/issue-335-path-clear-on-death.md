# Issue #335 — 몬스터 이동경로 마커, 스폰 완료가 아닌 마지막 몬스터 소멸 시 숨김

## 1. 시스템 구조

### 현재 흐름 (#335 1차 구현 이후)

```
WaveSystem.SpawnEnemies()
 ├─ 코루틴 진입 시 IsSpawning=true, OnSpawningStateChanged(true) invoke
 └─ 마지막 몬스터를 "생성"한 직후(아직 걷고 있는 다른 몬스터와 무관하게)
    IsSpawning=false, OnSpawningStateChanged(false) invoke
        ▼
MonsterPathVisualizer
 └─ OnSpawningStateChanged(false) 수신 즉시 ClearMarkers()
```

문제: 마지막 몬스터가 "생성"되는 순간 마커가 사라지지만, 그보다 먼저 스폰된 몬스터들은 아직 본진까지 이동 중일 수 있다. 요구사항은 "생성 완료 시점"이 아니라 "스폰된 마지막 몬스터가 화면에서 사라지는(죽거나 본진 도달) 시점"에 경로를 지우는 것.

### 변경 후 흐름

`WaveSystem`은 이미 `_aliveCount`를 통해 "스폰된 몬스터가 모두 사라졌는지"를 정확히 추적하고 있다 (`Enemy.OnEnemyDied`/`Enemy.OnEnemyReachedBase` → `HandleEnemyRemoved` → `_aliveCount--` → 0이 되면 `EndWave()`). 이 시점은 이미 기존 `OnWaveStarted` / `OnWaveEnded` 이벤트로 노출되어 있고, `IsWaveActive`는 정확히 "웨이브 시작 ~ 마지막 몬스터 소멸(또는 플레이어 사망으로 인한 강제 종료)" 구간과 일치한다.

```
WaveSystem
 ├─ StartWave/StartRiftWave: IsWaveActive=true, OnWaveStarted invoke   (기존, 변경 없음)
 ├─ EndWave(): _aliveCount<=0 되는 시점(=스폰된 마지막 몬스터 소멸) → OnWaveEnded invoke  (기존, 변경 없음)
 └─ StopWave(): 플레이어 사망 등으로 강제 종료 → HandlePlayerDied 에서 OnWaveEnded(false) invoke (기존, 변경 없음)
        ▼
MonsterPathVisualizer
 └─ OnWaveStarted  → RefreshMarkers() (표시 시작 — 스폰 시작 시점, 기존과 동일 타이밍)
 └─ OnWaveEnded    → ClearMarkers()   (표시 종료 — 마지막 몬스터가 실제로 사라지는 시점으로 지연됨)
 └─ PathfindingSystem.OnPathsChanged 구독은 유지하되, IsWaveActive 인 동안만 RefreshMarkers() 실행
```

`IsSpawning`/`OnSpawningStateChanged`는 `MonsterPathVisualizer`가 유일한 소비자였으므로(grep 확인 완료), 더 이상 필요한 상태 전이가 아니면 `WaveSystem`에서 제거한다. "스폰 코루틴이 도는 중"이라는 개념 자체가 이번 요구사항에는 불필요하고, `IsWaveActive`/`OnWaveStarted`/`OnWaveEnded`만으로 정확히 원하는 구간을 표현할 수 있다.

### 추가 발견: 멀티 루트(스폰 지점 복수) 환경에서의 부분 소멸

구현 후 수동 테스트 중 다음 문제가 발견됨: 스폰 루트가 2개 이상일 때, 스폰 1의 몬스터가 아직 살아있고 스폰 2의 몬스터만 먼저 전부 사라진 경우에도 **스폰 2의 경로까지** 계속 표시된다. 이는 `IsWaveActive`/`OnWaveEnded`가 전체 웨이브 단위(`_aliveCount` 합계)로만 정의되어 있어, `MonsterPathVisualizer`가 모든 route를 하나로 묶어 표시/숨김을 결정하기 때문이다.

요구사항을 route 단위까지 정확히 만족시키려면 "route별 생존 카운트"가 필요하다:

```
WaveSystem
 ├─ StartWave/StartRiftWave: route별 스폰 예정 수를 세어 _aliveCountByRoute[route] 초기화
 ├─ HandleEnemyRemoved(enemy): 전체 _aliveCount-- 뿐 아니라 _aliveCountByRoute[enemy.RouteIndex]-- 도 수행
 │    └─ 특정 route의 카운트가 0이 되면 OnRouteCleared(route) invoke      ★ 신규
 └─ (전체 _aliveCount<=0 이면 기존과 동일하게 EndWave() → OnWaveEnded)
        ▼
MonsterPathVisualizer
 └─ 활성 route 집합(_activeRoutes)을 웨이브 시작 시 전체 route로 채우고,
    OnRouteCleared 수신 시 해당 route만 집합에서 제거 후 RefreshMarkers()
    (마커는 _activeRoutes 에 속한 route들의 경로만 합쳐 그린다 — 특정 route가 끝나도
     다른 route와 겹치는 셀은 그 route가 살아있는 한 계속 표시됨)
 └─ OnWaveEnded 수신 시에는 (강제 취소 경로 포함) _activeRoutes 를 통째로 비우고 ClearMarkers()
```

Enemy는 현재 자신이 어느 route에서 스폰됐는지 정보가 없으므로(`Enemy.Initialize`에 route 정보 없음), `Enemy.RouteIndex` 프로퍼티를 추가하고 `WaveSystem.SpawnEnemies()`에서 스폰 시 채워준다.

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
- `public int RouteIndex { get; private set; }` 추가.
- `Initialize(EnemyData, int, Vector2[])` / `Initialize(EnemyData, int, Vector2[], RiftWaveModifiers)` 양쪽에 `int routeIndex` 매개변수를 추가해 `RouteIndex`를 설정한다.

### `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
- `public bool IsSpawning { get; private set; }` 제거.
- `public static event Action<bool> OnSpawningStateChanged;` 제거.
- `SpawnEnemies()` 내부의 `IsSpawning = true/false; OnSpawningStateChanged?.Invoke(...)` 호출부 전부 제거 (코루틴 진입부, 마지막 스폰 직후, `data == null` 가드 분기).
- `StopWave()`: `IsSpawning` 관련 블록은 제거하되, `IsWaveActive`가 `true`였던 경우에만 `OnWaveEnded?.Invoke(false)`를 직접 invoke하도록 유지한다 — `TestRunner`의 R키 리셋처럼 `HandlePlayerDied`를 거치지 않고 `StopWave()`를 직접 호출하는 경로(Codex 리뷰 지적, P2)에서도 구독자가 웨이브 취소를 통지받아야 하기 때문. `HandlePlayerDied()`에서 중복으로 invoke하던 `OnWaveEnded?.Invoke(false)`는 제거해 이중 호출을 피한다.
- `SpawnEnemies()`의 `data == null` 가드에서도 `yield break` 전에 `StopWave()`를 호출해 동일한 취소 경로(`OnWaveEnded(false)`)를 타도록 한다 (Codex 리뷰 지적, P2 — 안 그러면 `_aliveCount`가 0에 도달할 계기가 없어 웨이브/마커가 영구히 걸림).
- `private int[] _aliveCountByRoute;` 필드 추가. `StartWave()`/`StartRiftWave()`에서 spawnList 확정 직후, `routeIndex = i % routeCount` 분배 규칙에 맞춰 route별 스폰 예정 수를 계산해 초기화한다.
- `public static event Action<int> OnRouteCleared;` 추가 — 특정 route의 `_aliveCountByRoute[route]`가 0이 되는 순간 invoke.
- `public bool IsRouteActive(int routeIndex)` 추가 — `MonsterPathVisualizer`가 늦게 구독(Start 시점에 이미 웨이브 진행 중)할 때 route별 초기 상태를 정확히 복원하기 위함.
- `SpawnEnemies()`: `enemy.Initialize(...)` 호출 시 `routeIndex`를 함께 전달.
- `HandleEnemyRemoved(Enemy enemy)`: 기존 `_aliveCount--` 로직에 더해 `_aliveCountByRoute[enemy.RouteIndex]--` 수행, 0이 되면 `OnRouteCleared?.Invoke(enemy.RouteIndex)` invoke. 이후 기존과 동일하게 전체 `_aliveCount<=0` 체크로 `EndWave()` 호출.
- 그 외 웨이브 시작/종료/aliveCount 로직은 변경 없음.

### `MakeDefence/Assets/Scripts/Systems/MonsterPathVisualizer.cs`
- `WaveSystem.OnSpawningStateChanged += HandleSpawningStateChanged` 구독을 `WaveSystem.OnWaveStarted += HandleWaveStarted` / `WaveSystem.OnWaveEnded += HandleWaveEnded` / `WaveSystem.OnRouteCleared += HandleRouteCleared` 세 개 구독으로 교체. `OnDestroy()`에서도 동일하게 해제.
- `_isSpawning` 필드를 `_isWaveActive`로 이름 변경(의미상 "스폰 중"이 아니라 "웨이브 진행 중"이므로).
- `private readonly HashSet<int> _activeRoutes = new();` 필드 추가 — 아직 스폰된 몬스터가 남아있는 route 집합.
- `Start()`의 초기 상태 체크: `WaveSystem.Instance.IsWaveActive`이면 `_isWaveActive = true`로 설정하고, `MapTileSystem.Instance.RouteCount`만큼 순회하며 `WaveSystem.Instance.IsRouteActive(r)`가 true인 route만 `_activeRoutes`에 채운 뒤 `RefreshMarkers()`.
- 신규 `HandleWaveStarted(int stage)`: `_isWaveActive = true;` 설정 후 `_activeRoutes`를 `MapTileSystem.Instance.RouteCount` 전체로 채우고 `RefreshMarkers()`.
- 신규 `HandleWaveEnded(bool cleared)`: `_isWaveActive = false; _activeRoutes.Clear(); ClearMarkers();` (cleared 값과 무관하게 항상 전체 숨김 — 정상 종료/강제 취소 공통 안전망).
- 신규 `HandleRouteCleared(int routeIndex)`: `_activeRoutes.Remove(routeIndex); RefreshMarkers();` (웨이브가 끝나지 않은 상태에서만 의미 있게 호출됨 — `WaveSystem.HandleEnemyRemoved`가 `IsWaveActive` false일 때는 애초에 invoke하지 않음).
- `RefreshMarkers()`: 기존에는 `MapTileSystem.Instance.RouteCount` 전체를 순회했지만, 이제 `_activeRoutes`에 포함된 route만 순회해 경로를 그린다 (다른 route와 겹치는 셀은 dedup 로직 그대로 유지되므로 자연히 계속 표시됨).
- `HandlePathsChanged()`의 가드를 `if (_isWaveActive) RefreshMarkers();`로 변경 (기존 유지, `_activeRoutes` 기준으로 다시 그림).
- 기존 `HandleSpawningStateChanged` 메서드는 삭제.

### `docs/product-specs/map-system.md`
- §7 설명 문구를 "스폰 코루틴 진행 중에만 표시, 마지막 몬스터 생성 직후 숨김" → "웨이브 진행 중(스폰 시작 ~ 스폰된 마지막 몬스터가 죽거나 본진에 도달해 모두 사라질 때까지) 표시"로 수정.
- `IsSpawning` / `OnSpawningStateChanged` API 목록을 제거하고, 대신 이미 문서화된 `OnWaveStarted` / `OnWaveEnded`를 트리거로 참조하도록 수정.

## 3. 신규 클래스 / 파일
없음 (기존 두 클래스 + 문서 1건만 수정).

## 4. 테스트 계획

### 수동 (Unity Editor)
- [ ] Play 모드 진입 직후, 웨이브 시작 전에는 마커가 보이지 않는지
- [ ] `StartWave` 직후 첫 몬스터가 스폰되는 순간부터 마커가 보이는지 (기존 동작 유지)
- [ ] 스폰 리스트의 마지막 몬스터가 "생성"된 시점에는 마커가 사라지지 않고, 그 몬스터를 포함해 스폰된 모든 몬스터가 죽거나 본진에 도달해 화면에서 사라진 시점에 비로소 마커가 사라지는지
- [ ] 스폰이 끝났지만 이전에 스폰된 몬스터가 아직 이동/전투 중인 구간에도 마커가 계속 보이는지, 그 동안 타워를 배치해 경로를 막으면 마커가 새 경로로 즉시 갱신되는지
- [ ] 모든 몬스터가 사라져 웨이브가 클리어된 뒤(마커 숨김 상태) 타워를 배치/삭제해도 에러 없이 조용히 무시되는지
- [ ] 웨이브 도중 플레이어가 사망(`StopWave`)할 때 마커가 즉시 사라지는지 (살아있는 몬스터가 남아있어도 게임이 종료되므로 즉시 숨김이 맞는 동작)
- [ ] `StartRiftWave` (균열 웨이브)도 동일하게 동작하는지
- [ ] 오토웨이브(`_autoWave`)로 웨이브가 연속 시작될 때 `OnWaveEnded(true)` → `OnWaveStarted` 재호출 사이에 마커가 깜빡이지 않고 자연스럽게 갱신되는지
- [ ] (사용자 리포트, 2건 코드리뷰 이후 발견) 스폰 루트가 2개 이상인 맵에서, 스폰 1의 몬스터가 아직 살아있는 상태로 스폰 2의 몬스터만 먼저 전부 사라지면 스폰 2의 경로만 즉시 사라지고 스폰 1의 경로는 계속 표시되는지 — 이후 스폰 1의 몬스터도 마저 사라지면 전체 경로가 사라지는지
- [ ] TestRunner R키로 리셋할 때 route별 상태(`_activeRoutes`)도 함께 초기화되어, 다음 웨이브 시작 시 모든 route가 정상적으로 다시 표시되는지

## 5. 위험 요소

### R1. `EndWave()`의 상태 가드로 인한 `OnWaveEnded` 미발생 가능성
`EndWave()` 최상단에 `if (GameStateSystem.Current != GameState.Playing) return;` 가드가 있다. 플레이어 사망으로 이미 `Defeat` 상태가 된 이후 남은 몬스터의 `_aliveCount`가 0이 되어도 `EndWave()`가 조용히 리턴해 `OnWaveEnded`가 다시 발생하지 않는다. 하지만 이 경로는 `HandlePlayerDied()` → `StopWave()` 직후 이미 `OnWaveEnded?.Invoke(false)`가 호출되므로 마커는 이미 숨겨진 상태이며 중복 호출이 없을 뿐 문제는 없다. 구현 후 확인 필요.

### R2. `OnWaveStarted`가 스폰 시작 "직전"에 발생
`OnWaveStarted`는 `StartWave()`/`StartRiftWave()`에서 스폰 코루틴을 시작하기 직전에 invoke된다. 기존 `OnSpawningStateChanged(true)`와 사실상 동일한 타이밍(같은 프레임)이라 체감 차이는 없다. 경로 계산(`ComputeFullCellPath`)은 실제 몬스터 존재 여부와 무관하게 스폰 지점→본진 좌표만으로 이뤄지므로 몬스터가 아직 하나도 생성되지 않은 시점에 호출해도 문제없다.

### R3. `IsSpawning`/`OnSpawningStateChanged` 제거로 인한 API 소실
현재 저장소 전체에서 `MonsterPathVisualizer`가 유일한 소비자임을 grep으로 확인했다. 제거 후 다른 스크립트에서 컴파일 에러가 나지 않는지 `read_console`로 재확인한다.

### R4. 이벤트 구독/해제 누락
`OnWaveStarted`, `OnWaveEnded` 모두 정적 이벤트이므로 `OnDestroy()`에서 반드시 구독 해제해야 씬 전환 후 `MissingReferenceException`을 방지할 수 있다 (#331/#335 R4와 동일한 패턴).

### R5. 오토웨이브 연속 진행 시 마커 깜빡임
`_autoWave`가 켜져 있으면 `EndWave()`에서 `OnWaveEnded?.Invoke(true)` 직후 곧바로 `StartWave()`가 호출되어 `OnWaveStarted`가 재발생한다. `ClearMarkers()` → `RefreshMarkers()`가 같은 프레임 내에서 연달아 실행되므로 화면상 깜빡임은 거의 없을 것으로 예상되지만, 실기기/에디터에서 체감 확인이 필요하다.
