# Issue #316 — 타워 재배치 (이미 배치된 타워가 있을 때 생성 버튼 클릭 시 이동 모드로 전환)

## 1. 시스템 구조

```
[유닛 버튼 클릭 — BuildModeToggleButton.Toggle()]
    └─ InputManager.SetBuildMode(Tower)
           └─ TowerPlacer.EnterPlacementMode()
                  ├─ MapTileSystem에 이미 배치된 타워가 있는가?
                  │      ├─ 있음 → MapTileSystem.HasVacantBuildableTile(원래 좌표) 체크
                  │      │      ├─ 옮길 빈 Buildable 타일이 하나도 없음
                  │      │      │      └─ 이동 모드 진입 취소 (SetBuildMode(Rift)로 즉시 복귀,
                  │      │      │         ghost/픽업 없이 종료)
                  │      │      └─ 있음 → 기존 Tower 인스턴스를 "이동 모드" 대상으로 픽업
                  │      │                (SetGhostVisual(true)로 비활성화, 신규 Instantiate 없음)
                  │      └─ 없음 → 기존과 동일하게 신규 ghost Instantiate (issue #314)
                  └─ 공통: _ghost / _ghostRenderers 참조 설정, IsPlacingTower = true

[매 프레임 Update — TowerPlacer] (issue #314와 동일 로직 재사용)
    └─ 마우스 좌표 → coord 변환 → ghost 위치 이동
           └─ CanPlaceHere(coord) 체크
                  ├─ 이동 모드 && coord == 원래 좌표 → 항상 유효(제자리 복귀 취급)
                  ├─ MapTileSystem.CanPlaceTower(coord) → true → 초록색
                  └─ false → 빨간색

[좌클릭 — InputManager.HandleClick → TowerPlacer.TryPlace(coord)]
    ├─ 신규 생성 모드 → 기존 로직 그대로 (Instantiate + 큐브 소모)
    └─ 이동 모드
           ├─ coord가 유효(Buildable && 비어있음 or 원래 좌표)
           │      └─ MapTileSystem.RemoveTower(원래 좌표)
           │             → tower.MoveTo(coord) (TileCoord + transform 갱신, 재등록 없음)
           │             → MapTileSystem.PlaceTower(coord, tower)
           │             → tower.SetGhostVisual(false) (여기서 즉시 복귀 — Exit에서 처리 안 함)
           │             → _isMoving/_movingTower/_ghost 등 이동 상태 즉시 초기화 (성공 확정)
           │             → 큐브 소모 없음 → true 반환
           └─ 무효 → 이동 상태를 그대로 둔 채 false 반환 (아직 "취소되지 않은 진행 중" 상태 유지)
    └─ 이후 ExitPlacementMode() 호출 (기존 흐름 유지 — TryPlace 성공/실패와 무관하게 항상 호출됨)

[우클릭 / ESC / TryPlace 이후 — TowerPlacer.ExitPlacementMode()]
    ├─ 신규 생성 모드 → ghost Destroy (기존과 동일)
    ├─ 이동 모드였지만 TryPlace 성공으로 이미 상태가 초기화됨(_isMoving=false, _ghost=null)
    │      → 이 함수는 아무 것도 되돌리지 않고 IsPlacingTower=false / SetBuildMode(Rift)만 수행
    └─ 이동 모드가 아직 진행 중(_isMoving=true — 취소 또는 무효 클릭)
           → tower.MoveTo(원래 좌표)로 원위치 복귀 + SetGhostVisual(false)
             (MapTileSystem 등록은 픽업 시점부터 건드리지 않았으므로 별도 복구 불필요)
```

> ⚠️ Codex 리뷰 반영: `InputManager.HandleClick`이 `TryPlace(c)` 이후 성공 여부와 무관하게
> `ExitPlacementMode()`를 항상 호출하기 때문에, 이동 성공 케이스에서 `ExitPlacementMode()`가
> 다시 원좌표로 되돌리면 `MapTileSystem`(새 좌표로 갱신됨)과 실제 타워 위치(원좌표로 되돌아감)가
> 어긋난다. 따라서 **이동 성공은 `TryPlace()` 안에서 즉시 상태를 종료**시키고,
> `ExitPlacementMode()`는 오직 "아직 진행 중인 이동(취소/무효 클릭)"만 되돌리도록 분리한다.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
  - `MoveTo(Vector2Int coord)` 메서드 추가 — `TileCoord` 갱신 + `transform.position` 갱신만 수행.
    `Place(coord)`와 달리 `ItemSystem.RegisterTower` / `OnTowerPlaced` 이벤트는 재호출하지 않음
    (이미 등록된 타워를 다시 등록하는 부작용 방지).
  - `SetGhostVisual(bool active)` 메서드 추가 — `InitAsGhost()`의 비활성화 로직(enabled=false,
    Collider2D 비활성화)을 재사용 가능한 형태로 분리. 기존 타워를 픽업할 때 `IsGhost` 플래그는
    건드리지 않고(진짜 타워이므로 OnDestroy 시 MapTileSystem 정리 로직이 계속 정상 동작해야 함)
    시각/충돌만 토글.

