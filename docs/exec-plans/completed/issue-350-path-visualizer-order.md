# Issue #350 — 웨이브 연속 생성 중 경로 시각화가 두 번째 자동 웨이브부터 표시되지 않음

## 1. 시스템 구조

### 현재 흐름 (버그)

`WaveSystem.OnWaveEnded`(`Action<bool>`)를 구독하는 두 컴포넌트가 있다:

- `RepeatGenerateToggleButton` — `OnEnable()`에서 구독
- `MonsterPathVisualizer` — `Start()`에서 구독

Unity는 씬 로드 시 모든 오브젝트의 `OnEnable()`을 모든 오브젝트의 `Start()`보다 먼저 실행하므로, `RepeatGenerateToggleButton`의 구독이 `MonsterPathVisualizer`보다 먼저 이루어진다. C# 멀티캐스트 이벤트는 구독 순서대로 호출되므로, `OnWaveEnded` invoke 시 `RepeatGenerateToggleButton.HandleWaveEnded`가 항상 먼저 실행된다.

"웨이브 연속 생성" 토글이 켜진 상태에서 웨이브가 클리어되면:

```
WaveSystem.EndWave()
 └─ OnWaveEnded?.Invoke(true) 디스패치 시작
     ├─ [1번째 구독자] RepeatGenerateToggleButton.HandleWaveEnded(true)
     │     └─ TryConsumeNext() → WaveGeneratorSystem.OpenRift()
     │           └─ WaveSystem.StartRiftWave(mods)   ← 다음 웨이브를 동기적으로 즉시 시작
     │                 ├─ IsWaveActive = true
     │                 └─ OnWaveStarted?.Invoke(stage)
     │                       └─ MonsterPathVisualizer.HandleWaveStarted
     │                             _isWaveActive=true, _activeRoutes 재구성, 마커 생성  ← 정상
     │
     └─ [2번째 구독자] MonsterPathVisualizer.HandleWaveEnded(true)   ← 같은 디스패치가 계속 진행되어 호출됨
           _isWaveActive=false, _activeRoutes.Clear(), 마커 파괴   ← 방금 세팅한 새 웨이브 상태를 덮어씀 (버그)
```

결과: `WaveSystem.IsWaveActive`는 `true`(새 웨이브 진행 중)인데 `MonsterPathVisualizer._isWaveActive`는 `false`로 어긋난 채 고정된다. 이후 `HandlePathsChanged`도 `_isWaveActive` 가드에 걸려 아무 것도 갱신하지 않으므로, 그 다음부터는 타워를 배치/이동해도 경로 마커가 영구히 나타나지 않는다.

일반 자동 웨이브(`WaveSystem.SetAutoWave`, TestRunner 디버그 토글)는 이 문제가 없다 — `EndWave()`가 `OnWaveEnded?.Invoke(true)`를 완전히 끝낸 뒤(모든 구독자 처리 완료 후) 순차문으로 `StartWave()`를 호출하기 때문에, "클리어 처리"와 "다음 웨이브 시작"이 같은 이벤트 디스패치 안에서 겹치지 않는다. 문제는 "웨이브 연속 생성" 토글이 `OnWaveEnded`의 한 구독자(`RepeatGenerateToggleButton`) 안에서 재귀적으로 다음 웨이브를 여는 구조이기 때문에 발생한다.

Unity Play Mode에서 리플렉션으로 직접 재현 확인: 웨이브 연속 생성 시작 후 반복적으로 몬스터를 처치해 자동 전환을 유도하자, 두 번째 전환부터 `MonsterPathVisualizer._isWaveActive=False`, `_activeRoutes=[]`, `markers=0`인 채로 `WaveSystem.IsWaveActive=True`(실제로는 웨이브 진행 중)인 상태가 재현됨.

### 변경 후 동작

구독 순서에 의존하지 않도록, `MonsterPathVisualizer.HandleWaveEnded`가 무조건 상태를 지우는 대신 **실제 `WaveSystem` 상태를 다시 확인**한다: 이 핸들러가 호출된 시점에 이미 `WaveSystem.Instance.IsWaveActive`가 `true`라면(디스패치 도중 다른 구독자가 이미 다음 웨이브를 시작시킨 것) 지우지 않고 그대로 둔다 — 그 상태는 방금 `HandleWaveStarted`가 새 웨이브 기준으로 정확히 세팅한 것이므로 다시 손댈 필요가 없다.

```csharp
private void HandleWaveEnded(bool cleared)
{
    if (WaveSystem.Instance != null && WaveSystem.Instance.IsWaveActive) return;

    _isWaveActive = false;
    _activeRoutes.Clear();
    ClearMarkers();
}
```

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Systems/MonsterPathVisualizer.cs`
- `HandleWaveEnded(bool cleared)` 최상단에 `if (WaveSystem.Instance != null && WaveSystem.Instance.IsWaveActive) return;` 가드 추가.
- 다른 로직(`HandleWaveStarted`, `HandleRouteCleared`, `HandlePathsChanged`)은 변경 없음 — 이번 버그는 `HandleWaveEnded`가 이벤트 데이터(`cleared`)만 믿고 "웨이브가 끝났다"고 단정하는 데서 발생했으므로, 실제 소스(`WaveSystem.IsWaveActive`)를 다시 확인하는 것만으로 구독 순서와 무관하게 해결된다.

## 3. 신규 클래스 / 파일

없음 — 기존 파일 수정만으로 해결.

## 4. 테스트 계획

### 수동 (Unity Editor, Play Mode)
- [x] 리플렉션으로 `MonsterPathVisualizer`의 private 상태(`_isWaveActive`, `_activeRoutes`, `_markers`)를 직접 관찰하며 검증 완료 (수정 전: 재현됨 / 수정 후: 5회 이상 연속 웨이브에서 정상 유지 확인)
- [ ] 실제로 UI에서 "웨이브 연속 생성" 토글을 켜고 눈으로 경로 마커가 매 웨이브마다 계속 보이는지 확인 (자동화 검증은 완료했으나 시각적 확인은 사용자 몫으로 남겨둘 수 있음)
- [ ] 일반 자동 웨이브(TestRunner B키 등)는 기존과 동일하게 정상 동작하는지 회귀 확인 (이번 변경은 조건 추가만이라 영향 없음)
- [ ] 웨이브 연속 생성 중 인벤토리가 소진되어 `Stop()`되는 정상 종료 시나리오에서도 마커가 올바르게 사라지는지 확인 (이 경우 `WaveSystem.IsWaveActive`가 실제로 `false`이므로 가드를 통과해 정상적으로 clear됨)

## 5. 위험 요소

- 가드 조건(`WaveSystem.Instance.IsWaveActive`)이 `HandleWaveEnded` 호출 시점에 이미 `true`인 경우는 "디스패치 도중 다른 구독자가 이미 다음 웨이브를 시작시킨" 경우로 한정된다 — 이 프로젝트에서 그런 재귀 개시를 하는 곳은 `RepeatGenerateToggleButton` 하나뿐이라 현재는 안전하지만, 앞으로 `OnWaveEnded` 구독자 중 또 다른 컴포넌트가 비슷하게 동기적으로 웨이브를 재시작하는 패턴을 추가한다면 동일한 종류의 재검토가 필요함을 인지해둔다.
- 이번 수정은 `WaveSystem.cs`를 건드리지 않고 `MonsterPathVisualizer.cs` 한 곳만 수정하므로, 이슈 #347(HUD 패널) 작업과는 독립적이다.
