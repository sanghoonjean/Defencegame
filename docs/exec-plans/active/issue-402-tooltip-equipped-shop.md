# Issue #402 — 아이템 툴팁 확장: 장착 슬롯 + 상점 슬롯

## 1. 시스템 구조

#398 의 ItemTooltipTrigger 에 **텍스트 소스 델리게이트**를 추가해, 슬롯 종류별 데이터 출처 차이를 흡수한다.

```
ItemTooltipTrigger
 ├─ TextSource (Func<string>) 지정됨   → 호버 시 델리게이트 호출 (장착/상점 슬롯)
 └─ TextSource 미지정 (기본)           → 같은 GO 의 InvenSlotDragHandler 에서 읽음 (인벤 슬롯, #398 동작 유지)
```

슬롯별 데이터 소스:

| 슬롯 | 부착 위치 | 데이터 소스 |
|---|---|---|
| 장착 스킬 | SkillSlotUI.Awake | `InventorySystem.Instance?.SelectedTower?.EquippedSkill` (호버 시점 평가 — 스테일 없음) |
| 장착 서포트 | SupportSlotUI.Awake | 기존 InvenSlotDragHandler.Support (기본 동작, TextSource 불필요) |
| 장착 차원석 | GenerateSlotDropTarget.Awake | `WaveGeneratorSystem.Instance?.LoadedStone` |
| 상점 스킬 | ShopSkillSlotUI.Awake | serialized `skillData` |
| 상점 서포트 | ShopSupportSlotUI.Awake | serialized `optionData` |

- 전부 코드에서 AddComponent — 씬/프리팹 수정 없음
- 텍스트 빌더(BuildSkillText 등)는 public static 으로 공개해 델리게이트에서 재사용
- 드래그 시작 시 숨김은 기존 IBeginDragHandler 로 동일 동작 (SkillSlotDragHandler / GenerateSlotDropTarget 드래그 포함)

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/ItemTooltipTrigger.cs` — `TextSource` 프로퍼티 추가, 빌더 public 화
- `MakeDefence/Assets/Scripts/UI/SkillSlotUI.cs` — Awake 에서 트리거 부착 + TextSource 지정
- `MakeDefence/Assets/Scripts/UI/SupportSlotUI.cs` — Awake 에서 트리거 부착
- `MakeDefence/Assets/Scripts/UI/GenerateSlotDropTarget.cs` — Awake 에서 트리거 부착 + TextSource 지정
- `MakeDefence/Assets/Scripts/UI/ShopSkillSlotUI.cs` — Awake 에서 트리거 부착 + TextSource 지정
- `MakeDefence/Assets/Scripts/UI/ShopSupportSlotUI.cs` — Awake 에서 트리거 부착 + TextSource 지정

## 3. 신규 클래스 / 파일

없음 (#398 컴포넌트 재사용)

## 4. 테스트 계획

- UnityMCP `refresh_unity` + `read_console` 컴파일 검증
- 플레이 모드 실측:
  - 타워 선택 + 스킬 장착 상태에서 Main Skill 슬롯 호버 → 스킬 툴팁
  - 서포트 장착 슬롯 호버 → 서포트 툴팁 (잠금/빈 슬롯은 미표시)
  - GenerateSlot 에 차원석 장착 후 호버 → 차원석 툴팁
  - 상점 스킬/서포트 슬롯 호버 → 아이템 툴팁
  - 인벤 슬롯 기존 동작 회귀 확인

## 5. 위험 요소

- 호버 이벤트는 자식 raycast 대상에서 부모로 전파되므로 상점 슬롯의 구매 버튼 위에서도 툴팁이 뜸 — 의도된 동작으로 간주
- 장착 슬롯 내용이 호버 중 바뀌는 경우 (클릭 장착) 툴팁 갱신은 재호버 시점 — 허용
- 타워 미선택/빈 슬롯이면 TextSource 가 null 반환 → 툴팁 미표시 확인 필요