- `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`
  - `EnterPlacementMode()` — `MapTileSystem.Instance.GetPlacedTower()` (신규) 조회 후 분기.
    있으면 먼저 `MapTileSystem.Instance.HasVacantBuildableTile(existing.TileCoord)`로 옮길 곳이
    있는지 확인. 없으면 픽업하지 않고 `InputManager.Instance.SetBuildMode(BuildMode.Rift)`를 호출해
    이동 모드 진입 자체를 취소(버튼을 눌러도 아무 일도 일어나지 않은 것처럼 즉시 원상 복귀).
    있으면 `_isMoving = true`, `_movingTower`, `_moveOriginCoord` 저장 + `SetGhostVisual(true)`.
    타워가 없으면 기존처럼 `Instantiate(towerPrefab)` + `InitAsGhost()`.
  - `Update()` — 유효성 체크에 "이동 모드 && 원래 좌표" 예외 추가.
  - `TryPlace(coord)` — 이동 모드 분기 추가 (큐브 미소모, `RemoveTower` + `MoveTo` + `PlaceTower`).
    **성공 시 그 자리에서 `SetGhostVisual(false)` 호출 + `_isMoving=false`/`_movingTower=null`/
    `_ghost=null` 등 이동 상태를 즉시 초기화**하여, 뒤이어 호출되는 `ExitPlacementMode()`가
    이미 확정된 이동을 다시 원좌표로 되돌리지 않도록 한다. 실패 시에는 상태를 그대로 두어
    `ExitPlacementMode()`의 되돌리기 로직이 정상 작동하게 한다.
  - `ExitPlacementMode()` — `_isMoving == true`(아직 진행 중인 이동)일 때만 `Destroy(_ghost)` 대신
    `tower.MoveTo(원래 좌표)` + `SetGhostVisual(false)`로 복귀. `_isMoving == false`면(생성 모드
    이거나, 이동이 이미 `TryPlace()`에서 성공 처리된 경우) 기존 생성 모드 정리 로직만 수행.
    마지막에 이동/생성 모드 공통 상태(`_isMoving`, `_movingTower`, `_moveOriginCoord`) 초기화.

- `MakeDefence/Assets/Scripts/Systems/MapTileSystem.cs`
  - `GetPlacedTower()` 추가 — `_placedTowers`에 항목이 있으면 첫 번째(유일한) `Tower` 반환, 없으면 null.
    (현재 게임 설계상 타워는 항상 최대 1개이므로 선택 UI 없이 단순 조회로 충분)
  - `HasVacantBuildableTile(Vector2Int excludeCoord)` 추가 — `buildableTilemap.cellBounds`를
    순회하며 `excludeCoord`를 제외하고 `GetTileType == Buildable && !_placedTowers.ContainsKey
    && !_placedRifts.ContainsKey`인 셀이 하나라도 있으면 true. 이동 모드 진입 가능 여부 판단에 사용.

## 3. 신규 클래스 / 파일

없음 (기존 파일 수정만)

## 4. 테스트 계획

- [ ] 타워가 없는 상태에서 유닛 버튼 클릭 → 기존과 동일하게 신규 ghost 생성/배치 동작 (회귀 없음)
- [ ] 타워가 이미 배치된 상태에서 유닛 버튼 클릭 → 새 ghost가 생기지 않고 기존 타워가 반투명 상태로 커서를 따라다님
- [ ] 맵의 모든 Buildable 타일이 (타워 자기 자신 좌표 제외하고) 다른 타워/균열 생성기로 이미 채워진 상태에서 유닛 버튼 클릭 → 이동 모드에 진입하지 않고 즉시 Rift 모드로 복귀 (버튼 라벨도 원래대로)
- [ ] 이동 모드에서 원래 좌표 위 → ghost 초록색(유효) 유지
- [ ] 이동 모드에서 다른 Buildable 빈 타일 위 → 초록색, 좌클릭 시 그 위치로 이동 확정 (원래 좌표는 비워짐)
- [ ] **이동 확정 직후 타워 위치가 원좌표로 되돌아가지 않는지 확인** (`ExitPlacementMode()`가 뒤이어
  호출돼도 새 좌표에 그대로 남아있어야 함 — `Tower.transform.position`/`TileCoord`와
  `MapTileSystem`의 등록 좌표가 일치하는지 확인)
