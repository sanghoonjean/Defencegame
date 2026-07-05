# Issue #347 — 인게임 HUD: 남은 몬스터 수(EnemyPanel) + 플레이어 체력(HPPanel) 패널 추가

## 1. 시스템 구조

### 현재 상태
- 플레이어 체력 로직은 이미 완성되어 있음: `Enemy.TakeDamage`가 기지 도달 시 `PlayerSystem.Instance.TakeDamage(_playerDamage)` 호출 → `PlayerSystem`이 `CurrentHp` 갱신 후 `OnHpChanged`(int) invoke, 0 이하가 되면 `OnPlayerDied` invoke → `WaveSystem.HandlePlayerDied`가 구독해 `GameStateSystem.SetState(GameState.Defeat)`까지 이어짐. **이 흐름에는 손대지 않는다.**
- 몬스터 카운트는 `WaveSystem` 내부 `_aliveCount`(private)로만 관리되고, 웨이브 시작 시 전체 수를 담아 `OnWaveStarted(int stage)`를 invoke하지만 stage 값만 전달할 뿐 스폰될 총 마리 수는 외부에 공개하지 않는다. 남은 수가 줄어들 때(`HandleEnemyRemoved`)도 이를 구독자에게 알리는 이벤트가 없다.
- 즉 이번 작업은 두 갈래:
  1. **HPPanel**: 순수 UI — 기존 `PlayerSystem.OnHpChanged`/`MaxHp`/`CurrentHp`를 구독해 텍스트로 표시만 하면 됨.
  2. **EnemyPanel**: UI + `WaveSystem`에 "전체 스폰 수"와 "남은 수 변경" 정보를 공개하는 작은 확장 필요.

### 변경 후 데이터 흐름

```
[HP]
Enemy.TakeDamage(기지 도달) → PlayerSystem.TakeDamage → OnHpChanged(int currentHp)
                                                              └─ HPPanelController.OnHpChanged(hp) → $"{hp} / {MaxHp}" 텍스트 갱신

[Enemy Count]
WaveSystem.StartWave/StartRiftWave → _aliveCount 확정 직후 OnAliveCountChanged?.Invoke(_aliveCount, total) 최초 1회
WaveSystem.HandleEnemyRemoved(처치/기지도달) → _aliveCount-- 후 OnAliveCountChanged?.Invoke(_aliveCount, total)
                                                              └─ EnemyPanelController.OnAliveCountChanged(alive, total) → $"{alive} / {total}" 텍스트 갱신
```

- 신규 이벤트는 `OnWaveStarted`처럼 정적 이벤트로 추가하고, WaveSystem 내부 카운트가 바뀌는 두 지점(웨이브 시작, `HandleEnemyRemoved`)에서만 invoke한다. 별도 폴링 없이 이벤트 기반으로 UI를 갱신한다 (기존 `UnitPanelController`/`SettingsPanelController` 패턴과 동일하게 `OnEnable`에서 구독 + 현재 상태로 즉시 1회 갱신, `OnDisable`에서 해제).
- 웨이브가 진행 중이 아닐 때(`GameState.WaveResult` 등) EnemyPanel 표시 값은 마지막 웨이브의 최종 값(보통 0 / total)을 그대로 유지 — 별도 초기화 불필요.

### 주의: 기지 도달 + 사망이 같은 프레임에서 겹치는 경우 (Codex 리뷰 지적)

`Enemy.ReachBase()`는 `PlayerSystem.Instance.TakeDamage(_playerDamage)`를 먼저 호출한 뒤 `OnEnemyReachedBase`를 invoke한다. `PlayerSystem.TakeDamage`는 HP가 0 이하가 되면 동기적으로 `OnPlayerDied`를 invoke하고, 이를 구독하는 `WaveSystem.HandlePlayerDied`가 `StopWave()`를 호출해 `IsWaveActive`를 즉시 `false`로 바꾼다. 따라서 마지막 기지 도달이 곧 플레이어 사망인 경우, 뒤이어 `OnEnemyReachedBase`가 invoke될 때 `HandleEnemyRemoved`의 기존 가드(`if (!IsWaveActive) return;`)에 걸려 그 마지막 몬스터의 카운트 감소/알림이 **누락**된다 — EnemyPanel이 실제로는 전멸했는데 1마리가 남은 것처럼 표시된 채 멈춘다.

