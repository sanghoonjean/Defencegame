# Issue #253 — 다중 스폰 + 단일 종착점 합류 시스템

> **핵심**: 현재 "한 곳에서 순차 스폰 → 단일 경로 이동" 을 "여러 스폰 지점에서 동시 스폰 → 공통 종착점으로 합류" 로 변경.
>
> 파격적 신규 시스템이 아니라, `MapTileSystem` 의 단일 경로 필드를 배열(SpawnRoute[]) 로 승격하고 `WaveSystem.SpawnEnemies` 코루틴 하나가 라운드로빈으로 배분하는 최소 변경.

## 1. 시스템 구조

### 현재 흐름

```
MapTileSystem                          (Inspector)
 ├─ spawnPoint  : Vector2              ← 단일
 ├─ waypoints[] : Vector2[]            ← 단일 경로
 └─ basePoint   : Vector2              ← 종착점
        │
        ▼ GetFullPath() → Vector2[] (spawn + waypoints + base)
        │
WaveSystem.SpawnEnemies()
 └─ for enemy in spawnList:
       enemy.Initialize(data, stage, waypoints[])
       WaitForSeconds(spawnInterval)   ← 1마리씩 순차
        │
        ▼
Enemy.MoveAlongPath()                  (이미 인스턴스별 waypoints 배열 보유)
```

### 변경 후 흐름

```
MapTileSystem                          (Inspector)
 ├─ spawnRoutes[] : SpawnRoute[]       ★ 신규 — 각 원소가 {spawnPoint, waypoints[]}
 └─ basePoint     : Vector2            ← 공통 종착점 (변경 없음)
        │
        ▼ GetFullPath(routeIndex) → Vector2[]  (route.spawn + route.waypoints + basePoint)
        │
WaveSystem.SpawnEnemies()
 └─ int r = 0 ; for enemy in spawnList:
       var wp = MapTileSystem.Instance.GetFullPath(r % RouteCount);
       enemy.Initialize(data, stage, wp, _currentRiftMods);
       r++;
       WaitForSeconds(spawnInterval)   ← 각 틱마다 다음 경로 사용
        │
        ▼
Enemy.MoveAlongPath()                  ← 코드 변경 無 (이미 각자 경로 소유)
```

### 스폰 리듬 (핵심 결정)

두 후보:

| 방식 | 동작 | 장단 |
|---|---|---|
| **A. 라운드로빈 순차** (권장) | `spawnInterval` 마다 다음 경로에서 1마리. N개 경로면 N턴 뒤 첫 경로로 회귀 | 기존 총 스폰 수/시간 유지. 밸런스 회귀 위험 최소. "여러 곳에서 밀려온다" 는 체감은 여전히 발생 |
| B. 동시 스폰 | `spawnInterval` 마다 모든 경로에서 동시 1마리씩 | 총 몬스터 수 유지 시 웨이브 지속시간 1/N, 유지 안 하면 실제 부담 N배. 밸런스 재조정 필수 |

**결정: A** 로 시작. `BuildSpawnList` / `GetEnemyCount` 는 그대로 두어 밸런스 영향 최소. 추후 B(동시 스폰)로 확장하고 싶으면 `spawnPerTick` 파라미터 추가.

### Enemy 측 변경 여부

