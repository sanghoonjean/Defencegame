# Issue #77 — Shop 스킬에 구현된 공격 스킬 들 등록 및 구매 기능 추가

## 1. 시스템 구조

```
Shop Panel (UIToggleButton으로 열림)
  └─ SkillSlot_0 ~ SkillSlot_4 (각각 ShopSkillSlotUI 컴포넌트)
       ├─ Image (아이콘)
       ├─ Text (스킬 이름)
       ├─ Text (가격 고정 "하위 큐브 ×1")
       └─ Button (구매) ← 큐브 부족 시 비활성화
```

구매 흐름:
```
Button.onClick
  → ShopSkillSlotUI.OnBuyClicked()
  → ShopSystem.Instance.BuySkill(skillData)  ← 큐브 소모 + _ownedSkills.Add()
  → ShopSystem.OnInventoryChanged 이벤트 발동
  → 버튼 상태 갱신 (큐브 잔량 기반)
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Systems/ShopSystem.cs` | `BuySkill()`에서 중복 방지 체크 제거. 스펙: "동일 항목 중복 구매 가능 (여러 타워에 장착 가능)" |

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/ShopSkillSlotUI.cs` | Shop 패널 내 개별 스킬 슬롯. Inspector에서 `SkillData` 지정, 아이콘/이름/구매버튼 관리 |

## 4. 테스트 계획

- [ ] Unity Editor: Shop 패널 하위에 5개 슬롯 GameObject 생성, `ShopSkillSlotUI` 부착
- [ ] 각 슬롯에 Inspector에서 `SkillData` 에셋 연결 (`Perfab/Skills/*.asset`)
- [ ] 하위 큐브 0개 상태 → 구매 버튼 비활성화 확인
- [ ] `C`키(TestRunner)로 큐브 +10 지급 → 버튼 활성화 확인
- [ ] 구매 버튼 클릭 → `ShopSystem.OwnedSkills.Count` +1 확인
- [ ] 구매 후 큐브 1개 감소 확인
- [ ] 동일 스킬 재구매 → 중복 구매 허용 확인 (OwnedSkills에 2개 추가)
- [ ] 큐브 0개에서 구매 시도 → 버튼 비활성화로 클릭 불가 확인

## 5. 위험 요소

- Unity Editor에서의 Scene 설정(패널 GameObject, 슬롯 배치, Inspector 연결)은 개발자 수동 작업
- `ShopSystem.BuySkill()` 중복 방지 제거 시, 동일 스킬을 무한 구매 가능 → 큐브 소진으로 자연 제한됨
- `InventorySystem.EquipSkill()`에서 `_ownedSkills` 보유 여부 검증 없음 → 별도 이슈 범위
