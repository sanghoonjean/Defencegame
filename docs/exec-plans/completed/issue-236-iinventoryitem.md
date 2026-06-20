# Issue #236 — 인벤토리 데이터 통합 (IInventoryItem 공통 인터페이스)

## 1. 시스템 구조

### 배경
- `SkillData` / `SupportOptionData` 가 공통 베이스 없는 `ScriptableObject` 자매 클래스.
- `ShopSystem` 은 `_ownedSkills` / `_ownedSupports` 두 평행 `List<T>` 와 표시 순서 메타 `_displayOrder` 를 보유.
- #220 작업으로 표시 측 추상화 `DisplayItem` (struct) 가 이미 존재해 `Icon` / `DisplayName` 을 통합 access 하지만, 이는 ShopSystem 내부 wrapping 수준이고 ScriptableObject 자체에는 공통 인터페이스가 없음.

### 변경 후 구조

```
IInventoryItem  (interface, neutral location)
├─ string DisplayName { get; }
├─ Sprite Icon        { get; }
└─ InventoryItemKind Kind { get; }     ← 드롭 타겟 필터링용 (Skill / Support)

SkillData         : ScriptableObject, IInventoryItem   ← Kind => Skill
SupportOptionData : ScriptableObject, IInventoryItem   ← Kind => Support

ShopSystem
├─ _ownedSkills    : List<SkillData>          ← 데이터 ground truth (유지)
├─ _ownedSupports  : List<SupportOptionData>  ← 데이터 ground truth (유지)
├─ _displayOrder   : List<DisplayEntry>       ← 그리드 위치 메타 (유지)
└─ OwnedItems      : IEnumerable<IInventoryItem>   ← NEW: 통합 뷰 (displayOrder 순)
```

### 핵심 결정
- **저장 컬렉션은 합치지 않음** — `_ownedSkills` / `_ownedSupports` 그대로. 호출 지점이 많아 별도 PR 의 영역.
- **하위호환 100%** — `OwnedSkills`, `OwnedSupports`, `GetDisplayItem(int)`, `RemoveByDisplayIndex(int)`, `SwapDisplayOrder`, `MoveDisplayOrder` 등 기존 API 전부 시그니처/동작 유지.
- **`DisplayItem` struct 는 유지** — #220 의 InvenUI 가 `DisplayItem.Skill` / `DisplayItem.Support` null-체크 분기로 작동 중. 인터페이스 도입과 무관하게 그대로 둠. (단, `DisplayItem.Item` 같은 `IInventoryItem` access 헬퍼는 선택 추가)
- **`Kind` 는 인터페이스 멤버** — `is SkillData` 패턴 매칭은 핫패스 (Refresh/Drop) 에서 reflection 비용 발생 가능. enum read 가 더 가볍고 InvenUI 분기 코드와 일관.
- **enum 식별자 통합 안 함** — `SkillType`, `SupportOptionType` 그대로. `InventoryItemKind` 와 다른 축.

## 2. 수정 파일

| 파일 | 변경 |
|------|------|
| `MakeDefence/Assets/Scripts/Gameplay/Tower/SkillData.cs` | `: ScriptableObject, IInventoryItem` 선언. `DisplayName`, `Icon`, `Kind` 명시적 구현 (기존 `displayName`/`icon` 필드 그대로, property 가 위임) |
| `MakeDefence/Assets/Scripts/Gameplay/Tower/SupportOptionData.cs` | 동일 패턴으로 인터페이스 구현 |
| `MakeDefence/Assets/Scripts/Systems/ShopSystem.cs` | `IEnumerable<IInventoryItem> OwnedItems` 신규 — `_displayOrder` 순회하며 `GetDisplayItem(i)` 로 `SkillData`/`SupportOptionData` yield. (선택) `DisplayItem` 에 `IInventoryItem Item` access 헬퍼 추가. |

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `MakeDefence/Assets/Scripts/Systems/IInventoryItem.cs` | 인터페이스 정의 — `DisplayName`, `Icon`, `Kind` |

