# Issue #220 — 인벤토리 UI 통합 (메인 스킬 / 서포트 옵션 단일 그리드)

## 1. 시스템 구조

### 현 구조
```
ShopSystem
├─ _ownedSkills   : List<SkillData>          ──┐
└─ _ownedSupports : List<SupportOptionData>  ──┤
                                               ▼
                   InvenUI         (스킬만)
                   SupportInvenUI  (서포트만)
                       │
                       ▼
                   InvenSlotDragHandler        (스킬)
                   SupportOptionDragHandler    (서포트)
                       │
                       ▼
                   SkillSlotUI / SupportSlotUI
                   ShopDropHandler (판매)
```

데이터 두 컬렉션은 분리, UI 도 분리. 각자 자기 컬렉션만 순회.

### 신 구조
```
ShopSystem
├─ _ownedSkills    : List<SkillData>           (데이터 ground truth — 유지)
├─ _ownedSupports  : List<SupportOptionData>   (데이터 ground truth — 유지)
└─ _displayOrder   : List<DisplayEntry>        (표시 순서 메타데이터 — 신규)
    DisplayEntry = { Kind: Skill|Support, DataIndex: int }
                       │
                       ▼
              UnifiedInvenUI  (단일 그리드, 동적 슬롯)
                       │
                       ▼
              UnifiedInvenSlotDragHandler  (스킬/서포트 양쪽 페이로드)
                       │
                       ▼
              SkillSlotUI / SupportSlotUI  (drop 시 Kind 검사 — 부적합 거부)
              ShopDropHandler              (Kind 무관 환급)
```

### 핵심 결정
- **ShopSystem 의 두 List 는 그대로 유지** — 호환성, 회귀 최소화
- `_displayOrder` 가 그리드 위치 ↔ (Kind, DataIndex) 매핑을 책임 → cross-type 자유 재배치 가능
- `_displayOrder` 항목 = `DisplayEntry { InventoryItemKind Kind; int DataIndex }` 
- 데이터 인덱스 변경 (예: 중간 스킬 제거) 시 displayOrder 의 뒤 항목 DataIndex 보정
- 시각 구분 없음 — 슬롯이 SkillData 인지 SupportOptionData 인지 표시하지 않음 (아이콘만 그대로)

