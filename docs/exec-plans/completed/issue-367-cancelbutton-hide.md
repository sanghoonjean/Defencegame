# Issue #367 — SHOP_UI / InventoryUI CancelButton 클릭 시 UI 숨김 처리

## 1. 시스템 구조

```
SHOP_UI (GameObject, SetActive 방식 표시/숨김)
  └─ CancelButton (Button)
        └─ UICloseButton  ← targetPanel = SHOP_UI
InventoryUI (GameObject, SetActive 방식 표시/숨김)
  └─ CancelButton (Button)
        └─ UICloseButton  ← targetPanel = InventoryUI
```

- SHOP_UI / InventoryUI 둘 다 CanvasGroup 이 없어 `GameObject.SetActive` 로 열고 닫힌다 (`UIToggleButton` 패턴과 동일).
- CancelButton 은 부모 패널의 자식이므로, 패널을 `SetActive(false)` 하면 버튼도 함께 사라진다 → 이후 재클릭 불가(정상 동작).
- 열기는 기존 SHOPbtn / InventoryBtn(`UIToggleButton`)이 담당하므로, CancelButton 은 "닫기 전용"이면 충분하다.

## 2. 수정 파일

| 파일 | 변경 |
|------|------|
| `Assets/Scenes/SampleScene.unity` | CancelButton 2개에 `UICloseButton` 컴포넌트 부착 및 `targetPanel` 배선 (UnityMCP 로 처리) |

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/UICloseButton.cs` | Button 클릭 시 지정된 `targetPanel` 을 `SetActive(false)` 로 숨김. `targetPanel` 은 Inspector 에서 지정. null 가드 포함 |

> `UIToggleButton`(토글) / `CanvasGroupToggleButton`(alpha 토글) 과 별개로, 의도를 명확히 하기 위해 "닫기 전용" 컴포넌트를 신설한다.

## 4. 테스트 계획

- [ ] SHOP_UI 표시 상태에서 CancelButton 클릭 → SHOP_UI 숨김 확인
- [ ] InventoryUI 표시 상태에서 CancelButton 클릭 → InventoryUI 숨김 확인
- [ ] 숨김 후 기존 SHOPbtn / InventoryBtn 으로 다시 열림 확인 (열기 로직 영향 없음)
- [ ] `targetPanel` 미연결 시 클릭해도 예외 없이 경고 로그만 출력
- [ ] 컴파일 에러 없음 (`read_console`)

## 5. 위험 요소

- CancelButton 이 아직 씬 파일에 저장되지 않은 라이브 에디터 상태 → 배선 후 씬 저장 필요.
- 컴포넌트 부착 및 `targetPanel` 연결은 UnityMCP 로 수행 (AGENTS.md §7: .unity 직접 편집 금지).
- `targetPanel` 이 null 이면 NullReferenceException 대신 경고 로그만 남기도록 가드 처리.
