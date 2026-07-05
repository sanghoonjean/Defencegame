# Issue #343 — 유닛 스폰 버튼별로 서로 다른 유닛 생성 지원

## 1. 시스템 구조

### 현재 흐름 (버그)

```
Unitbtn / Unitbtn1 ~ 4  (전부 BuildModeToggleButton)
 └─ Toggle() → InputManager.SetBuildMode(Tower)
                 └─ (SetBuildMode 내부에서) TowerPlacer.EnterPlacementMode()  ← 인자 없음, 프리팹 고정
                       └─ MapTileSystem.GetPlacedTower() 로 "맵에 타워가 하나라도 있는가"만 확인
                             ├─ 있으면 → 무조건 이동 모드(_isMoving=true, 그 타워를 집어듦)
                             └─ 없으면 → TowerPlacer.towerPrefab 으로 신규 배치
```

문제:
- 버튼 5개가 전부 동일한 `BuildModeToggleButton` + 동일한 `TowerPlacer.towerPrefab` 하나만 참조 → 버튼별 유닛 구분이 아예 없음.
- `MapTileSystem.GetPlacedTower()`는 "설계상 항상 최대 1개"라는 전제로 좌표 무관하게 아무 타워나 반환 → 어떤 버튼을 누르든 "이미 뭔가 있으면 이동" 판정이 전역으로 적용됨.

### 변경 후 흐름

```
Unitbtn{N}  (신규 UnitSpawnButton, 각자 고유 unitPrefab 보유)
 └─ OnClick()
     ├─ 이미 다른 배치/이동이 진행 중이면 먼저 TowerPlacer.ExitPlacementMode() 로 취소
     ├─ 이 버튼이 소유한 인스턴스(_placedTower)가 없으면
     │     → InputManager.SetBuildMode(Tower)
     │     → TowerPlacer.EnterPlacementMode(unitPrefab, OnUnitPlaced)   ← 신규 오버로드, 항상 "새로 배치"
     │           OnUnitPlaced(tower): _placedTower = tower; tower.OnRemoved += 초기화 콜백
     └─ 이미 _placedTower 가 있으면
           → InputManager.SetBuildMode(Tower)
           → TowerPlacer.EnterMoveMode(_placedTower)                   ← 신규 public 메서드, 기존 이동 로직 재사용
```