Enemy 는 이미 `Initialize(EnemyData, int, Vector2[], RiftWaveModifiers)` 시그니처로 **인스턴스별 waypoints 배열**을 받으므로 **코드 변경 불필요**. 여러 경로 각각의 `Vector2[]` 를 넘겨주기만 하면 된다.

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Systems/MapTileSystem.cs`
- 내부에 다음 nested 타입 추가 — **필드에 반드시 `[SerializeField]` 명시** (또는 `public`). 접근 지정자를 생략하면 C# 기본값이 `private` 이라 struct 자체는 배열로 직렬화돼도 안의 필드들이 Inspector 에 나오지 않고 저장/로드되지 않아 SampleScene 마이그레이션이 빈 원소로 채워짐 (Codex #323 line 66 지적):
  ```csharp
  [Serializable]
  public struct SpawnRoute
  {
      [SerializeField] public Vector2 spawnPoint;
      [SerializeField] public Vector2[] waypoints;
  }
  ```
- `[SerializeField] Vector2 spawnPoint` / `[SerializeField] Vector2[] waypoints` → `[SerializeField] SpawnRoute[] spawnRoutes` 로 대체
- `GetSpawnPoint()`, `GetWaypoints()`, `GetFullPath()` → 인덱스 오버로드 추가
  - `int RouteCount { get; }`
  - `Vector2 GetSpawnPoint(int routeIndex)`
  - `Vector2[] GetFullPath(int routeIndex)`
  - 기존 무인자 버전은 `routeIndex=0` 으로 위임 (호출부 하위 호환)

### `MakeDefence/Assets/Scripts/Systems/PathfindingSystem.cs`
- 위 인덱스 오버로드 그대로 프록시 노출:
  - `int RouteCount => MapTileSystem.Instance?.RouteCount ?? 0`
  - `Vector2 GetSpawnPoint(int routeIndex)`
  - `Vector2[] GetFullPath(int routeIndex)`

### `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
- **빈 route guard 는 `StartWave` / `StartRiftWave` 진입부** 에서 조기 return 으로 처리 (Codex #323 line 94 지적):
  - 현재 흐름은 `IsWaveActive = true` → `_aliveCount = spawnList.Count` → `StartCoroutine(SpawnEnemies)` 순서. `SpawnEnemies` 안에서 `yield break` 만 하면 `_aliveCount` 가 감소할 계기가 없어 `EndWave` 도 못 불려 웨이브가 **stuck**.
  - 따라서 아래 순서로 방어:
    ```csharp
    public void StartWave() {
        if (IsWaveActive) return;
        if (MapTileSystem.Instance == null || MapTileSystem.Instance.RouteCount == 0) {
            Debug.LogError("[WaveSystem] spawnRoutes 미설정 — 웨이브 시작 취소");
            return; // IsWaveActive = false 유지, _aliveCount 손대지 않음
        }
        IsWaveActive = true;
        // ... 기존 로직
    }
    ```
  - `StartRiftWave` 도 동일하게 앞단에서 방어. `SpawnEnemies` 내부는 이 시점에서 `RouteCount >= 1` 이 보장된 상태로 진입.
- `SpawnEnemies` 를 라운드로빈으로 수정:
  ```csharp
  int routeCount = MapTileSystem.Instance.RouteCount; // >=1 보장
  int r = 0;
  foreach (var grade in spawnList) {
      var wp = MapTileSystem.Instance.GetFullPath(r % routeCount);
      var enemy = ObjectPoolSystem.Instance.Get();
      enemy.Initialize(data, CurrentStage, wp, _currentRiftMods);
      r++;
      yield return new WaitForSeconds(spawnInterval);
  }
  ```
- 리프트 웨이브 경로는 별도로 두지 않음 — 같은 `spawnRoutes` 재사용

### `MakeDefence/Assets/Scenes/SampleScene.unity`
- `MapTileSystem` 컴포넌트에서
  - 기존 `spawnPoint` / `waypoints` 필드가 사라짐 → 유니티가 자동으로 값 폐기
  - `spawnRoutes` 배열에 **최소 2개** 를 채운다 (수동 검증 목적):
    - Route 0: 현재 spawnPoint + waypoints 를 그대로 이식
    - Route 1: 반대편/측면에서 시작해 base 로 향하는 새 경로 (좌표는 구현 단계에서 실기 검토)
- ⚠️ `.unity` 직접 YAML 편집 금지 — UnityMCP `manage_components` 로 수정 ([feedback_unity_asset_edits](../../../../.claude/projects/C--Users-kalon-Documents-GitHub-Defencegame/memory/feedback_unity_asset_edits.md))

### `docs/product-specs/map-system.md`
- 경로가 다중이라는 사실 반영 (요약 문단만 갱신)

## 3. 신규 클래스 / 파일

**없음** — `SpawnRoute` 는 `MapTileSystem.cs` 내부 nested `[Serializable] struct` 로 둔다 (파일 분리하면 리팩터 비용 대비 이득 없음).

## 4. 테스트 계획

### 수동 (Unity Editor)

전제: SampleScene, MapTileSystem 컴포넌트에 route 2개 설정.

- [ ] 웨이브 시작 → 1턴차 몬스터가 Route 0 스폰점에서 출현
- [ ] 다음 `spawnInterval` 후 몬스터가 Route 1 스폰점에서 출현
- [ ] 이후 Route 0/1 이 번갈아 스폰되는지 육안 확인
- [ ] 두 경로 모두 최종적으로 `basePoint` 에 도달 (합류 확인)
- [ ] 총 스폰 마릿수 = `GetEnemyCount(stage)` 로 기존과 동일
- [ ] 웨이브 종료 조건이 정상 트리거 (kill + reachBase 합계로 `_aliveCount == 0`)
- [ ] 차원석 리프트 웨이브도 다중 경로에서 정상 스폰
- [ ] `spawnRoutes` 가 비어 있을 때 에러 로그만 나오고 게임이 크래시하지 않음 (방어 로직 검증)

### EditMode

- `MapTileSystem.GetFullPath(int)` 단위 테스트 추가:
  - route i 의 결과가 `[route[i].spawn +0.5, ...route[i].waypoints +0.5, basePoint +0.5]` 순서로 조립되는지
  - 인덱스 범위 밖 요청 시 처리 (clamp vs empty) — 정책 결정 필요, 초안: `Debug.LogError` + `new Vector2[0]`

## 5. 위험 요소

### R1. Inspector 재설정 필요
`spawnPoint` / `waypoints` 필드 제거 시 유니티는 값 폐기. **먼저 값을 메모해 두고 `spawnRoutes[0]` 로 이식**하는 순서로 진행해야 데이터 손실 없음. 커밋 전 SampleScene 실기 로드해서 첫 경로가 이전과 동일한지 확인.

### R2. 밸런스 영향
라운드로빈은 총 스폰 수/시간을 그대로 유지하므로 이론적으론 밸런스 영향 미미. 그러나 **타워 배치 지역이 특정 경로에 편중된 맵** 은 다른 경로 쪽에서 관통되는 사고가 생길 수 있음. Route 1 초안은 Route 0 과 어느 정도 지역이 겹치는 완만한 경로로 잡아, 극단 밸런스 사고 회피.

### R3. 리프트 웨이브
`WaveSystem.StartRiftWave` 도 동일 `SpawnEnemies` 를 쓰므로 자동으로 다중 스폰됨. `RiftWaveModifiers` 는 개별 적 능력치만 조작하므로 경로 다중화와 충돌 없음. 다만 리프트 모디파이어 중 "경로/이동 관련" 이 신설되면 재검토.

### R4. 호출부 하위 호환
`GetFullPath()` / `GetSpawnPoint()` 무인자 호출이 게임 다른 곳(예: 카메라 초기 위치, UI 아이콘, 디버그 툴) 에서 쓰이면 항상 `route 0` 을 반환한다. 스폰 지점 아이콘 표시가 있다면 여러 개 렌더링하도록 별도 이슈로 분리.

### R5. `Enemy.Initialize` 호출 인자 개수
현재 `enemy.Initialize(data, stage, waypoints, _currentRiftMods)` — 이번 변경으로 signature 는 그대로 두고 `waypoints` 만 route 별로 바꾼다. Enemy 코드는 손대지 않는다.

### R6. Object pool 재사용
`ObjectPoolSystem.Get()` 이 반환하는 재활용 Enemy 는 이전 웨이포인트를 여전히 필드에 갖고 있을 수 있는데, `Initialize` 에서 `_waypoints = waypoints; _waypointIndex = 0` 로 항상 덮어쓰므로 문제 없음 (확인 완료).

### R7. `SpawnRoute` 필드 직렬화 (Codex #323 line 66)
struct 필드에 접근 지정자를 생략하면 C# 기본값이 `private` 이라 Unity 가 각 원소 내부를 저장/노출하지 않는다. → 배열은 유지되지만 안이 항상 빈 채로 남아 Inspector 에서 채울 수 없다. **반드시 `public` 또는 `[SerializeField]`** 를 명시 (§2 코드 스니펫 참조).

### R8. 빈 `spawnRoutes` guard 위치 (Codex #323 line 94)
`StartWave` / `StartRiftWave` 는 `IsWaveActive = true` + `_aliveCount = spawnList.Count` 를 세팅한 뒤 `SpawnEnemies` 를 실행한다. 빈 route 방어를 `SpawnEnemies` 내부에서 `yield break` 로만 처리하면 `_aliveCount` 가 감소할 계기가 없어 `EndWave` 도 호출되지 않아 **웨이브가 stuck**. → 방어는 반드시 `StartWave` / `StartRiftWave` **진입부** 에서 `IsWaveActive` / `_aliveCount` 를 세팅하기 전에 조기 return 한다 (§2 코드 스니펫 참조).

## 6. 오픈 이슈 (Plan PR 에서 확정)

- **Q1**: Route 개수 상한을 두는가? (초안: 상한 없음, Inspector 자유)
- **Q2**: 경로별 몬스터 종류/수를 다르게 배분할 수 있어야 하나? (초안: **아니오** — 별도 이슈로 분리)
- **Q3**: `spawnRoutes` 비었을 때 fallback 로 이전 `spawnPoint/waypoints` 를 읽는 마이그레이션 코드를 넣을지? (초안: **넣지 않음** — 사용 씬이 SampleScene 1개뿐, 수동 이식이 더 확실)
- **Q4**: Route 1 의 실제 좌표 — 플랜 승인 후 실기 배치 단계에서 결정
