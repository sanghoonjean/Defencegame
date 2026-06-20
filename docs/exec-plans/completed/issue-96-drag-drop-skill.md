# Issue #96 — 드래그 앤 드랍으로 타워 스킬 장착 기능 구현

## 1. 시스템 구조

```
[드래그 소스]
InvertoryUI → Scroll View → Viewport → Content
  └── InvenSlot[N]  ← InvenSlotDragHandler 런타임 추가 (InvenUI.Awake)
        └── ICON (Image)  ← 드래그 중 고스트 이미지 원본

[드랍 타겟]
Unit_C_Panel → Skill_Panel → Main_Skill  [SkillSlotUI 부착됨]
  └── IDropHandler 추가 (SkillSlotUI 확장)
```

이벤트 흐름:
```
InvenSlot 드래그 시작 (IBeginDragHandler)
  → Canvas 위에 고스트 Image 생성
  → OnDrag: 고스트를 포인터 위치로 이동
  → Main_Skill에 드랍 (IDropHandler)
      → 타워 미선택 시 Log만
      → 타워 선택 시:
          기존 장착 스킬 → ShopSystem.ReturnSkill() (인벤토리로 반환)
          새 스킬 → InventorySystem.EquipSkill()
      → OnEndDrag: 고스트 제거
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/UI/InvenUI.cs` | Awake에서 각 슬롯에 InvenSlotDragHandler 추가, Refresh에서 Skill 참조 갱신 |
| `Assets/Scripts/UI/SkillSlotUI.cs` | IDropHandler 구현 — 드랍 시 스킬 교체 처리 |
| `Assets/Scripts/Systems/ShopSystem.cs` | ReturnSkill(SkillData) 메서드 추가 |

## 3. 신규 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/InvenSlotDragHandler.cs` | InvenSlot에 런타임 추가. IBeginDragHandler / IDragHandler / IEndDragHandler 구현. 드래그 중 고스트 Image 표시 |

## 4. 테스트 계획

- [ ] 스킬 구매 후 인벤토리 슬롯에서 Main_Skill로 드래그 앤 드랍 → 스킬 장착 확인
- [ ] 타워 미선택 상태에서 드랍 → 로그만 발생, 장착 안 됨
- [ ] 이미 스킬 장착된 상태에서 다른 스킬 드랍 → 기존 스킬 인벤토리 반환, 새 스킬 장착
- [ ] Main_Skill 외 다른 곳에 드랍 → 로그만 발생
- [ ] 드래그 취소(ESC or 다른 곳 드랍) → 원래 슬롯 상태 유지

## 5. 위험 요소

- 고스트 Image 생성 시 Canvas 참조 필요 → `GetComponentInParent<Canvas>()` 사용
- `ShopSystem.OwnedSkills`는 `IReadOnlyList`라 직접 수정 불가 → `ReturnSkill()` 메서드 필요
- Tower의 `EquippedSkill`이 null일 수 있음 → null 체크 필수
- InvenUI.Refresh() 호출 시 슬롯 인덱스와 스킬 매핑이 바뀔 수 있으므로 DragHandler의 Skill 참조도 함께 갱신 필요
