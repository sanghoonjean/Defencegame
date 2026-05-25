# Issue #118 — 스킬 판매 확인 팝업 UI

## 1. 시스템 구조

현재 흐름:
```
InvenSlotDragHandler → ShopDropHandler.OnDrop()
                         → UnequipSkill() + CubeSystem.Add() 즉시 실행
```

수정 흐름:
```
ShopDropHandler.OnDrop()
  → SellConfirmPopup.Show(skillName, cubeCount)
      → "판매" 클릭 → UnequipSkill() + CubeSystem.Add()
      → "취소" 클릭 → 팝업 닫기, 원상복귀
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/ShopDropHandler.cs`

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/UI/SellConfirmPopup.cs`
  - `SupportUnlockPopup.cs`와 동일한 singleton+panel 패턴
  - Inspector 직결: `panel`, `messageText`, `confirmButton`, `cancelButton`
  - `Show(SkillData skill)` — 스킬명 + 큐브 수량 메시지 표시
  - "판매" → `InventorySystem.UnequipSkill()` + `CubeSystem.Add(Lower, 1)` 후 팝업 닫기
  - "취소" → 팝업 닫기

## 4. 테스트 계획

- [ ] 장착 스킬을 상점으로 드래그 드랍 → 확인 팝업 표시
- [ ] 팝업에 스킬명 + "하급 큐브 1개 획득" 메시지 표시
- [ ] "판매" 클릭 → 큐브 +1, 스킬 해제 확인
- [ ] "취소" 클릭 → 스킬 유지, 팝업 닫힘 확인
- [ ] 타워 미선택 상태에서 드랍 → 팝업 미표시 (기존 동작 유지)

## 5. 위험 요소

- `SellConfirmPopup` prefab/panel을 Unity Inspector에서 직접 연결해야 동작
- 팝업이 없으면(Instance == null) 즉시 판매로 폴백해 기존 동작 유지
