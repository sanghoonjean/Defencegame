# Issue #77 — InvertoryUI InvenSlot 갱신 수정

## 1. 시스템 구조

```
InvertoryUI (팝업, 기본 비활성)
└── Scroll View → Viewport
    └── Content (GridLayoutGroup: 6열, 80×80)
        ├── Slot[0] ← 빈 슬롯 (ItemImage.sprite = null)
        ├── Slot[1] ← 구매 후 → ItemImage.sprite = skill.icon
        └── ... (총 34개)
```

이벤트 흐름:
```
ShopSkillSlotUI.OnBuyClicked()
  → ShopSystem.BuySkill()
  → ShopSystem.OnInventoryChanged
  → InvenUI.Refresh()
      → Content 하위 Slot들의 ItemImage.sprite 순서대로 갱신
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scenes/SampleScene.unity` | Unit_L_Panel에서 OwnedSkillsListUI + HorizontalLayoutGroup 제거; Content에 InvenUI 추가 |

## 3. 신규 / 삭제 파일

**신규**
| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/InvenUI.cs` | Content에 부착. ShopSystem.OnInventoryChanged 구독 → Slot ItemImage sprite 갱신 |

**삭제**
- `Assets/Scripts/UI/OwnedSkillsListUI.cs`
- `Assets/Scripts/UI/OwnedSkillSlotUI.cs`
- `Assets/Perfab/ETC/OwnedSkillSlot.prefab`

## 4. 테스트 계획

- [ ] 스킬 구매 → InvertoryUI 열었을 때 Slot에 아이콘 표시 확인
- [ ] 스킬 2개 구매 → 첫 두 슬롯에 각각 아이콘, 나머지 빈 슬롯
- [ ] 화면 하단 Unit_L_Panel에 아이콘이 더 이상 생기지 않는지 확인

## 5. 위험 요소

- InvertoryUI가 기본 비활성이므로 OnEnable/OnDisable 기반 구독 안전
- Content 자식 Slot 34개 초과 구매 시 슬롯 부족 → 현재 스펙 범위 밖