- `MapTileSystem._placedTowers`(좌표 → Tower 딕셔너리)는 이미 여러 좌표를 동시에 담을 수 있는 구조라 구조 변경 불필요. "전역 최대 1개" 전제였던 `GetPlacedTower()`만 제거한다.
- "이동 모드"는 없어지는 게 아니라, 트리거를 "전역 아무 타워"에서 "그 버튼이 직접 생성한 타워"로 좁힌다. 버튼 재클릭 시 그 유닛만 이동 모드로 들어가고 다른 버튼의 유닛은 영향받지 않는다.
- `InputManager.SetBuildMode()`가 내부에서 `TowerPlacer.Enter/ExitPlacementMode()`를 자동 호출하던 것은 제거한다 — 어떤 프리팹으로 배치할지/어떤 타워를 이동할지는 버튼(호출부)만 아는 정보라 자동 호출로는 표현할 수 없기 때문. 대신 모든 호출부가 명시적으로 Enter/Exit를 호출한다 (기존 `BuildModeToggleButton.Toggle()`도 원래 이미 명시적으로 호출하던 패턴 — 지금은 `SetBuildMode`와 이중 호출이라 사실상 두 번째 호출이 가드에 막혀 no-op이었음).
- 유닛 삭제(`TowerDeleteConfirmPopup` → `InventorySystem.DeleteTower`) 시 `Tower.OnDestroy()`에서 신규 `OnRemoved` 이벤트를 invoke → 버튼의 `_placedTower`가 자동으로 `null`로 초기화되어, 삭제 후 재클릭하면 다시 신규 배치 모드로 들어간다.

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`
- `[SerializeField] private Tower towerPrefab;`은 "기본/디버그용" 프리팹으로 유지 (TestRunner의 B키 디버그 토글이 계속 동작하도록).
- `EnterPlacementMode()` (인자 없음, 기존 동작 유지 — 기본 프리팹으로 신규 배치, 디버그 전용)와 `EnterPlacementMode(Tower prefab, Action<Tower> onPlaced = null)` (신규 오버로드) 두 개로 분리.
  - 신규 오버로드는 `MapTileSystem.GetPlacedTower()` 호출/이동 분기를 하지 않는다 — 항상 `prefab`으로 ghost를 만들어 신규 배치 모드로만 진입한다.
  - `_pendingOnPlaced` 필드를 추가해 `TryPlace()`가 신규 배치를 성공시켰을 때 호출한다.
- 신규 `public void EnterMoveMode(Tower existingTower)` 추가 — 기존 `EnterPlacementMode()`의 "existing != null" 분기(옮길 곳 없으면 취소, ghost 비주얼 전환 등)를 그대로 옮긴다. `MapTileSystem.GetPlacedTower()` 대신 인자로 받은 타워를 사용.
- `TryPlace(coord)`: 이동 분기(`_isMoving`)는 변경 없음. 신규 배치 분기에서 `PlaceTower(coord)`가 만든 `Tower`를 `_pendingOnPlaced?.Invoke(tower)`로 콜백.
- `ExitPlacementMode()`: `_pendingOnPlaced`도 함께 초기화(`ClearMoveState()` 또는 별도 클리어)해 다음 배치에 이전 콜백이 남지 않도록 한다.

### `MakeDefence/Assets/Scripts/Systems/MapTileSystem.cs`
- `GetPlacedTower()` 삭제 (호출부가 `TowerPlacer.cs` 한 곳뿐이며, 그 호출부도 이번에 제거됨 — grep으로 다른 참조 없음 확인 완료).
- `HasVacantBuildableTile(excludeCoord)`는 `EnterMoveMode`에서 계속 사용하므로 유지.

### `MakeDefence/Assets/Scripts/Systems/InputManager.cs`
- `SetBuildMode(mode)`에서 `TowerPlacer.Instance?.EnterPlacementMode()` / `ExitPlacementMode()` 자동 호출 제거. `CurrentBuildMode` 갱신 + `OnBuildModeChanged` invoke만 남긴다.

### `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
- `public event Action OnRemoved;` 추가.
- `OnDestroy()`: 기존 로직(ghost면 무시, `ItemSystem.UnregisterTower` / `MapTileSystem.RemoveTower`) 끝에 `OnRemoved?.Invoke();` 추가 (ghost 타워는 기존과 동일하게 조기 return이라 invoke 안 됨).

### `MakeDefence/Assets/Scripts/UI/BuildModeToggleButton.cs`
- 삭제하고 아래 신규 `UnitSpawnButton.cs`로 대체 (역할이 완전히 흡수됨). Scene에서 5개 버튼 모두 컴포넌트를 교체해야 함.

### `MakeDefence/Assets/Scripts/TestRunner.cs`
- B키 디버그 토글(`Input.GetKeyDown(KeyCode.B)` 블록, 56~64행): `SetBuildMode(next)` 자동 호출 제거에 맞춰, `next == BuildMode.Tower`일 때 `TowerPlacer.Instance?.EnterPlacementMode()`(기본 프리팹, 인자 없는 오버로드)를, `next == BuildMode.None`일 때 `TowerPlacer.Instance?.ExitPlacementMode()`를 명시적으로 호출하도록 추가.

