# Issue #310 — 차원석 인벤토리와 일반 인벤토리 통합

## 1. 시스템 구조

### Before

```
ShopSystem (skill/support)            DimensionStoneInventory (stone)
   ├─ _ownedSkills / _ownedSupports         ├─ _stones
   ├─ _displayOrder (Kind = Skill|Support)  ├─ OnInventoryChanged
   └─ OnInventoryChanged                    └─ initialStones
        │                                        │
   InvenUI                              DimensionStoneInventoryView
   InvenSlotDragHandler                 DimensionStoneSlot
   InvenDropHandler                     DimensionStoneInventoryDropTarget
```

스킬/서포트는 `ShopSystem` 한 곳에서 `IInventoryItem` / `InventoryItemKind` 기반으로 통합 관리되고 있다.
차원석만 별도 시스템 + 뷰 + 슬롯 + 드롭 타깃을 가진다.

### After

```
ShopSystem
   ├─ _ownedSkills / _ownedSupports / _ownedStones
   ├─ _displayOrder (Kind = Skill|Support|Stone)
   ├─ stoneIcon (직렬화된 공용 sprite, 차원석은 ScriptableObject 아님)
   └─ OnInventoryChanged
        │
   InvenUI       — Stone Kind 도 함께 그리드 표시
   InvenSlotDragHandler
        ├─ Stone 페이로드 (DimensionStone)
        └─ Rift GenerateSlot 로 드롭하면 swap 장착
   InvenDropHandler
        └─ 현재 LoadedStone 회수 (GenerateSlotDropTarget 드래그 → 인벤 배경)
```

`DimensionStoneInventory` / `DimensionStoneInventoryView` / `DimensionStoneSlot` / `DimensionStoneInventoryDropTarget` 4 개 클래스 삭제.

### 데이터 흐름

```
드랍 픽업 → DroppedStoneSystem ─► ShopSystem.AddStone(DimensionStone)
                                       │
                                       ▼
                                 _ownedStones + _displayOrder
                                       │
                                 OnInventoryChanged
                                       │
                                       ▼
                                  InvenUI.Refresh
                                       │
                                  ┌────┴─────┐
                       (드래그)               (클릭)
                          │                       │
                          ▼                       ▼
                 GenerateSlotDropTarget    InvenSlotDragHandler → ShopSystem.RemoveByDisplayIndex
                          │                       │
                          ▼                       ▼
                 RiftGenerator.SetStone   InventorySystem.SelectedRift → SetStone
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|---|---|
| `MakeDefence/Assets/Scripts/Systems/InventoryItemKind.cs` | `Stone` enum 추가 |
| `MakeDefence/Assets/Scripts/Systems/IInventoryItem.cs` | (변경 없음 — 인터페이스 유지) |
| `MakeDefence/Assets/Scripts/Systems/ShopSystem.cs` | `_ownedStones`, `AddStone`, `RemoveStone`, `OwnedStones`, Display* 메서드의 Stone 분기 추가. `[SerializeField] Sprite stoneIcon` 추가. `DisplayItem` 에 `Stone` 필드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Rift/Core/DimensionStone.cs` | `IInventoryItem` 구현 — `DisplayName`("차원석" 또는 옵션 첫 글자), `Icon` (ShopSystem.StoneIcon), `Kind = Stone` |
| `MakeDefence/Assets/Scripts/UI/InvenUI.cs` | `DisplayItem.Stone` 경로 처리. 클릭 장착: Stone 일 때 SelectedRift 에 `EquipToRift` (기존 swap 로직). drag 핸들러에 `Stone` 페이로드 셋업 |
| `MakeDefence/Assets/Scripts/UI/InvenSlotDragHandler.cs` | `DimensionStone Stone` 프로퍼티. `HasItem`/`Icon`/`Kind` 확장. `OnDrop` 에서 Stone 출처 인벤 슬롯 ↔ 인벤 슬롯 swap 도 동작. (Stone → Rift 는 `GenerateSlotDropTarget.OnDrop` 에서 받음) |
| `MakeDefence/Assets/Scripts/UI/InvenDropHandler.cs` | `GenerateSlotDropTarget` 출처 드래그 (현재 LoadedStone 회수) 추가 — 기존 `DimensionStoneInventoryDropTarget` 로직 이전 |
| `MakeDefence/Assets/Scripts/UI/GenerateSlotDropTarget.cs` | `OnDrop` 에서 `DimensionStoneSlot` 대신 `InvenSlotDragHandler`(Stone 페이로드) 도 받도록. 기존 `DimensionStoneSlot` 분기 제거 |
| `MakeDefence/Assets/Scripts/UI/RiftGeneratorPanel.cs` | 인벤 카운트/`LoadNextStone`/`UnloadStone` 을 `ShopSystem` 의 stone 컬렉션 기반으로 변경. 구독 이벤트도 `DimensionStoneInventory.OnInventoryChanged` → `ShopSystem.OnInventoryChanged` |
| `MakeDefence/Assets/Scripts/UI/RepeatGenerateToggleButton.cs` | 동일 — `ShopSystem` 기반 stone 카운트/소비 |
| `MakeDefence/Assets/Scripts/Systems/DroppedStoneSystem.cs` | `DimensionStoneInventory.Instance.Add(stone)` → `ShopSystem.Instance.AddStone(stone)` |
| `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftGenerator.cs` | Clone 큐브 분기 `DimensionStoneInventory.Instance.Add(LoadedStone.Clone())` → `ShopSystem.Instance.AddStone(...)`. `Remove` 도 마찬가지 |
| `MakeDefence/Assets/Scripts/TestRunner.cs` | `DimensionStoneInventory.Instance.Stones[0]` 등 참조 갱신 |
| `MakeDefence/Assets/Tests/EditMode/Rift/DimensionStoneTests.cs` | (필요 시) 시스템 의존 부분 갱신 |
| `MakeDefence/Assets/Scenes/SampleScene.unity` | DimensionStoneInventory GameObject 제거, DimensionStoneInventoryView GameObject 제거, InvenUI 의 slot prefab/풀 크기 조정 (UnityMCP 로 처리) |