### 데이터 흐름
```
[구매]  ShopSystem.BuySkill / BuySupportOption
          → 해당 데이터 List append
          → _displayOrder.Add(new DisplayEntry(Kind, dataIndex))
          → OnInventoryChanged

[표시]  UnifiedInvenUI.Refresh
          → _displayOrder 순회
          → entry.Kind 로 SkillData 또는 SupportOptionData 가져옴
          → 슬롯 동적 생성/풀링 후 아이콘 바인딩

[재배치] UnifiedInvenSlotDragHandler.OnDrop (인벤 내부)
          → ShopSystem.SwapDisplayOrder(srcIdx, dstIdx)
          → OnInventoryChanged

[장착]  UnifiedInvenSlotDragHandler.OnDrop (타워 슬롯)
          → 타겟 슬롯의 expected Kind 와 페이로드 Kind 비교
          → 일치: 기존 EquipSkill / 서포트 장착 경로
          → 불일치: 시각 거부 (DropTargetHighlight)

[판매]  ShopDropHandler.OnDrop
          → 페이로드 Kind 별로 SellSkill / SellSupport 분기 (기존과 동일)
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|---|---|
| `MakeDefence/Assets/Scripts/Systems/ShopSystem.cs` | `DisplayEntry` 중첩 struct + `_displayOrder` 필드 + `OwnedDisplayOrder` 노출 + `SwapDisplayOrder`, `MoveDisplayOrder` 추가. 기존 `BuySkill`/`BuySupportOption`/`ReturnSkill`/`ReturnSupportOption`/`RemoveOwnedSkill`/`RemoveOwnedSupportOption` 가 `_displayOrder` 동기화. 기존 `SwapOwnedSkills`/`MoveOwnedSkill` 는 호환 유지하되 deprecated 표시 (UI 가 새 API 로 이행) |
| `MakeDefence/Assets/Scripts/UI/InvenUI.cs` | 통합 그리드로 재작성 — `_displayOrder` 순회, 동적 슬롯 인스턴스화 (LayoutGroup + 슬롯 프리팹), 슬롯 풀링 |
| `MakeDefence/Assets/Scripts/UI/SupportInvenUI.cs` | **삭제** (UnifiedInvenUI 가 흡수). 컴포넌트가 씬에 남아있을 가능성 대비 Obsolete 빈 클래스로 한 PR 더 두는 것 고려 |
| `MakeDefence/Assets/Scripts/UI/InvenSlotDragHandler.cs` | `Skill` 단일 필드 → `SkillData Skill` + `SupportOptionData Support` (둘 중 하나만 set), `InventoryItemKind Kind` getter. `OnDrop` 의 인벤 ↔ 인벤 분기를 `SwapDisplayOrder` 호출로 교체 |
| `MakeDefence/Assets/Scripts/UI/SupportOptionDragHandler.cs` | **삭제** — InvenSlotDragHandler 에 흡수. 참조하는 모든 호출자가 InvenSlotDragHandler 로 이행 (아래 두 파일 포함) |
| `MakeDefence/Assets/Scripts/UI/SkillSlotUI.cs` | `OnDrop` 시 페이로드의 Kind 검사 — Skill 만 허용. Support 페이로드면 거부 |
| `MakeDefence/Assets/Scripts/UI/SupportSlotUI.cs` | `SupportOptionDragHandler` 의존 제거 → `InvenSlotDragHandler` 사용. `OnDrop` 에서 Kind == Support 만 허용 |
| `MakeDefence/Assets/Scripts/UI/OwnedSupportSlotUI.cs` | `SupportOptionDragHandler` 추가/사용 부분을 `InvenSlotDragHandler` (Kind = Support) 로 이행. 타워 장착 슬롯에서 인벤으로 드래그 해제 동작 유지 |
| `MakeDefence/Assets/Scripts/UI/InvenDropHandler.cs` | `GetComponent<SupportOptionDragHandler>()` 분기 → `GetComponent<InvenSlotDragHandler>()` + `Kind == Support` 검사로 이행. 서포트 해제 후 인벤 반환 로직은 유지 |
| `MakeDefence/Assets/Scripts/UI/DropTargetHighlight.cs` | 드래그 시작 이벤트의 페이로드 Kind 에 따라 호환되는 슬롯만 하이라이트 |
| `MakeDefence/Assets/Scripts/UI/ShopDropHandler.cs` | `GetComponent<SupportOptionDragHandler>()` → 통합 핸들러 + Kind 검사. SellSkill / SellSupport 분기 유지 |
| `MakeDefence/Assets/Scenes/SampleScene.unity` | **사용자 Editor 작업** — 두 인벤 패널 → 단일 그리드 패널, 슬롯 프리팹 연결, LayoutGroup 설정 |

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|---|---|
| `MakeDefence/Assets/Scripts/Systems/InventoryItemKind.cs` | `enum InventoryItemKind { Skill, Support }` — ShopSystem 과 드래그 핸들러 공유 |
| (ShopSystem 내부) `DisplayEntry` struct | `{ InventoryItemKind Kind, int DataIndex }` — 표시 순서 매핑 |
| `MakeDefence/Assets/Perfab/UI/UnifiedInvenSlot.prefab` | **사용자 Editor 작업** — 슬롯 1개 프리팹 (Image ICON + DragHandler) |

## 4. 테스트 계획

### 구매 / 표시
- [ ] 스킬 구매 → 통합 인벤 끝에 표시
- [ ] 서포트 구매 → 같은 그리드 다음 슬롯에 표시
- [ ] 구매 순서대로 슬롯이 채워짐
- [ ] 보유 0개 → 그리드 비어있음, 슬롯 0개

### 장착 (Kind 기반 드롭 검사)
- [ ] 스킬 아이콘 → 타워 메인 스킬 슬롯: 정상 장착
- [ ] 스킬 아이콘 → 타워 서포트 슬롯: 드롭 거부 (시각 차단)
- [ ] 서포트 아이콘 → 타워 메인 슬롯: 드롭 거부
- [ ] 서포트 아이콘 → 타워 서포트 슬롯: 정상 장착

### 자유 재배치
- [ ] 스킬 A ↔ 스킬 B 슬롯 swap
- [ ] 서포트 A ↔ 서포트 B 슬롯 swap
- [ ] **스킬 ↔ 서포트 cross-type swap** (핵심)
- [ ] 빈 슬롯으로 이동 (Move)
- [ ] 재배치 후 ShopSystem.OwnedSkills / OwnedSupports 의 데이터는 변경 없음 (순서 메타만 변경)

### 동적 확장 / 축소
- [ ] 보유 1 → 2 → 3 늘어날 때 그리드 슬롯이 자동 추가
- [ ] 판매로 줄어들 때 빈 슬롯이 자동 축소 (또는 마지막 한 줄만 보존하는 정책)
- [ ] LayoutGroup 가 정상 갱신 (overlap / clipping 없음)

### 판매
- [ ] 스킬 아이콘 → ShopDropHandler: SellSkill 호출, 큐브 환급
- [ ] 서포트 아이콘 → ShopDropHandler: SellSupport 호출, 큐브 환급
- [ ] 판매 후 `_displayOrder` 에서 해당 항목 제거, 뒤 항목 DataIndex 보정

### 회귀
- [ ] 타워 EquippedSkill / SupportSlots 동작 변경 없음
- [ ] 큐브 환급 로직 변경 없음
- [ ] `OwnedSkillsListUI` / `OwnedSupportListUI` 동작 회귀 없음
- [ ] `OwnedSupportSlotUI` — 통합 드래그 핸들러로 이행 후에도 \"장착된 서포트 → 인벤으로 드래그 해제\" 시나리오 정상
- [ ] `InvenDropHandler` — \"장착된 스킬/서포트 → 인벤으로 드래그\" 양쪽 모두 정상 동작
- [ ] 컴파일 에러 없음: `SupportOptionDragHandler` 삭제 후 모든 참조 (`SupportSlotUI`, `OwnedSupportSlotUI`, `InvenDropHandler`, `ShopDropHandler`) 가 통합 핸들러로 이행됨

## 5. 위험 요소

### 데이터 정합성
- **DataIndex 보정 버그 위험**: `RemoveOwnedSkill(skill)` 가 List 중간 항목을 제거하면 그 뒤 `DisplayEntry.DataIndex` 가 모두 -1 되어야 함. 보정 누락 시 다른 항목을 가리키는 댕글링 인덱스 발생
- 완화: ShopSystem 의 모든 데이터 변경 메서드에서 `_displayOrder` 보정을 강제하는 단일 진입점(`AddOwned(Kind, item)` / `RemoveOwned(Kind, item)`) 도입 검토. 외부 호출 메서드는 wrapper 로 위임
- 테스트: 중간 인덱스 제거 시나리오를 명시적으로 검증

### 호환성
- 기존 `SwapOwnedSkills(int, int)` / `MoveOwnedSkill(int, int)` 의 인덱스 의미가 \"스킬 List 인덱스\" 에서 변하지 않음 (그대로 유지). 새 \"통합 displayOrder 인덱스\" 와 혼동하지 않도록 메서드명 분리: `SwapDisplayOrder(int, int)` vs `SwapOwnedSkills(int, int)`
- 기존 호출자 (InvenSlotDragHandler 가 `SwapOwnedSkills` 호출하던 부분) 는 새 API 로 이행

### SupportOptionDragHandler 삭제 시 전수 마이그레이션 (Codex P2 지적)
삭제 전 다음 6개 참조 위치를 모두 이행해야 컴파일 가능:
1. `SupportSlotUI.cs:16, 20-21, 59` — drop 타겟 (타워 서포트 슬롯)
2. `OwnedSupportSlotUI.cs:9, 14-15` — 장착된 서포트 표시 (UnitPanel)
3. `InvenDropHandler.cs:23` — 장착 해제 → 인벤 반환
4. `ShopDropHandler.cs:27` — 판매
5. `SupportInvenUI.cs:9, 24-25` — 본 PR 에서 삭제
6. `SupportOptionDragHandler.cs` 자체

모두 `InvenSlotDragHandler` + `Kind == Support` 로 이행. 호환 wrapper 두는 옵션도 있지만, 코드 단순화를 위해 전수 이행 권장

### 씬 작업
- SampleScene 의 두 인벤 패널 제거 + 단일 그리드 패널 + 슬롯 프리팹 연결은 **Unity Editor 에서 사용자가 수행**. 코드 PR 머지만으로는 게임 동작 안 됨
- 완화: 플랜과 PR body 에 \"Editor 작업 필요\" 명시. SupportInvenUI 컴포넌트가 씬에 남아있으면 컴파일 에러 → 같이 정리 필요

### 동적 슬롯 생성
- LayoutGroup + dynamic Instantiate 는 매 OnInventoryChanged 마다 비용 발생. 보유 수가 적으니 (~20개) 실측 비용은 무시 가능하지만, 풀링 패턴 적용 권장 (slot prefab 재사용)

### Out of Scope (이번 PR 에서 안 함)
- IInventoryItem 인터페이스 (#236) — 향후 별도 PR
- 슬롯 정렬 자동 옵션 (현재는 자유 재배치만)
- 다중 선택 / 일괄 이동
- 인벤 카테고리 필터 (Kind 별 보기)
