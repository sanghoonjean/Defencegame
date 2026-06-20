# Issue #117 — 인벤토리 슬롯 간 드래그로 순서 재배치

## 1. 시스템 구조

- `InvenSlotDragHandler`에 `IDropHandler` 추가 → 인벤토리 슬롯끼리 드랍 처리
- `ShopSystem`에 `SwapOwnedSkills` / `MoveOwnedSkill` 추가 → 리스트 순서 변경
- `InvenUI.Awake`에서 각 슬롯 `drag.SlotIndex` 주입

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/InvenSlotDragHandler.cs`
- `MakeDefence/Assets/Scripts/UI/InvenUI.cs`
- `MakeDefence/Assets/Scripts/Systems/ShopSystem.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

### ShopSystem
- `SwapOwnedSkills(int a, int b)`: 두 인덱스 위치 교환
- `MoveOwnedSkill(int from, int to)`: from 위치 스킬을 to 위치로 이동

### InvenSlotDragHandler
- `SlotIndex` 프로퍼티 추가
- `IDropHandler.OnDrop`: 소스가 `InvenSlotDragHandler`이면 스왑/이동 처리
  - 목적지 빈 슬롯: `MoveOwnedSkill`
  - 목적지 스킬 있음: `SwapOwnedSkills`

### InvenUI
- `drag.SlotIndex = list.Count` 로 각 슬롯 인덱스 주입

## 5. 테스트 계획

- [ ] 스킬 2개 구매 → 인벤토리에서 두 슬롯 드래그 교환 → 위치 변경 확인
- [ ] 스킬 있는 슬롯 → 빈 슬롯으로 드래그 → 이동 확인
- [ ] 재배치 후 장착 버튼 정상 동작 확인
- [ ] 장착 슬롯/상점으로 드랍 시 기존 로직 유지 확인

## 6. 위험 요소

없음
