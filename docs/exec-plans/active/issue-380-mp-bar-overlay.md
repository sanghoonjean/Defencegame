# Issue #380 — MP 바 UI가 다른 UI 패널 위에 표시되는 현상 개선

## 1. 시스템 구조

### 현상 및 원인

- 타워 MP 바 / 적 HP 바 / 데미지 텍스트는 `GameUIManager.OnGUI()`(IMGUI)에서
  `GUI.DrawTexture` 로 스크린 좌표에 직접 그린다.
- Unity 렌더링 순서상 IMGUI 는 **모든 uGUI Canvas(Screen Space Overlay 포함)보다
  항상 나중에** 그려진다. 따라서 ItemHubPanel 등 UI 패널을 열어도 MP 바가
  패널 위로 뚫고 나온다. Canvas sorting order 로는 해결 불가.

### 해결 구조: 패널 스크린 영역 오클루전(가림) 처리

1. **`UIScreenBlocker` (신규 컴포넌트)** — 바를 가려야 하는 패널(실제로
   열림/닫힘이 일어나는 오브젝트)에 부착.
   - `OnEnable` 에서 정적 레지스트리(`List<UIScreenBlocker>`)에 등록,
     `OnDisable` 에서 해제. `SetActive` 토글 패널(`UIToggleButton`,
     `UITabView`, 팝업류의 자식 panel)과 정확히 맞물린다.
   - **CanvasGroup 인식(Codex 리뷰 반영)**: `SetActive` 대신 `CanvasGroup.alpha`
     로 숨는 패널(Unit_Panel, DimesionStoneInventoryUI)을 위해, Rect 수집 시
     자신의 `CanvasGroup` alpha 가 임계값(0.01) 이하면 가림 비활성으로 취급.
     CanvasGroup 이 없는 패널은 활성 상태 = 가림.
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

### UIScreenBlocker 부착 대상 패널 (Codex 리뷰 반영, 씬 계층 확인 후 확정)

| 대상 | 숨김 방식 | 부착 위치 |
|---|---|---|
| `ItemHubPanel` (이슈 주 대상) | SetActive (`UIToggleButton`) | 패널 루트 |
| `Unit_Panel` | CanvasGroup alpha (`UnitPanelController`) | 패널 루트 (CanvasGroup 인식으로 처리) |
| `DimesionStoneInventoryUI` | CanvasGroup alpha (`CanvasGroupToggleButton`) | 패널 루트 (CanvasGroup 인식으로 처리) |
| `SettingsPanel` | SetActive — 컨트롤러가 자식 `panel` 토글 | **토글되는 panel 오브젝트** |
| `JobSelectPopup` / `SellConfirmPopup` / `TowerDeleteConfirmPopup` / `SupportUnlockPopup` | SetActive — 루트는 상시 활성, 자식 `panel` 만 토글 | **자식 `panel` 오브젝트** (루트 아님) |

- **제외**: `EnemyPanel`, `UnitListPanel`, `HPPanel` 등 상시 표시 HUD —
  씬 로드 시 항상 활성이고 SetActive 토글 배선이 없어, 부착하면 해당 영역의
  바가 영구히 사라지는 부작용 발생 (Codex 지적으로 대상에서 제거).

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/UI/UIScreenBlocker.cs`
  - 역할: 부착된 패널이 **보이는 동안**(활성 + CanvasGroup alpha > 임계값)
    자신의 스크린 Rect 를 정적 레지스트리에 노출. 로직 없음(등록/가시성
    판정/Rect 계산만) — 어떤 패널에도 재사용 가능.

## 4. 테스트 계획

- [ ] 마나 시스템 있는 타워 배치 → 인벤토리/상점(ItemHubPanel) 열기 →
      패널과 겹치는 MP 바가 보이지 않는지 확인
- [ ] 패널 닫으면 MP 바가 다시 정상 표시되는지 확인
- [ ] 탭 전환(인벤 ↔ 상점) 중에도 가림이 유지되는지 확인
- [ ] 적 HP 바도 패널 뒤에서 가려지는지 확인
- [ ] 패널과 겹치지 않는 위치의 바는 영향 없이 표시되는지 확인
- [ ] 팝업(판매 확인, 타워 삭제 등) 위로도 바가 뚫고 나오지 않고,
      팝업을 닫으면(자식 panel SetActive(false)) 바가 즉시 복원되는지 확인
- [ ] Unit_Panel: 타워 선택 시 가림 / 선택 해제(alpha=0) 시 즉시 복원 확인
- [ ] DimesionStoneInventoryUI: 열림(alpha=1) 시 가림 / 닫힘 시 복원 확인
- [ ] EnemyPanel·UnitListPanel 등 상시 HUD 영역에서는 바가 정상 표시되는지 확인
- [ ] 카메라 줌/이동 시 가림 판정이 바 위치를 따라가는지 확인

## 5. 위험 요소

- **부착 대상 선별**: 반투명하거나 작은 HUD 요소에 붙이면 바가 과도하게
  사라짐 → 화면을 크게 덮는 열림/닫힘 패널에만 부착 (상시 HUD 제외 —
  위 부착 대상 표 참고).
- **CanvasGroup alpha 로 숨기는 패널** (Codex 리뷰 반영): Unit_Panel,
  DimesionStoneInventoryUI 는 alpha=0 으로 숨고 GameObject 는 활성 유지 →
  UIScreenBlocker 가 자신의 CanvasGroup alpha 를 검사해 임계값 이하면 가림
  비활성 처리. alpha 애니메이션(페이드)이 도입되면 임계값 재검토 필요.
- **팝업 부착 위치**: 팝업 루트는 상시 활성이고 자식 `panel` 만 토글되므로,
  블로커는 반드시 토글되는 자식 panel 에 부착 (루트 부착 시 영구 가림).
- **데미지 텍스트는 범위 제외**: 동일 문제가 있으나 수명이 짧고 시각적 영향이
  작아 이번 이슈에서 제외. 필요 시 후속 이슈로 처리.
- **씬 편집은 UnityMCP 로 수행** — 도메인 리로드 전 씬 저장 필수
  (미저장 변경 유실 방지).