## 3. 신규 클래스 / 파일

- 신규 클래스 없음. 기존 `ShopSystem` 을 확장.
- 4개 클래스 **삭제**:
  - `MakeDefence/Assets/Scripts/Systems/DimensionStoneInventory.cs`
  - `MakeDefence/Assets/Scripts/UI/DimensionStoneInventoryView.cs`
  - `MakeDefence/Assets/Scripts/UI/DimensionStoneSlot.cs`
  - `MakeDefence/Assets/Scripts/UI/DimensionStoneInventoryDropTarget.cs`

### 핵심 설계 결정

- **차원석 sprite 공급원**: `DimensionStone` 은 ScriptableObject 아님 → `IInventoryItem.Icon` 을 어떻게 제공할지 문제. → `ShopSystem` 에 `[SerializeField] Sprite stoneIcon` 추가 + static 접근자 (`ShopSystem.Instance.StoneIcon`) 노출. `DimensionStone.IInventoryItem.Icon` 이 그 값을 반환. 인스펙터에서 지정.
- **display order 의 Stone 항목 식별**: 기존 `_ownedSkills` / `_ownedSupports` 와 동일 패턴으로 `_ownedStones` 인덱스를 `DisplayEntry.DataIndex` 가 가리킨다. 같은 stone 이 중복 보유될 수 있어 인덱스 기반 식별이 정확.
- **Stone 클릭 장착 동작**: SelectedRift 가 있으면 기존 `DimensionStoneSlot.EquipToRift` 와 동일하게 swap. SelectedRift 가 없으면 아무것도 안 함 (기존과 동일).
- **Stone 드래그 ghost**: `InvenSlotDragHandler` 의 기존 ghost 사이즈/sprite 로직 그대로. 자식 ICON Image 색은 흰색(채움) 동일.
- **Stone → Rift 드롭 호환**: 기존 `GenerateSlotDropTarget.OnDrop` 은 `DimensionStoneSlot` 컴포넌트를 찾는다. 새 인벤 슬롯에는 `InvenSlotDragHandler` 만 있으므로, `GenerateSlotDropTarget` 의 분기를 `InvenSlotDragHandler` 에서 `Stone` 페이로드를 읽는 형태로 변경.

## 4. 테스트 계획

### 자동 (EditMode)

