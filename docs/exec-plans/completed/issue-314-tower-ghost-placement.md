# Issue #314 — 유닛 버튼 클릭 → Ghost 미리보기 후 배치 시스템

## 1. 시스템 구조

```
[유닛 버튼 클릭]
    └─ BuildModeToggleButton.Toggle()
           └─ InputManager.SetBuildMode(Tower)
                  └─ TowerPlacer.EnterPlacementMode()
                         └─ ghost GameObject 생성 (반투명)

[매 프레임 Update — TowerPlacer]
    └─ 마우스 좌표 → coord 변환
           └─ MapTileSystem.CanPlaceTower(coord) 체크
                  ├─ true  → ghost 초록색 (a=0.5)
                  └─ false → ghost 빨간색 (a=0.5)

[좌클릭 — InputManager.HandleClick]
    ├─ TowerPlacer.IsPlacingTower == true
    │      └─ TryPlace(coord) 후 ExitPlacementMode()
    └─ IsPlacingTower == false → 기존 로직 유지

[우클릭 / ESC — TowerPlacer.Update]
    └─ ExitPlacementMode() → ghost 삭제
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
  - `IsGhost` bool 프로퍼티 추가
  - `InitAsGhost()` 메서드 추가 (enabled=false, Collider2D 비활성화)
  - `OnDestroy()` IsGhost 체크 추가 (ghost 파괴 시 MapTileSystem 건드리지 않음)

- `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`
  - Ghost 필드: `_ghost`, `_ghostRenderers`, `IsPlacingTower`
  - `EnterPlacementMode()` / `ExitPlacementMode()` 추가
  - `Update()` 추가: ghost 위치/색상 갱신, ESC·우클릭 취소

- `MakeDefence/Assets/Scripts/Systems/InputManager.cs`
  - `HandleClick()` 앞부분에 `TowerPlacer.IsPlacingTower` 분기 추가
  - 배치 대기 중 좌클릭 → TryPlace + ExitPlacementMode (기존 빈칸 즉시배치 로직 우선 처리)

- `MakeDefence/Assets/Scripts/UI/BuildModeToggleButton.cs`
  - Tower 모드 전환 시 `TowerPlacer.EnterPlacementMode()` 호출
  - Rift 모드 전환 시 `TowerPlacer.ExitPlacementMode()` 호출 (배치 취소)

## 3. 신규 클래스 / 파일

없음 (기존 파일 수정만)

## 4. 테스트 계획

- [ ] 유닛 버튼 클릭 시 커서에 반투명 ghost 표시됨
- [ ] Buildable 타일 위 → ghost 초록색
- [ ] Path / 이미 타워 있는 곳 → ghost 빨간색
- [ ] 초록색 상태에서 좌클릭 → 타워 배치 확정, ghost 사라짐
- [ ] 빨간색 상태에서 좌클릭 → 배치 안 됨, ghost 유지
- [ ] ESC 또는 우클릭 → ghost 사라지고 배치 취소
- [ ] Rift 모드 전환 시 Tower ghost 자동 취소

## 5. 위험 요소

- Tower.Awake()가 ghost 생성 시 실행됨 → `InitAsGhost()`로 즉시 비활성화 필요
- Tower.OnDestroy()가 ghost 파괴 시 MapTileSystem.RemoveTower(0,0) 호출 위험 → IsGhost 가드로 방어
- 큐브 수가 부족할 때 ghost는 색상 체크 없이 Buildable이면 초록색으로 표시됨 (TryPlace에서 큐브 부족 시 실패하게 됨 — 현재 동작과 동일, 별도 이슈)
