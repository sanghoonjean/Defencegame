# Issue #99 — 드래그 앤 드랍으로 메인 스킬 해제 구현

## 1. 시스템 구조

```
[드래그 소스]
Unit_C_Panel → Skill_Panel → Main_Skill  [SkillSlotUI + SkillSlotDragHandler]
  └── ICON (Image) ← 드래그 중 고스트 이미지 원본

[드랍 타겟 A — 장착 해제]
InvertoryUI → Scroll View → Viewport → Content  [InvenDropHandler 추가]
  → 드랍 시: Tower.UnequipSkill() + ShopSystem.ReturnSkill()

[드랍 타겟 B — 판매]
ShopUI 패널 루트 오브젝트  [ShopDropHandler 추가]
  → 드랍 시: Tower.UnequipSkill() + CubeSystem.Add(Lower, 1)
```

이벤트 흐름:
```
Main_Skill 드래그 시작 (SkillSlotDragHandler.OnBeginDrag)
  → 장착 스킬 없으면 취소
  → Canvas 위에 고스트 Image 생성
  → OnDrag: 고스트를 포인터 위치로 이동
  → Content에 드랍 (InvenDropHandler.OnDrop)
      → InventorySystem.UnequipSkill()
      → ShopSystem.ReturnSkill(skill)
  → Shop패널에 드랍 (ShopDropHandler.OnDrop)
      → InventorySystem.UnequipSkill()
      → CubeSystem.Add(CubeType.Lower, 1)
  → OnEndDrag: 고스트 제거
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Systems/InventorySystem.cs` | `UnequipSkill()` 메서드 추가 |
| `Assets/Scripts/Gameplay/Tower/Tower.cs` | `UnequipSkill()` 메서드 추가 |

## 3. 신규 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/SkillSlotDragHandler.cs` | Main_Skill에 런타임 or Inspector 부착. IBeginDragHandler / IDragHandler / IEndDragHandler 구현. 장착 스킬 드래그 시 고스트 표시 |
| `Assets/Scripts/UI/InvenDropHandler.cs` | 인벤토리 Content 오브젝트에 부착. IDropHandler 구현. 드랍 시 스킬 장착 해제 + 인벤토리 반환 |
| `Assets/Scripts/UI/ShopDropHandler.cs` | Shop 패널 루트에 부착. IDropHandler 구현. 드랍 시 스킬 판매 + Lower 큐브 1개 반환 |

## 4. 테스트 계획

- [ ] 스킬 장착 후 Main_Skill 드래그 → 인벤토리 Content에 드랍 → 장착 해제 + 인벤토리 슬롯에 스킬 추가 확인
- [ ] 스킬 장착 후 Main_Skill 드래그 → Shop 패널에 드랍 → 장착 해제 + Lower 큐브 +1 확인
- [ ] 스킬 미장착 상태에서 Main_Skill 드래그 → 드래그 자체가 시작 안 됨 확인
- [ ] 드래그 취소(다른 곳 드랍) → 장착 상태 유지 확인

## 5. 위험 요소

- `SkillSlotUI`는 `iconImage`가 SerializeField라 런타임 Inspector 연결 필요 → `SkillSlotDragHandler`를 별도 컴포넌트로 분리, `SkillSlotUI`에서 `Init()` 호출
- Shop 패널 오브젝트 이름 확인 필요 (씬 파일 직접 수정 불가 → `ShopDropHandler`를 `ShopSkillSlotUI`가 있는 부모 오브젝트에 AddComponent하거나 별도 부착 안내)
- `Tower.EquipSkill`은 `EquippedSkill = skill` 단순 대입 → `UnequipSkill()`은 `EquippedSkill = null` + 스탯 초기화 필요
