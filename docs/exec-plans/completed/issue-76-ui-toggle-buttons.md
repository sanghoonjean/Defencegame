# Issue #76 — Shop UI / Inventory UI 호출 버튼 추가

## 1. 시스템 구조

```
SHOPbtn (Button)
  └─ UIToggleButton  ← targetPanel = ShopUI GameObject
InventoryBtn (Button)
  └─ UIToggleButton  ← targetPanel = InventoryUI GameObject
```

버튼 클릭 시 `targetPanel.SetActive(!targetPanel.activeSelf)` 로 토글.

## 2. 수정 파일

없음.

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/UIToggleButton.cs` | Button 클릭 시 지정된 패널을 토글. `targetPanel` Inspector에서 지정 |

## 4. 테스트 계획

- [ ] SHOPbtn 클릭 → ShopUI 활성화 확인
- [ ] SHOPbtn 재클릭 → ShopUI 비활성화 확인
- [ ] InventoryBtn 동일 테스트

## 5. 위험 요소

- 씬에서 버튼 GameObject에 컴포넌트 부착 및 targetPanel 연결은 사용자가 직접 수행
- `targetPanel`이 null이면 클릭 시 NullReferenceException 발생 → null 가드 처리
