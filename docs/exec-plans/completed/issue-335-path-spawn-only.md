# Issue #335 — 몬스터 이동경로 시각화를 몬스터 생성(스폰) 중에만 표시

## 1. 시스템 구조

### 현재 흐름 (#331 이후)

```
MonsterPathVisualizer.Start()
 └─ RefreshMarkers() 즉시 1회 실행 → 이후 PathfindingSystem.OnPathsChanged 이벤트로만 갱신
 └─ Play 모드 진입 직후부터 웨이브 진행 상태와 무관하게 항상 마커 표시
```

`WaveSystem`은 웨이브 단위 상태(`IsWaveActive`, `OnWaveStarted`/`OnWaveEnded`)만 노출하며, "스폰 코루틴이 몬스터를 순차 생성 중인지"를 나타내는 상태는 없다. `SpawnEnemies()` 코루틴은 `StartWave`/`StartRiftWave`에서 시작되어 `spawnList`를 모두 소진하면 종료되지만, 이 시점을 외부에서 관찰할 방법이 없다.

### 변경 후 흐름

```
WaveSystem
 ├─ IsSpawning { get; }                          ★ 신규
 └─ static event Action<bool> OnSpawningStateChanged;  ★ 신규
        │  SpawnEnemies() 진입 시 true, 종료(정상 소진) 시 false 로 invoke
        │  StopWave() 로 중도 중단 시에도 false 로 invoke (플레이어 사망 등)
        ▼
MonsterPathVisualizer
 └─ Start: WaveSystem.OnSpawningStateChanged 구독 (기존 OnPathsChanged 구독과 동일하게 Start/OnDestroy 쌍으로 관리)
 └─ isSpawning=true  → RefreshMarkers() (마커 생성/갱신)
 └─ isSpawning=false → ClearMarkers()   (마커 전부 제거, 재생성 없음)
 └─ PathfindingSystem.OnPathsChanged 구독은 유지하되, 현재 스폰 중일 때만 RefreshMarkers() 실행
    (스폰 종료 후 타워를 배치해도 숨겨진 마커를 다시 그리지 않음)
```

스폰 시작/종료 시점만 `WaveSystem`이 소유한 상태이므로, 마커의 표시/숨김 트리거를 `WaveSystem`에 두고 `MonsterPathVisualizer`가 구독하는 구조가 기존 `OnPathsChanged` 패턴과 일관적이다.

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
- `public bool IsSpawning { get; private set; }` 추가.
- `public static event Action<bool> OnSpawningStateChanged;` 추가.
- `SpawnEnemies()` 코루틴 최상단에서 `IsSpawning = true; OnSpawningStateChanged?.Invoke(true);` 실행.
- `SpawnEnemies()` 루프에서 **마지막 몬스터(`i == spawnList.Count - 1`)를 스폰한 직후, 그 뒤의 `yield return new WaitForSeconds(spawnInterval)`를 기다리기 전에** `IsSpawning = false; OnSpawningStateChanged?.Invoke(false);`를 실행한다.
  - 기존 루프는 스폰마다(마지막 포함) 항상 `WaitForSeconds`를 거치므로, 코루틴이 완전히 끝난 뒤에 상태를 false로 바꾸면 마지막 몬스터가 이미 등장한 후에도 `spawnInterval` 만큼 마커가 한 박자 늦게 사라진다. "생성이 끝나는 즉시 숨김"이라는 요구사항을 지키려면 상태 전환을 마지막 스폰 직후 지점으로 옮겨야 한다 (Codex 리뷰 지적 반영).
  - 기존 `data == null` 가드의 `yield break` 경로도 스폰 중단이므로, `yield break` 직전에 동일하게 `IsSpawning = false; OnSpawningStateChanged?.Invoke(false);`를 실행한다.
- `StopWave()`: `StopCoroutine`은 코루틴을 즉시 중단시켜 코루틴 내부의 종료 처리 코드가 실행되지 않으므로, `StopWave()` 안에서도 `IsSpawning`이 true였다면 `IsSpawning = false; OnSpawningStateChanged?.Invoke(false);`를 명시적으로 호출.

### `MakeDefence/Assets/Scripts/Systems/MonsterPathVisualizer.cs`
- `Start()`: 무조건 `RefreshMarkers()` 호출하던 부분 제거. 대신 `WaveSystem.Instance`가 이미 스폰 중이면(늦은 구독 등 엣지 케이스 대비) 초기 상태를 맞춰준다.
- `Start()`에서 `WaveSystem.OnSpawningStateChanged += HandleSpawningStateChanged` 구독, `OnDestroy()`에서 해제 — 기존 `PathfindingSystem.OnPathsChanged` 구독과 동일하게 `Start`/`OnDestroy` 쌍으로만 관리한다 (`OnEnable`/`OnDisable`을 함께 쓰면 비활성화-재활성화 시 중복 구독되어 스폰 상태 전환마다 마커 갱신이 중복 실행될 수 있음 — Codex 리뷰 지적 반영).
- `PathfindingSystem.OnPathsChanged` 콜백(`RefreshMarkers`)은 `_isSpawning`이 true일 때만 실제로 마커를 다시 그리도록 가드 추가.
- 신규 `HandleSpawningStateChanged(bool spawning)`:
  - `spawning == true` → `_isSpawning = true; RefreshMarkers();`
  - `spawning == false` → `_isSpawning = false; ClearMarkers();`
