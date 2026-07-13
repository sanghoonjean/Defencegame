# Issue #383 — Unit_Panel: 타워 클릭 없이 최근 타워 정보 표시

## 1. 시스템 구조

Unit_Panel 의 가시성/내용은 전부 `InventorySystem.OnTowerSelected` 이벤트로 구동된다.

```text
InputManager.HandleClick (좌클릭 단일 진입점)
 ├─ 타워 hit        → InventorySystem.SelectTower(tower) → OnTowerSelected(tower)
 └─ 빈 칸 (배치 실패) → InventorySystem.Deselect()        → OnTowerSelected(null)  ← 제거 대상

TowerPlacer
 ├─ PlaceTower(coord) : 신규 배치. 현재는 배치 후 선택하지 않음        ← 자동 선택 추가
 └─ TryMove(coord)    : 기존 타워 이동. 현재는 이동 후 선택 변경 없음  ← 자동 선택 추가

OnTowerSelected 구독자 (전부 UI, 월드 사이드 이펙트 없음)
 ├─ UnitPanelController  : CanvasGroup alpha 로 패널 표시/숨김
 ├─ JobClassDisplayUI / SkillSlotUI / OwnedSkillSlotUI / SupportSlotUI : 내용 갱신
 └─ (UnitPanelController.OnEnable 이 SelectedTower 를 재조회하므로 탭 재오픈 시에도 동기화됨)
```

변경 후 선택(= 최근 타워)이 바뀌는 경로:

- 다른 타워 클릭 (`SelectTower`)
- 신규 타워 배치 (`PlaceTower` → `SelectTower`)
- 타워 이동 완료 (`TryMove` → `SelectTower`)
- 선택된 타워 삭제 (`DeleteTower` → `Deselect`, 패널 숨김 — 기존 유지)

빈 칸 클릭으로는 선택이 해제되지 않으므로 Unit_Panel 은 항상 최근 타워 정보를 유지한다.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/InputManager.cs`
  - `HandleClick()` : 빈 칸 클릭(배치 실패) 시 `InventorySystem.Deselect()` 호출 제거
- `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`
  - `PlaceTower()` : `_pendingOnPlaced` 콜백(직업 설정) 실행 **후** `SelectTower(tower)` 호출
  - `TryMove()` : 이동 성공 시 `ClearMoveState()` **전에** `SelectTower(_movingTower)` 호출

## 3. 신규 클래스 / 파일

없음 — 기존 이벤트 흐름만 조정한다.

## 4. 테스트 계획

플레이 모드에서 확인:

- [ ] 타워 배치 직후 클릭 없이 Unit_Panel 에 해당 타워 정보 표시 (직업 스탯 반영 상태)
- [ ] 빈 땅 클릭 후에도 패널이 사라지지 않고 마지막 타워 정보 유지
- [ ] 타워 이동 완료 시 이동한 타워가 패널에 표시
- [ ] 다른 타워 클릭 시 패널 내용이 해당 타워로 전환
- [ ] 선택된 타워 삭제 시 패널 숨김 (기존 동작 유지)
- [ ] 게임 시작 직후(타워 0개) 패널 비표시
- [ ] 컴파일 에러/신규 콘솔 에러 없음

## 5. 위험 요소

- `SelectTower` 호출이 늘어나므로 `OnTowerSelected` 구독 UI 들이 추가로 Refresh 된다
  → 구독자가 전부 경량 UI 갱신이라 성능 영향 미미
- `PlaceTower` 에서 직업 설정 콜백(`_pendingOnPlaced`) **이후에** 선택해야
  패널에 직업 보정 스탯이 반영된 상태로 표시된다 (순서 주의)
- 빈 칸 클릭으로 패널을 닫는 기존 UX 가 사라짐 — 패널을 닫으려면 탭 토글 사용
  (이슈 요구사항에 따른 의도된 변경)
- 파괴된 타워가 선택으로 남는 경로는 없음 — 삭제는 `DeleteTower` → `Deselect` 로 정리됨
