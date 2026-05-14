# Issue #71 — 스킬 슬롯 UI 구현

## 1. 시스템 구조

```
InventorySystem.OnTowerSelected (event)
        │
        ▼
SkillSlotUI (MonoBehaviour)          SupportSlotUI (MonoBehaviour) ×3
  - 공격 스킬 아이콘/이름 표시          - 보조 옵션 슬롯 1개 표시
  - 스킬 없으면 패널 숨김              - 옵션 없으면 "비어있음" 표시
```

**UI 계층 (씬에서 사용자가 구성)**
```
Unit_C_Panel
  └─ Skill Panel
       ├─ Main Skill          ← SkillSlotUI 부착
       │    ├─ Icon (Image)
       │    └─ SkillName (Text)
       └─ Support Slots
            ├─ Slot 0        ← SupportSlotUI 부착 (slotIndex=0)
            ├─ Slot 1        ← SupportSlotUI 부착 (slotIndex=1)
            └─ Slot 2        ← SupportSlotUI 부착 (slotIndex=2)
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `Sprite icon`, `string displayName` 필드 추가 |
| `Assets/Scripts/Gameplay/Tower/SupportOptionData.cs` | `Sprite icon` 필드 추가 |

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/SkillSlotUI.cs` | 선택된 타워의 공격 스킬을 표시하는 패널 컨트롤러. `InventorySystem.OnTowerSelected` 구독, Icon + Name 업데이트 |
| `Assets/Scripts/UI/SupportSlotUI.cs` | 보조 옵션 슬롯 1칸 표시. `slotIndex` 인스펙터 설정, 옵션 없으면 빈 슬롯 표시 |

## 4. 테스트 계획

- [ ] 타워 배치 → 타워 클릭 → Unit_C_Panel의 Main Skill에 스킬 이름 표시 확인
- [ ] 스킬 미장착 타워 클릭 → 스킬 슬롯 비어있음 또는 패널 숨김 확인
- [ ] 다른 타워 클릭 시 UI가 새 타워 스킬로 업데이트 확인
- [ ] 보조 옵션 장착 후 타워 클릭 → 해당 슬롯에 옵션 이름 표시 확인
- [ ] 타워 선택 해제(Deselect) 시 UI 초기화 확인

## 5. 위험 요소

- 씬/프리팹 수정은 사용자가 직접 수행 — `SkillSlotUI`, `SupportSlotUI` 컴포넌트를 해당 GameObject에 부착하고 Inspector에서 참조 연결 필요
- `SkillData.icon`이 null이면 Image 비활성화 처리 필요
- `Tower.SupportOptions`는 `UnlockedSupportSlots` 수만큼만 유효 — 잠금된 슬롯은 "잠김" 표시 또는 숨김