- 기존 `DimensionStoneTests` 가 통과해야 함 — 시스템 호출이 있다면 `ShopSystem` 으로 마이그레이션.
- 통합 시점에 다음 시나리오용 신규 EditMode 테스트 추가:
  - `ShopSystem.AddStone` 후 `OwnedDisplayOrder` 의 마지막 항목이 `Stone` Kind 로 들어감
  - 스킬/서포트/차원석 혼합 보유 상태에서 `RemoveByDisplayIndex` 가 다른 Kind 의 DataIndex 를 깨뜨리지 않음
  - `SwapDisplayOrder` / `MoveDisplayOrder` 가 Stone 끼리 / Stone↔Skill 양쪽에서 정상

### 수동 (PlayMode in Unity Editor)

체크리스트:

- [ ] 게임 시작 직후 인벤에 초기 차원석 1 개가 표시된다 (기존 `initialStones=1` 동작 유지 — `ShopSystem` 으로 이동)
- [ ] Rift 선택 → 인벤 슬롯의 차원석 클릭 → Rift 에 장착, 인벤에서 제거
- [ ] Rift 선택 → 인벤 차원석 드래그 → GenerateSlot 위 드롭 → 장착
- [ ] 이미 장착된 상태에서 다른 차원석 드래그/클릭 → swap (기존 stone 인벤 복귀)
- [ ] 장착 stone 드래그 → 인벤 배경 위 드롭 → 회수
- [ ] 인벤 슬롯끼리 stone ↔ skill ↔ support 자유 드래그 재배치
- [ ] DroppedStone 픽업 시 stone 이 인벤에 추가됨
- [ ] Clone 큐브 적용 시 인벤에 stone 복제본 추가
- [ ] RepeatGenerate 토글: 인벤의 차원석을 순차 소진
- [ ] 인벤이 비면 RepeatGenerate / LoadNextStone 버튼 비활성

## 5. 위험 요소

- **씬 의존성 (Scene 마이그레이션)**: `SampleScene.unity` 안에 `DimensionStoneInventory` 와 `DimensionStoneInventoryView` 가 GameObject 로 살아 있다. 스크립트만 지우면 missing script 경고. UnityMCP 의 `manage_gameobject` / `manage_scene` 으로 정리 필요. 이건 구현 단계의 핵심 위험 — 메모리에 따르면 .unity / .prefab 직접 편집은 금지.
- **stoneIcon 미할당**: `ShopSystem.stoneIcon` 을 인스펙터에서 안 채워두면 인벤 슬롯이 빈 아이콘으로 보임. 구현 시 fallback (예: 빈 sprite 일 때 자식 ICON 의 기본 색을 사용) 검토 필요.
- **`DimensionStoneInventory.Instance` 동시 접근**: 다른 시스템 (`DroppedStoneSystem`, `RiftGenerator`, `RepeatGenerateToggleButton`) 이 `Instance` 를 null 체크하지만 빈도 높게 참조. 마이그레이션 누락 시 NullRef. 전체 grep + 치환으로 잡아야 함 (확인됨: 5 파일).
- **`OnInventoryChanged` 이벤트 통합**: 기존 차원석 변동 시점 (`Add`/`Remove`) 이 이제 `ShopSystem.OnInventoryChanged` 로 합류. `RiftGeneratorPanel` / `RepeatGenerateToggleButton` 등의 구독 채널 변경. 구독 누락 → UI 미갱신.
- **EquipToRift 의 위치**: 현재 `DimensionStoneSlot.EquipToRift` 가 static helper. 클래스 삭제 시 호출처 (`RepeatGenerateToggleButton.TryConsumeNext`) 가 깨짐. → `ShopSystem` 또는 `InventorySystem` 또는 `RiftGenerator` 로 이동.
- **drag/drop 회귀**: GenerateSlot ↔ 인벤 사이 swap 로직 (`DimensionStoneInventoryDropTarget` + `GenerateSlotDropTarget.OnBeginDrag`) 이 InvenDropHandler 와 InvenSlotDragHandler 로 분산되면서 race 위험. 테스트 체크리스트로 회귀 잡기.
- **자동 테스트 한도**: PlayMode 드래그/드롭 자동화는 미지원. 수동 체크리스트 의존.