해결: `HandleEnemyRemoved`의 가드 위치를 바꿔 "카운트 갱신"과 "웨이브 종료 트리거"를 분리한다.
```csharp
private void HandleEnemyRemoved(Enemy enemy)
{
    if (_aliveCount <= 0) return; // 이미 0이면 더 이상 처리할 것 없음(중복 호출 방어)
    _aliveCount--;

    int routeIndex = enemy.RouteIndex;
    if (_aliveCountByRoute != null && routeIndex >= 0 && routeIndex < _aliveCountByRoute.Length)
    {
        _aliveCountByRoute[routeIndex]--;
        if (_aliveCountByRoute[routeIndex] <= 0)
            OnRouteCleared?.Invoke(routeIndex);
    }

    OnAliveCountChanged?.Invoke(_aliveCount, _totalCount);

    if (IsWaveActive && _aliveCount <= 0)
        EndWave();
}
```
- 카운트 감소/알림은 `IsWaveActive`와 무관하게 항상 수행하되(`_aliveCount <= 0`으로만 중복 방어), `EndWave()` 호출만 `IsWaveActive` 조건으로 계속 가드해 이미 `StopWave()`로 끝난 웨이브에서 `EndWave()`가 다시 호출되지 않도록 한다.
- `OnRouteCleared`가 웨이브 종료 후에도 호출될 수 있게 되지만, 해당 route의 경로 마커를 숨기는 용도(`MonsterPathVisualizer`)라 종료 후 호출돼도 부작용 없음.

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
- 신규 정적 이벤트 `public static event Action<int, int> OnAliveCountChanged;` (alive, total) 추가.
- `_totalCount` private 필드 추가 (현재 웨이브에 스폰 예정인 총 마리 수 — `_aliveCount`는 처치되며 줄어들지만 total은 고정값으로 별도 보관 필요).
- `StartWave()`/`StartRiftWave()`에서 `_aliveCount = spawnList.Count;` 직후 `_totalCount = _aliveCount;`를 설정하고, 기존 `OnWaveStarted?.Invoke(...)` 가드(`if (IsWaveActive)`) 바로 아래에 `OnAliveCountChanged?.Invoke(_aliveCount, _totalCount);`를 함께 invoke (같은 가드 조건 재사용).
- `HandleEnemyRemoved(Enemy enemy)`: 기존 최상단 가드 `if (!IsWaveActive) return;`를 `if (_aliveCount <= 0) return;`로 교체하고, `_aliveCount--` 및 route 처리 뒤에 `OnAliveCountChanged?.Invoke(_aliveCount, _totalCount);`를 추가. 웨이브 종료 트리거(`EndWave()`)만 `if (IsWaveActive && _aliveCount <= 0)`로 별도 가드 (위 "주의" 항목 참고 — 기지 도달이 곧 사망인 경우에도 카운트가 정확히 반영되도록).

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/UI/EnemyPanelController.cs`
- `[SerializeField] private TMPro.TMP_Text countText;`
- `OnEnable`: `WaveSystem.OnAliveCountChanged += OnAliveCountChanged;` 구독. `WaveSystem.Instance`가 있으면 현재 상태로 초기 텍스트 세팅(없으면 웨이브 시작 전까지 Inspector 기본 텍스트 유지).
- `OnDisable`: 구독 해제.
- `OnAliveCountChanged(int alive, int total)`: `countText.text = $"{alive} / {total}";`

### `MakeDefence/Assets/Scripts/UI/HPPanelController.cs`
- `[SerializeField] private TMPro.TMP_Text hpText;`
- `OnEnable`: `PlayerSystem.OnHpChanged += OnHpChanged;` 구독. `PlayerSystem.Instance`가 있으면 `OnHpChanged(PlayerSystem.Instance.CurrentHp)`로 즉시 1회 갱신.
- `OnDisable`: 구독 해제.
- `OnHpChanged(int hp)`: `hpText.text = $"{hp} / {PlayerSystem.Instance.MaxHp}";`

### `MakeDefence/Assets/Scenes/SampleScene.unity`
- Canvas 하위에 `EnemyPanel`, `HPPanel` GameObject(각각 TMP 텍스트 자식 포함) 추가하고 위 컨트롤러 컴포넌트를 붙여 텍스트 필드를 연결한다.
- 씬 편집은 UnityMCP(`manage_gameobject`/`manage_ui`)로 진행하고 `.unity` YAML 직접 편집은 하지 않는다 (프로젝트 가이드 — [[feedback_unity_asset_edits]]).
- 참고: 현재 워킹트리에 이미 커밋되지 않은 `Panel`/`Lower` GameObject 추가분이 있음 — 이번 작업과 무관하면 건드리지 않고, 관련이 있다면(예: 사용자가 미리 만들어둔 HUD 컨테이너) 확인 후 재사용.

## 4. 테스트 계획

### 수동 (Unity Editor, Play Mode)
- [ ] 웨이브 시작 전: EnemyPanel이 빈 값/기본값이어도 에러 없이 표시되는지 확인.
- [ ] 웨이브 시작 직후: EnemyPanel이 `0 / {total}`이 아니라 `{total} / {total}`(스폰 완료 전이라도 전체 대비 아직 안 줄어든 상태)로 표시되는지 확인.
- [ ] 몬스터 처치 시마다 남은 수가 1씩 감소하는지, 기지에 도달해 사라질 때도 동일하게 감소하는지 확인.
- [ ] 웨이브 전체 클리어 시 EnemyPanel이 `0 / {total}`로 끝나는지 확인.
- [ ] 몬스터가 기지에 도달할 때마다 HPPanel 수치가 감소하는지 확인 (`_playerDamage`만큼).
- [ ] 체력이 0이 되어 `Defeat` 상태로 전환될 때 HPPanel이 `0 / 100`으로 표시되고 크래시 없는지 확인.
- [ ] 다음 웨이브 시작 시 `PlayerSystem.ResetHp()`로 체력이 회복되고 HPPanel도 즉시 갱신되는지 확인.
- [ ] 균열 웨이브(`StartRiftWave`, `ExtraCount` 포함)에서도 total 수치가 추가 스폰분까지 정확히 반영되는지 확인.
- [ ] (회귀 확인) 플레이어 체력을 낮춰둔 뒤 마지막 남은 몬스터가 기지에 도달해 체력이 0이 되는 상황을 재현 — `Defeat` 전환과 동시에 EnemyPanel도 `0 / {total}`로 정확히 갱신되는지, `EndWave()`가 중복 호출되어 에러가 나지 않는지 확인 (Codex 리뷰 지적 사항).

## 5. 위험 요소

- `HandleEnemyRemoved`의 가드를 `IsWaveActive` → `_aliveCount <= 0`로 바꾸는 부분은 기존 로직에 실질적인 동작 변경이 있는 지점(Codex 리뷰로 발견) — 반드시 "기지 도달이 곧 마지막 HP"인 케이스를 테스트 계획에 포함해 회귀 여부를 확인한다.
- `_totalCount`를 `_aliveCount`와 별도로 두지 않으면(`_aliveCount`만 재사용) 처치될 때마다 total도 같이 줄어드는 버그가 나므로 반드시 별도 필드로 분리.
- Canvas/패널 배치(화면 어디에 둘지, 스타일)는 아직 미확정 — 우선 기능 동작을 검증하고 위치/스타일은 아트 패스에서 조정 가능하도록 Inspector 노출 필드로 둔다.
- 씬에 이미 존재하는 커밋되지 않은 `Panel`/`Lower` 변경분과 충돌하지 않도록, 작업 시작 전 UnityMCP로 현재 하이어라키를 먼저 확인한다.
