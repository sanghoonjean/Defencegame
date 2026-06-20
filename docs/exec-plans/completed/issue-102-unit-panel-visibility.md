# Issue #102 — 타워 클릭 시에만 Unit_Panel 표시

## 1. 시스템 구조

```
[클릭 감지]
TestRunner.Update (좌클릭)
  ├── UI 위 클릭
  │     ├── Inventory/Shop 패널 위 → 아무것도 안 함 (패널 유지)
  │     └── 그 외 UI 위 → InventorySystem.Deselect()
  └── 빈 공간 클릭
        ├── 타워 히트 → InventorySystem.SelectTower(tower)
        └── 타워 없음 → InventorySystem.Deselect()

[Unit_Panel 표시/숨김]
UnitPanelController (Unit_Panel에 부착)
  └── InventorySystem.OnTowerSelected 구독
        ├── tower != null → Unit_Panel.SetActive(true)
        └── tower == null → Unit_Panel.SetActive(false)

[안전 UI 마커]
KeepSelectionUI (Inventory 패널 루트, Shop 패널 루트에 부착)
  └── 클릭 시 선택 해제 방지 마커 역할
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/TestRunner.cs` | 좌클릭 로직 확장 — 빈 공간/비안전 UI 클릭 시 `Deselect()` 호출 |

## 3. 신규 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/UnitPanelController.cs` | `Unit_Panel`에 부착. `OnTowerSelected` 구독하여 패널 활성/비활성 제어 |
| `Assets/Scripts/UI/KeepSelectionUI.cs` | 마커 컴포넌트. Inventory 패널·Shop 패널 루트에 부착. 클릭해도 선택 해제 안 됨 |

## 4. 테스트 계획

- [ ] 타워 클릭 → Unit_Panel 표시 확인
- [ ] 빈 공간 클릭 → Unit_Panel 숨김 확인
- [ ] Inventory 패널 클릭 → Unit_Panel 유지 확인
- [ ] Shop 패널 클릭 → Unit_Panel 유지 확인
- [ ] 타워 선택 후 다른 타워 클릭 → Unit_Panel 유지 (새 타워로 갱신)

## 5. 위험 요소

- `EventSystem.current.RaycastAll()` 결과에서 부모 방향으로 `KeepSelectionUI` 탐색 필요
- `Unit_Panel`이 초기에 비활성 상태여야 함 (씬에서 직접 설정 필요)
- TestRunner는 개발 테스트용 스크립트 — 추후 실제 클릭 매니저로 이전 필요
