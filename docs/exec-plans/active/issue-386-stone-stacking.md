# Issue #386 — 차원석 인벤토리 슬롯 스택 기능 (동일 옵션 차원석 겹치기)

## 1. 시스템 구조

### 현재 구조

- `DimensionStone` (MakeDefence.Rift.Core asmdef): 옵션(종류+수치) 최대 6개를 가진 순수 C# 클래스. `CreateRandom()` / `Clone()` / 큐브 조작 메서드 보유. 동등성 비교 수단 없음 (참조 비교만 가능).
- `ShopSystem._ownedStones : List<DimensionStone>`: 차원석 1개 = 리스트 항목 1개 = `_displayOrder`의 `DisplayEntry` 1개 = 인벤 슬롯 1칸.
- `InvenUI`: `_displayOrder` 순서대로 슬롯을 채움. 모든 차원석은 동일 아이콘/이름("Dimension Stone")으로 표시 — 옵션 차이가 UI에 드러나지 않음. `TryRegisterSlot()`이 슬롯 하위 TMP 텍스트를 전부 비활성화함.
- 차원석 유입 경로: 적 드랍(`DroppedStoneSystem`), 초기 지급(`ShopSystem.Awake`), Clone 큐브(`WaveGeneratorSystem.ApplyCube`), 웨이브 생성기 회수(`InventorySystem.EquipStone` swap / `TryUnloadStone`).
- 차원석 소비 경로: 클릭/드래그 장착(`InventorySystem.EquipStone` → `ShopSystem.RemoveStone`), 연속 생성(`RepeatGenerateToggleButton` → `OwnedStones[0]`), `TestRunner`.

### 변경 후 구조

차원석 저장을 **스택 단위**로 전환한다:

- `ShopSystem` 내부에 `StoneStack { DimensionStone Stone; int Count }` 도입. `_ownedStones`를 `List<StoneStack>`으로 교체.
- **스택 키 = 옵션 구성 동등성**: `DimensionStone.HasSameOptions(other)` 신설 — 옵션은 타입 중복이 없으므로(생성 로직이 보장) 타입 정렬 후 (Type, Value) 쌍 전체 일치로 판정.
- `AddStone(stone)`: 동일 옵션 스택 존재 → `Count++` (DisplayEntry 추가 없음). 없으면 새 스택 + 새 DisplayEntry.
- `RemoveStone(stone)` / `RemoveByDisplayIndex(idx)`: `Count--`, 0이 되면 스택과 DisplayEntry 제거.
- **참조 공유(aliasing) 차단**: `Count > 1` 상태에서 1개를 꺼낼 때, 스택 대표 인스턴스를 `Clone()`으로 교체한다. 꺼낸 인스턴스는 호출자(WaveGeneratorSystem)가 단독 소유 → 큐브 조작(Reroll/Upgrade 등)이 남은 스택에 영향을 주지 않음.
- `OwnedStones`(IReadOnlyList<DimensionStone>)는 **스택 대표들의 리스트**로 의미가 바뀜. 기존 호출자(`RepeatGenerateToggleButton`, `TestRunner`)는 `Count > 0` 체크와 `[0]` 접근만 하므로 시맨틱 변화 없이 동작 (스택 대표를 꺼내면 `RemoveStone`이 decrement 처리).
- `InvenUI`: `DisplayItem`에 `StackCount` 추가. 슬롯마다 카운트 텍스트(TMP, 숫자만 — 한글 폰트 불필요)를 동적 생성해 `Count >= 2`일 때 `xN` 표시.

### 데이터 흐름