- [ ] 이동 모드에서 Path/이미 다른 오브젝트 있는 타일 위 → 빨간색, 좌클릭해도 이동 안 됨
- [ ] 이동 확정 시 큐브가 소모되지 않는지 확인
- [ ] 이동 모드 중 ESC/우클릭 취소 → 타워가 원래 좌표/위치로 정확히 복귀, 공격 동작 재개
- [ ] 이동 후에도 타워의 스킬/보조옵션/아이템 슬롯 데이터가 그대로 유지되는지 확인 (인스턴스 재사용이므로 유지되어야 정상)
- [ ] 이동 후 Rift 모드로 전환 시 정상적으로 이동 모드가 취소되는지 (기존 Rift 전환 취소 로직과 충돌 없는지)

## 5. 위험 요소

- **이동 비용 정책 미확정**: 이번 플랜은 "이동 시 큐브 비용 없음"을 기본값으로 가정했다. 리소스
  밸런스상 이동에도 비용을 물릴지는 리뷰 시 확인 필요.
- **"타워는 항상 최대 1개" 전제**: `MapTileSystem`의 `_placedTowers`는 여러 타워를 지원하는
  구조(Dictionary)라 향후 다중 타워 설계로 바뀌면 `GetPlacedTower()`의 "첫 번째 항목 반환" 방식이
  깨진다. 현재는 유일하게 존재한다는 게임 설계 전제 하에 단순 구현.
- **`Tower.MoveTo` vs `Place` 부작용 분리**: `Place()`가 호출하는 `ItemSystem.RegisterTower` /
  `OnTowerPlaced` 이벤트를 이동 시 재발화하지 않는 것이 맞는지 확인 필요 — 만약 UI 등이
  `OnTowerPlaced`를 "새로 생김" 신호로 쓰고 있다면 이동 시에도 위치 갱신 알림이 필요할 수 있음
  (현재 코드 조사 결과 별도 `OnTowerMoved` 이벤트를 구독하는 곳은 없어 보이나, 구현 단계에서 재확인).
- **이동 중 InputManager.HandleClick의 무효 클릭 처리**: 기존 코드는 `TryPlace` 실패 여부와
  무관하게 클릭 시 즉시 `ExitPlacementMode()`를 호출한다(issue #314부터 존재하는 기존 동작).
  즉 빨간 타일을 클릭하면 이동이 실패하며 그대로 취소(원위치 복귀)된다 — 재시도를 위해 다시
  버튼을 눌러야 하는 기존 UX 그대로 유지.
- **(Codex 리뷰로 발견) 이동 성공 직후 되돌림 버그**: `TryPlace()`가 성공해도 `HandleClick`이
  곧바로 `ExitPlacementMode()`를 호출하므로, 이동 상태 초기화를 `TryPlace()` 성공 분기 안에서
  즉시 하지 않으면 `ExitPlacementMode()`가 이미 확정된 이동을 다시 원좌표로 되돌려
  `MapTileSystem` 등록 좌표와 실제 위치가 어긋난다. 구현 시 반드시 성공 경로에서 `_isMoving`을
  먼저 false로 내린 뒤 리턴하도록 순서를 지켜야 한다 (위 시스템 구조/수정 파일 섹션에 반영함).
- **옮길 곳 없을 때 무반응 취소에 대한 피드백 부재**: `HasVacantBuildableTile`이 false면 버튼을
  눌러도 겉보기엔 아무 일도 일어나지 않는다(라벨은 Tower→Rift로 바뀌었다가 즉시 Rift로 되돌아옴).
  플레이어에게 "옮길 곳이 없습니다" 같은 명시적 피드백(토스트/로그)을 줄지는 이번 플랜 범위 밖으로
  두고 `Debug.Log` 수준으로만 남긴다 — 필요 시 별도 UX 이슈로 분리.