### 위치 결정
- `InventoryItemKind.cs` 가 이미 `Systems/` 에 있으므로 `IInventoryItem.cs` 도 같이 둠. Gameplay/Tower 의 ScriptableObject 가 Systems 의 인터페이스를 구현하는 방향 (인벤은 시스템 레이어, 데이터는 게임플레이 레이어로 의존성 맞음).

## 4. 테스트 계획

### 컴파일 / 인터페이스 구현
- [ ] `SkillData` / `SupportOptionData` 모두 `IInventoryItem` 으로 캐스팅 가능
- [ ] `(SkillData) so).Kind == InventoryItemKind.Skill`
- [ ] `(SupportOptionData) so).Kind == InventoryItemKind.Support`
- [ ] `DisplayName`, `Icon` 이 기존 `displayName`, `icon` 필드와 동일한 값 반환

### ShopSystem 통합 뷰
- [ ] 스킬 2개 + 서포트 2개 보유 + cross-type 재배치 → `OwnedItems` 가 `_displayOrder` 순서 그대로 반환
- [ ] 빈 인벤 → `OwnedItems` 가 empty 시퀀스
- [ ] 판매 / 장착 / `MoveDisplayOrder` / `SwapDisplayOrder` 후 `OwnedItems` 가 최신 상태 반영

### 회귀 (하위호환)
- [ ] `OwnedSkills` / `OwnedSupports` 리스트 그대로 동작 — `OwnedSkillsListUI`, 기존 호출자 영향 없음
- [ ] `GetDisplayItem(int)`, `RemoveByDisplayIndex`, `SwapDisplayOrder`, `MoveDisplayOrder` 시그니처/동작 변경 없음
- [ ] InvenUI / SupportSlotUI / SkillSlotUI / ShopDropHandler 코드 변경 없이 정상 동작
- [ ] #220 의 cross-type swap, 중복 보유 데이터 무결성 회귀 없음

## 5. 위험 요소

### 낮음 — 인터페이스 단순 도입
- `IInventoryItem` 가 `DisplayName`/`Icon`/`Kind` 만 요구하므로 `SkillData`/`SupportOptionData` 변경은 라인 단위로 가벼움. 기존 필드/메서드/Inspector 직렬화 영향 없음.

### Unity Editor / .meta
- `SkillData`, `SupportOptionData` 는 ScriptableObject 라 인스턴스 에셋 (`*.asset`) 이 다수 존재. 클래스에 인터페이스만 추가하는 변경은 GUID/직렬화에 영향 없음 (필드 추가/삭제 아님). 기존 에셋 그대로 사용 가능.
- 신규 `IInventoryItem.cs` 의 `.meta` 는 Unity Editor 가 자동 생성 — Claude 가 직접 만들지 않음.

### 사이드 이펙트 — 드래그 핸들러 페이로드 (의도적 제외)
- 현재 `InvenSlotDragHandler.Skill` / `Support` 두 필드. `IInventoryItem` 단일 필드로 교체 가능하지만 이번 PR scope 밖. 호출 지점 (`SupportSlotUI`, `SkillSlotUI`, `InvenUI`, `OwnedSupportSlotUI`, `InvenDropHandler`, `ShopDropHandler` 등) 광범위해 별도 후속 PR 로 분리. #236 본문의 "예상 변경 영역" 의 "(선택) 드래그 핸들러 페이로드 변경" 도 선택 항목 표기.

### 미정 항목 — 결정안
- **인터페이스 이름**: `IInventoryItem` 채택. 이슈 본문 표기와 일치하고 인벤 표시까지 포함하는 의미 범위 적절. `IEquipable` 은 향후 장착 전용 추상화 필요 시 분리 도입.
- **`Kind` 위치**: 인터페이스 멤버. (위 "핵심 결정" 참조)
- **`InventoryItemKind` 위치**: 이미 `Systems/` 에 존재. 그대로 유지.

### Out of Scope
- 저장 컬렉션 통합 (`List<IInventoryItem>` 단일화) — 호출 지점이 많고 #220 직후 또 다른 큰 refactor 라 분리.
- `Tower.EquippedSkill` / `_supportSlots[5]` 슬롯 구조 변경.
- 드래그 핸들러 페이로드 추상화 (위 "사이드 이펙트" 참조).
- 큐브 환급/구매 로직.