### `MakeDefence/Assets/Scenes/SampleScene.unity`
- `Unitbtn`, `Unitbtn1`, `Unitbtn2`, `Unitbtn3`, `Unitbtn4` 5개 GameObject에서 `BuildModeToggleButton` 컴포넌트를 `UnitSpawnButton`으로 교체하고, 각 버튼의 `unitPrefab` 필드에 서로 다른 Tower 프리팹을 연결한다.

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/UI/UnitSpawnButton.cs`
- `BuildModeToggleButton` 대체. `[SerializeField] private Tower unitPrefab;` 보유.
- 이 버튼이 신규 배치한 타워를 추적하는 `private Tower _placedTower;`.
- 클릭 시: 진행 중인 배치/이동이 있으면 먼저 취소 → `_placedTower`가 없으면 `EnterPlacementMode(unitPrefab, OnUnitPlaced)`로 신규 배치, 있으면 `EnterMoveMode(_placedTower)`로 이동.
- `OnUnitPlaced(Tower tower)`: `_placedTower = tower`, `tower.OnRemoved += HandleTowerRemoved` 구독.
- `HandleTowerRemoved()`: 구독 해제 후 `_placedTower = null`.

### 유닛 프리팹 4~5종 (`MakeDefence/Assets/Perfab/` 아래)
- 기존 `Tower.prefab`을 복제해 버튼 수만큼(`Unitbtn`~`Unitbtn4`) 만든다. 스탯(`baseAttackDamage`/`baseAttackSpeed`/`baseAttackRange`)만 우선 차등 적용하고, 스프라이트/애니메이터는 아트 리소스가 준비되기 전까지 기존 것을 재사용한다 (밸런스/아트는 별도 후속 이슈로 분리 가능).

## 4. 테스트 계획

### 수동 (Unity Editor)
- [ ] `Unitbtn` 클릭 → ghost가 마우스를 따라다니는지, 좌클릭으로 유닛1이 배치되는지 확인.
- [ ] 유닛1이 배치된 상태에서 `Unitbtn1` 클릭 → 유닛1은 그대로 두고 새 ghost(유닛2)가 생성되는지 확인 (기존 버그였던 "유닛1이 이동 모드로 빠지는" 현상이 재현되지 않아야 함).
- [ ] 유닛2까지 배치 후 `Unitbtn` 재클릭 → 유닛1만 이동 모드로 들어가는지(유닛2는 그대로인지) 확인.
- [ ] 이동 모드에서 우클릭/Esc → 원위치로 복귀하는지 확인 (기존 `ExitPlacementMode` 로직 그대로).
- [ ] 유닛1을 삭제(`D`키 → 삭제 확인 팝업) 후 `Unitbtn` 재클릭 → 이동 모드가 아니라 신규 배치 모드로 들어가는지 확인.
- [ ] 배치 진행 중(ghost 따라다니는 상태)에 다른 유닛 버튼을 클릭 → 이전 ghost가 취소되고 새 버튼의 ghost로 전환되는지 확인.
- [ ] 여러 유닛이 동시에 배치된 상태에서 웨이브를 돌려 각 유닛이 정상적으로 공격하는지, 경로 재계산(`WouldSeverPath`)이 여러 타워 기준으로 정상 동작하는지 확인.
- [ ] `B`키 디버그 토글(TestRunner)이 여전히 기본 프리팹으로 배치/해제되는지 확인 (회귀 확인).

## 5. 위험 요소

- `InputManager.SetBuildMode()`의 자동 Enter/Exit 호출 제거는 이 메서드를 호출하는 모든 곳(`UnitSpawnButton`, `TestRunner` B키)이 각자 명시적으로 `TowerPlacer.Enter/ExitPlacementMode()`를 호출하도록 함께 고쳐야 누락 없이 안전 — 수정 파일 목록에 `TestRunner.cs` 포함시켜 반영함.
- `Tower.OnRemoved` 구독을 버튼이 해제하지 않고 방치하면(예: 씬 전환 없이 반복 배치/삭제) 이벤트 핸들러가 죽은 타워 참조를 계속 들고 있을 수 있음 — `HandleTowerRemoved()`에서 반드시 구독 해제 후 `_placedTower = null` 처리.
- 유닛 프리팹 4~5종의 실제 스탯/스프라이트는 미확정 — 우선 스탯 차등만으로 동작을 검증하고, 아트/밸런스는 별도 후속 작업으로 분리될 수 있음.
- 씬 파일(`SampleScene.unity`) 수정은 텍스트 편집이 아니라 UnityMCP/Editor를 통해 컴포넌트 교체 + 프리팹 연결로 진행 (YAML 직접 편집 금지 — 프로젝트 가이드).
- 버튼이 5개보다 늘어나는 경우를 대비한 별도 데이터 테이블(예: ScriptableObject 목록)은 이번 스코프에서 제외 — 현재는 프리팹을 인스펙터에서 직접 연결하는 방식으로 충분히 단순함.
