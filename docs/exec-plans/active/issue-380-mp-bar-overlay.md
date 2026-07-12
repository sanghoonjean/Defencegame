# Issue #380 — MP 바 UI가 다른 UI 패널 위에 표시되는 현상 개선

## 1. 시스템 구조

### 현상 및 원인

- 타워 MP 바 / 적 HP 바 / 데미지 텍스트는 `GameUIManager.OnGUI()`(IMGUI)에서
  `GUI.DrawTexture` 로 스크린 좌표에 직접 그린다.
- Unity 렌더링 순서상 IMGUI 는 **모든 uGUI Canvas(Screen Space Overlay 포함)보다
  항상 나중에** 그려진다. 따라서 ItemHubPanel 등 UI 패널을 열어도 MP 바가
  패널 위로 뚫고 나온다. Canvas sorting order 로는 해결 불가.

### 해결 구조: 패널 스크린 영역 오클루전(가림) 처리

1. **`UIScreenBlocker` (신규 컴포넌트)** — 바를 가려야 하는 패널 루트에 부착.
   - `OnEnable` 에서 정적 레지스트리(`List<UIScreenBlocker>`)에 등록,
     `OnDisable` 에서 해제. 패널 토글이 전부 `SetActive` 기반(`UIToggleButton`,
     `UITabView`)이므로 이 패턴과 정확히 맞물린다.
   - `RectTransform.GetWorldCorners` 로 GUI 좌표계(좌상단 원점, y 반전) Rect 를
     계산해 제공. Screen Space Overlay 캔버스에서는 월드 코너가 곧 스크린 픽셀.
   - 프레임당 1회 Rect 캐싱 (`Time.frameCount` 비교) 으로 반복 계산 방지.

2. **`GameUIManager.OnGUI()` 수정** — Repaint 시작 시 열려 있는 블로커 Rect 목록을
   수집하고, 각 바(MP 바 + HP 바)의 Rect 가 블로커 Rect 와 `Overlaps` 하면
   해당 바 전체를 그리지 않는다 (부분 클리핑 대신 전체 스킵 — 시각적으로 깔끔).

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs`
  - 블로커 Rect 수집 + `IsBlocked(Rect)` 헬퍼 추가
  - 타워 MP 바 그리기 전 오클루전 검사 (이슈 본건)
  - 적 HP 바에도 동일 검사 적용 (같은 구조의 동일 버그)
- `MakeDefence/Assets/Scenes/SampleScene.unity`
  - 대상 패널 루트에 `UIScreenBlocker` 부착 (UnityMCP 로 수행)

### UIScreenBlocker 부착 대상 패널 (씬 계층 확인 후 확정)

- `ItemHubPanel` (인벤토리/상점 탭 패널 — 이슈 리포트의 주 대상)
- `Unit_Panel`, `EnemyPanel`, `UnitListPanel`, `SettingsPanel`
- `JobSelectPopup`, `SellConfirmPopup`, `TowerDeleteConfirmPopup` 등 팝업류
- 항상 떠 있는 HUD(HPPanel 등)는 제외 — 바가 상시 사라지는 부작용 방지

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/UI/UIScreenBlocker.cs`
  - 역할: 부착된 패널이 활성화된 동안 자신의 스크린 Rect 를 정적 레지스트리에
    노출. 로직 없음(등록/Rect 계산만) — 어떤 패널에도 재사용 가능.

## 4. 테스트 계획

- [ ] 마나 시스템 있는 타워 배치 → 인벤토리/상점(ItemHubPanel) 열기 →
      패널과 겹치는 MP 바가 보이지 않는지 확인
- [ ] 패널 닫으면 MP 바가 다시 정상 표시되는지 확인
- [ ] 탭 전환(인벤 ↔ 상점) 중에도 가림이 유지되는지 확인
- [ ] 적 HP 바도 패널 뒤에서 가려지는지 확인
- [ ] 패널과 겹치지 않는 위치의 바는 영향 없이 표시되는지 확인
- [ ] 팝업(판매 확인, 타워 삭제 등) 위로도 바가 뚫고 나오지 않는지 확인
- [ ] 카메라 줌/이동 시 가림 판정이 바 위치를 따라가는지 확인

## 5. 위험 요소

- **부착 대상 선별**: 반투명하거나 작은 HUD 요소에 붙이면 바가 과도하게
  사라짐 → 화면을 크게 덮는 열림/닫힘 패널에만 부착.
- **CanvasGroup alpha 로 숨기는 패널**: `SetActive` 가 아닌 alpha=0 방식으로
  숨는 패널이 있다면 OnDisable 이 안 불려 계속 가림 → 부착 대상을 SetActive
  토글 패널로 한정 (`CanvasGroupToggleButton` 사용처는 부착 전 확인).
- **데미지 텍스트는 범위 제외**: 동일 문제가 있으나 수명이 짧고 시각적 영향이
  작아 이번 이슈에서 제외. 필요 시 후속 이슈로 처리.
- **씬 편집은 UnityMCP 로 수행** — 도메인 리로드 전 씬 저장 필수
  (미저장 변경 유실 방지).