- 신규 `ClearMarkers()`: 기존 `RefreshMarkers()` 앞부분의 마커 제거 로직(`foreach Destroy` + `_markers.Clear()`)을 별도 메서드로 추출해 `RefreshMarkers()`와 `ClearMarkers()` 양쪽에서 재사용.

### `docs/product-specs/map-system.md`
- §7 "경로 시각화 (#331)" 절 내용을 갱신: 상시 표시 → "웨이브 스폰 진행 중에만 표시"로 문구 수정, `WaveSystem.OnSpawningStateChanged` 트리거 설명 추가.

## 3. 신규 클래스 / 파일
없음 (기존 두 클래스만 수정).

## 4. 테스트 계획

### 수동 (Unity Editor)
- [ ] Play 모드 진입 직후, 웨이브 시작 전에는 마커가 보이지 않는지
- [ ] 웨이브 시작(`StartWave`) 직후 첫 몬스터가 스폰되는 순간부터 마커가 보이는지
- [ ] 스폰 리스트의 마지막 몬스터가 생성된 직후 마커가 곧바로 사라지는지 — `spawnInterval` 만큼 지연되지 않고 마지막 몬스터 등장 시점에 바로 숨겨져야 함 (이전에 스폰된 몬스터들이 아직 이동/전투 중이어도 무관)
- [ ] 스폰 진행 중 타워를 배치해 경로를 막으면 마커가 새 경로로 즉시 갱신되는지 (기존 #331 동작 유지)
- [ ] 스폰이 끝난 뒤(마커 숨김 상태) 타워를 배치/삭제해도 에러 없이 조용히 무시되는지
- [ ] 웨이브 도중 플레이어가 사망해 `StopWave()`가 스폰 중간에 호출될 때 마커가 즉시 사라지고 `IsSpawning`이 false로 남는지 (다음 웨이브 시작 시 정상 표시되는지)
- [ ] `StartRiftWave` (균열 웨이브)도 동일하게 스폰 중에만 마커가 표시되는지
- [ ] 오토웨이브(`_autoWave`)로 웨이브가 연속 시작될 때 매 웨이브 스폰 구간마다 마커가 정상적으로 표시/숨김 반복되는지 (이벤트 중복 구독/누락 없는지)

## 5. 위험 요소

### R1. `StopWave()`의 코루틴 강제 종료로 인한 상태 불일치
`StopCoroutine`은 코루틴 내부의 이후 코드(정상 종료 시 `IsSpawning = false` 설정)를 실행시키지 않는다. `StopWave()` 안에서 별도로 `IsSpawning`을 false로 되돌리고 이벤트를 invoke하지 않으면, 플레이어 사망 등으로 웨이브가 중단됐을 때 마커가 계속 표시된 채로 남을 수 있다. 플랜 §2에서 명시한 대로 `StopWave()`에 정리 코드를 반드시 포함한다.

### R2. `data == null` 가드 분기의 `yield break`
`SpawnEnemies()` 중간에 `EnemyData`가 null이면 `yield break`로 코루틴이 즉시 끝난다. 이 경로에서도 `IsSpawning = false` 처리가 빠지면 R1과 동일한 문제가 발생한다. 정상 종료 경로와 이 예외 경로 모두 상태 정리가 되도록 구현 시 확인이 필요하다 (예: `finally` 블록 또는 두 지점 모두에 정리 코드 배치).

### R3. 스폰 종료 후 경로 재계산 반영 누락
스폰이 끝난 뒤(마커 숨김 상태)에 타워를 배치/삭제하면 `PathfindingSystem.OnPathsChanged`가 여전히 invoke되지만 `MonsterPathVisualizer`는 이를 무시(가드)한다. 이후 다음 웨이브가 시작될 때 `RefreshMarkers()`가 그 시점의 최신 경로를 다시 계산하므로 데이터 자체는 항상 최신이나, "스폰 중이 아닐 때 경로가 바뀌어도 마커가 갱신되지 않는 것"은 이번 이슈의 의도된 동작(숨김 상태이므로)임을 테스트 시 혼동하지 않도록 유의한다.

### R4. 이벤트 구독/해제 누락
`OnPathsChanged`와 마찬가지로 `OnSpawningStateChanged`도 정적 이벤트이므로 `OnDestroy()`에서 반드시 구독 해제해야 씬 전환 후 `MissingReferenceException`을 방지할 수 있다 (#331 R4와 동일한 패턴).

### R5. 마지막 스폰 이후 대기 시간만큼 상태 전환이 늦어지는 문제
`SpawnEnemies()` 루프가 스폰마다(마지막 포함) `WaitForSeconds(spawnInterval)`을 거치는 구조이므로, `IsSpawning = false` 전환 지점을 코루틴이 완전히 끝난 뒤로 두면 마지막 몬스터가 실제로 등장한 시점보다 `spawnInterval`만큼 늦게 마커가 사라진다. `spawnInterval`이 클수록 체감 지연이 커지므로, §2에서 명시한 대로 마지막 스폰 직후(대기 전)에 상태를 false로 전환해야 한다.