```text
차원석 획득 (드랍 / Clone 큐브 / 생성기 회수)
 ↓
ShopSystem.AddStone → HasSameOptions 로 기존 스택 탐색
 ├─ 있음: StoneStack.Count++          (슬롯 수 불변)
 └─ 없음: 새 StoneStack + DisplayEntry (새 슬롯)
 ↓
OnInventoryChanged → InvenUI.Refresh → 슬롯 아이콘 + xN 카운트 표시
 ↓
장착 (클릭 / 드래그 / 연속생성) → RemoveStone
 ├─ Count > 1: Count--, 대표를 Clone 으로 교체 (꺼낸 인스턴스 단독 소유)
 └─ Count == 1: 스택 + DisplayEntry 제거
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|---|---|
| `MakeDefence/Assets/Scripts/Gameplay/Rift/Core/DimensionStone.cs` | `HasSameOptions(DimensionStone other)` 동등성 판정 메서드 추가 |
| `MakeDefence/Assets/Scripts/Systems/ShopSystem.cs` | `_ownedStones`를 스택 리스트로 교체, `AddStone`/`RemoveStone`/`RemoveByDisplayIndex`/`GetDisplayItem`/`OwnedItems`/`FindStoneDisplayIndex` 스택 대응, `DisplayItem.StackCount` 추가, 총 보유량 `TotalStoneCount` 프로퍼티 추가 |
| `MakeDefence/Assets/Scripts/UI/InvenUI.cs` | 슬롯별 카운트 TMP 텍스트 동적 생성/갱신 (`Count >= 2`일 때 `xN`), `TryRegisterSlot`의 TMP 일괄 비활성화에서 카운트 텍스트 제외 |
| `MakeDefence/Assets/Tests/EditMode/Rift/DimensionStoneTests.cs` | `HasSameOptions` 테스트 추가 |

## 3. 신규 클래스 / 파일

- `ShopSystem.StoneStack` (ShopSystem 내부 private 클래스): 대표 `DimensionStone` + `Count`. 별도 파일 없음 — DimensionStone 은 Rift.Core asmdef 거주로 Assembly-CSharp 타입을 참조할 수 없어 스택 컨테이너는 ShopSystem 쪽에 둔다 (기존 `StoneInventoryItem` 어댑터와 같은 패턴).
- 신규 파일 없음 (기존 파일 내 추가로 충분).

## 4. 테스트 계획

### EditMode 테스트

- `HasSameOptions`: 동일 옵션(순서 무관) → true / 수치 다름 → false / 옵션 수 다름 → false / `Clone()` 결과 → true

### 수동 테스트 (Unity Editor)

1. 초기 차원석 + Clone 큐브 반복 → 동일 옵션 차원석이 한 슬롯에 `xN`으로 겹치는지
2. 스택 슬롯 클릭 장착 → 개수 1 감소, 슬롯 유지 (`x2` → 아이콘만)
3. 장착된 차원석을 Lower 큐브로 Reroll → **인벤에 남은 스택 옵션이 변하지 않는지** (aliasing 검증)
4. Reroll 된 차원석을 인벤으로 회수 → 옵션이 달라졌으므로 새 슬롯 생성 확인
5. 연속 생성 모드(RepeatGenerate)에서 스택이 1개씩 소비되는지
6. 스택 슬롯 드래그 재배치(swap/move) 정상 동작
7. 적 처치 드랍 차원석(옵션 1개 랜덤)이 동일 옵션 스택에 합류하는지

## 5. 위험 요소

- **참조 공유(aliasing)**: 스택에서 꺼낸 인스턴스와 스택 대표가 같은 참조면 큐브 조작이 스택 전체를 오염시킴. `RemoveStone` 시 대표 Clone 교체로 차단 — 이 처리가 핵심이며 테스트 3번으로 검증.
- **`OwnedStones` 시맨틱 변경**: 이제 "차원석 인스턴스 목록"이 아니라 "스택 대표 목록". `OwnedStones.Count`는 스택 수 ≠ 총 보유량. 총량이 필요한 곳이 생기면 `TotalStoneCount` 사용. 현재 호출자는 존재 여부/첫 항목만 사용해 영향 없음 — 구현 시 재확인.
- **InvenUI 카운트 텍스트**: `TryRegisterSlot`이 TMP 텍스트를 일괄 비활성화하므로, 동적 생성한 카운트 텍스트가 다시 꺼지지 않도록 생성 시점/이름 규칙 주의. 숫자만 표시하므로 한글 폰트 문제 없음.
- **씬 변경 최소화**: 카운트 텍스트를 코드에서 동적 생성하므로 씬/프리팹 수정 불필요 (UnityMCP 씬 편집 리스크 회피).
- **수치 동등성**: 옵션 수치는 `Mathf.Round`로 정수화되어 float 오차 문제는 낮지만, Upgrade(×1.5) 경로는 소수값 가능 → `Mathf.Approximately`로 비교.
- **EditMode 테스트 범위**: ShopSystem 은 Assembly-CSharp 거주 — 기존 EditMode 테스트 asmdef 가 Assembly-CSharp 을 참조하지 않으면 ShopSystem 스택 동작은 수동 테스트로 대체 (구현 시 asmdef 확인).
