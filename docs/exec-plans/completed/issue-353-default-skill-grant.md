# Issue #353 — 유닛 최초 설치시 기본 스킬 지급 기능 추가

## 1. 시스템 구조

현재 `Tower.EquippedSkill`은 배치 직후 항상 `null`이며, 플레이어가 인벤토리 UI(`InventorySystem.EquipSkill`)를 통해
수동으로 장착해야만 `SkillDispatcher`가 공격 로직을 실행한다.

배치 흐름:

```
UnitSpawnButton.OnClick()
  → TowerPlacer.EnterPlacementMode(unitPrefab, OnUnitPlaced)
  → TowerPlacer.PlaceTower(coord)
      → Instantiate(_pendingPrefab)
      → tower.Place(coord)          // ← 최초 설치 시 유일하게 호출되는 훅
          → ItemSystem.RegisterTower(this)
          → OnTowerPlaced?.Invoke(this)
      → MapTileSystem.PlaceTower(...)
```

`Tower.Place()`는 최초 설치에서만 호출되고, 기존 타워를 옮기는 `Tower.MoveTo()`는
좌표/월드 위치만 갱신할 뿐 `Place()`를 다시 호출하지 않는다. 따라서 `Place()`에
기본 스킬 지급 로직을 넣으면 "최초 설치 1회"라는 요구사항이 자연히 만족된다.

`Tower.EquipSkill(SkillData skill)`은 `ShopSystem`의 보유(ownership) 목록과 무관하게
동작하는 순수 슬롯 대입 함수이므로, 기본 스킬 지급 시 별도의 보유 처리 없이
`EquipSkill`을 그대로 재사용할 수 있다.

유닛 타입 구분은 코드 레벨(서브클래스/enum)이 아닌 프리팹 단위(`Tower.prefab`,
`Tower_Unit1~4.prefab`)로 이루어져 있다. 따라서 "타입별 기본 스킬"은 프리팹마다
Inspector에서 지정하는 `[SerializeField]` 필드로 표현하는 것이 기존 패턴과 일치한다.

### 기본 스킬 vs 보유 스킬 구분 (Codex 리뷰 반영)

`EquippedSkill` 슬롯은 "플레이어가 상점에서 구매/보유한 스킬"과 "설치 시 무료로 지급된
기본 스킬"을 구분하지 않는다. 그런데 기존 코드는 `tower.EquippedSkill != null`인 경우
이를 항상 "보유 스킬이 장착된 것"으로 간주하고 아래 지점에서 인벤토리로 되돌리거나
판매 보상(하급 큐브 1개)을 지급한다.

- `InventorySystem.DeleteTower()` (`Systems/InventorySystem.cs:96-97`) — 타워 삭제 시 `ShopSystem.ReturnSkill(target.EquippedSkill)`
- `InvenDropHandler.cs:24-26` — 장착 슬롯 → 인벤 드래그 시 unequip 후 `ReturnSkill`
- `InvenSlotDragHandler.cs:96-98` — 위와 동일 경로(다른 드래그 핸들러)
- `InvenUI.cs:120-123` — 인벤 스킬 클릭 교체 시, 기존 장착 스킬을 `ReturnSkill` 후 새 스킬 장착
- `SkillSlotUI.cs:56-64` — 스킬 슬롯에 드래그로 교체 시 기존 장착 스킬을 `ReturnSkill`
- `SellConfirmPopup.cs:109-115, 126` — 장착 스킬 판매 확인 시 `UnequipSkill()` 후 무조건 하급 큐브 1개 지급
- `ShopDropHandler.cs:47-48` — 판매 팝업이 없을 때 폴백 경로, 역시 무조건 큐브 1개 지급

기본 스킬을 그대로 `EquipSkill(defaultSkill)`로 지급하면, 위 6개 지점 중 어디로든
"삭제/해제/교체/판매"만 하면 무료 스킬이 보유 목록에 추가되거나 큐브로 환전되어
반복 악용(스킬 복제, 큐브 무한 생성)이 가능해진다.

**해결 설계** — `Tower`에 장착 스킬의 출처를 구분하는 마커를 추가한다.

- `Tower.EquipSkill(SkillData skill, bool isDefault = false)`로 시그니처 확장
  (기존 호출부는 전부 `isDefault` 생략 → 동작 변화 없음).
- `public bool IsDefaultSkillEquipped { get; private set; }` 추가. `EquipSkill` 호출 시
  `isDefault` 값으로 갱신, `UnequipSkill()` 호출 시 `false`로 리셋.
- `Place(Vector2Int coord)`에서는 `EquipSkill(defaultSkill, isDefault: true)`로 호출.
- 위 6개 지점에서 "보유 목록 반환" 또는 "판매 보상 지급" 여부를 결정할 때 마커를 확인한다.

**⚠️ 순서 주의 (2차 Codex 리뷰 반영)** — `InvenDropHandler`/`InvenSlotDragHandler`/
`ShopDropHandler`(폴백)/`SellConfirmPopup.OnConfirm`은 기존 코드에서 `UnequipSkill()`을
먼저 호출한 뒤 `ReturnSkill`/큐브 지급을 수행한다. `UnequipSkill()`이 마커를 `false`로
리셋하므로, "언이퀴 이후에" 마커를 확인하면 이미 리셋된 값을 읽어 가드가 항상 무력화된다.
따라서 이 4곳은 **`UnequipSkill()`을 호출하기 전에** `bool wasDefault = tower.IsDefaultSkillEquipped;`로
값을 캡처해두고, 이후 반환/보상 여부를 `wasDefault`로 판단해야 한다.
(반면 `InvenUI.cs`/`SkillSlotUI.cs`/`BuildDeleteSummary`는 `UnequipSkill()`을 거치지 않고
`ReturnSkill`을 먼저 호출한 뒤 새 스킬을 장착하거나 그대로 끝나므로, 마커가 아직 리셋되지 않은
상태라 순서 문제가 없다.)

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
  - `[SerializeField] private SkillData defaultSkill;` 필드 추가
  - `public bool IsDefaultSkillEquipped { get; private set; }` 추가
  - `EquipSkill(SkillData skill, bool isDefault = false)`로 시그니처 확장, `UnequipSkill()`에서
    플래그 리셋
  - `Place(Vector2Int coord)`에서 `EquippedSkill == null`이고 `defaultSkill != null`인 경우
    `EquipSkill(defaultSkill, isDefault: true)` 호출 추가
- `MakeDefence/Assets/Scripts/Systems/InventorySystem.cs`
  - `BuildDeleteSummary()`: `skill` 계산을 `target.EquippedSkill != null && !target.IsDefaultSkillEquipped`로
    수정. `DeleteTower()`의 `summary.SkillReturned` 가드와 `TowerDeleteConfirmPopup`의 확인 문구
    ("스킬 1개 반환")가 이 값 하나로 함께 정확해진다 (별도 가드 불필요, 순서 문제도 없음 —
    `Destroy()` 전에 계산되며 `UnequipSkill()`을 거치지 않음)
- `MakeDefence/Assets/Scripts/UI/InvenDropHandler.cs`
  - `var wasDefault = tower.IsDefaultSkillEquipped;`를 `UnequipSkill()` 호출 **전**에 캡처,
    `ReturnSkill`은 `if (!wasDefault)`로 가드
- `MakeDefence/Assets/Scripts/UI/InvenSlotDragHandler.cs`
  - 동일 패턴 (캡처 후 `UnequipSkill()` → 가드된 `ReturnSkill`)
- `MakeDefence/Assets/Scripts/UI/InvenUI.cs`
  - 인벤 스킬 교체 시 기존 장착 스킬 `ReturnSkill` 호출 전 `!tower.IsDefaultSkillEquipped` 가드 추가
    (여기는 `UnequipSkill()`을 거치지 않으므로 순서 문제 없음)
- `MakeDefence/Assets/Scripts/UI/SkillSlotUI.cs`
  - 동일 가드 추가 (순서 문제 없음)
- `MakeDefence/Assets/Scripts/UI/SellConfirmPopup.cs`
  - `OnConfirm()`: 장착 스킬 판매 분기(`tower != null`)에서 `tower.UnequipSkill()` 호출 **전**에
    `wasDefault`를 캡처, 공용 큐브 지급(`CubeSystem.Instance?.Add(...)`, 126행)을 `!wasDefault`일 때만 실행
  - `Show(Tower tower, SkillData skill)`: `tower.IsDefaultSkillEquipped`인 경우 확인 문구를
    "하급 큐브 1개를 획득합니다." 대신 "보상 없이 해제됩니다." 등으로 분기 처리 (보상 없는데
    보상을 약속하는 문구를 보여주지 않도록)
- `MakeDefence/Assets/Scripts/UI/ShopDropHandler.cs`
  - `SellEquippedSkill()`의 `SellConfirmPopup.Instance == null` 폴백 분기: `wasDefault`를
    `InventorySystem.Instance.UnequipSkill()` 호출 **전**에 캡처, 큐브 지급을 `!wasDefault`로 가드

## 3. 신규 클래스 / 파일

없음. 기존 `Tower`/`SkillData` 구조 재사용 (마커 필드만 추가).

## 4. 테스트 계획

- Unity Editor에서 각 `Tower_Unit*` 프리팹에 `defaultSkill` 필드를 임의의 `SkillData` 에셋으로
  지정(플레이스홀더, 실제 매핑은 게임 디자인 확정 후 별도 데이터 작업으로 채움).
- Play 모드에서 유닛 설치 직후:
  - [ ] `EquippedSkill`이 즉시 `defaultSkill`로 채워지는지 확인 (인벤토리 UI에서 스킬 슬롯 표시 확인)
  - [ ] 설치 직후 몬스터가 사거리 내에 있으면 자동으로 공격이 시작되는지 확인
  - [ ] 타워를 재배치(`MoveTo` 경로, 드래그 이동 등)했을 때 기존 장착 스킬이 유지되고 재지급 로직이
        중복 실행되지 않는지 확인
  - [ ] 인벤토리 UI에서 기본 스킬을 해제(`UnequipSkill`) 후 다른 스킬로 교체가 정상 동작하는지 확인
  - [ ] `defaultSkill`이 비어있는(null) 프리팹은 기존과 동일하게 미장착 상태로 유지되는지 확인
  - [ ] **(신규)** 기본 스킬이 장착된 타워를 삭제해도 `ShopSystem` 보유 스킬 목록에 추가되지 않는지 확인
  - [ ] **(신규)** 기본 스킬을 드래그로 인벤에 반환/해제해도 보유 목록에 추가되지 않는지 확인
  - [ ] **(신규)** 인벤 스킬 클릭/드래그로 기본 스킬을 다른 스킬로 교체해도 기본 스킬이 보유 목록에
        추가되지 않는지 확인
  - [ ] **(신규)** 기본 스킬을 판매 시도 시 큐브 보상이 지급되지 않는지 확인 (판매 팝업 경로 + 폴백 경로 모두)
  - [ ] **(신규)** 기본 스킬 해제 후 플레이어가 직접 다른 스킬을 장착하면 `IsDefaultSkillEquipped`가
        `false`로 정상 리셋되어, 이후 그 스킬은 정상적으로 반환/판매되는지 확인
  - [ ] **(신규)** 기본 스킬만 장착된 타워를 삭제 확인 팝업에 띄웠을 때, 문구에 "스킬 1"이 표시되지
        않는지 확인 (실제로 반환되지 않는데 반환된다고 안내하면 안 됨)
  - [ ] **(신규)** 기본 스킬 판매 확인 팝업을 띄웠을 때 "하급 큐브 1개 획득" 문구 대신 보상 없음 문구가
        표시되는지 확인
  - [ ] **(신규)** 일반 구매 스킬(비-기본)을 삭제/판매/교체하는 기존 플로우는 회귀 없이 그대로
        인벤토리 반환/큐브 지급이 되는지 확인 (가드가 기존 정상 케이스를 막지 않는지)

## 5. 위험 요소

- **유닛 타입 ↔ 기본 스킬 매핑은 게임 디자인 결정 사항 — 코드만으로는 기능이 무효** (2차 Codex
  리뷰 P2) — `defaultSkill` 필드가 각 `Tower_Unit*` 프리팹에 실제로 할당되지 않으면
  `Place()`는 항상 `defaultSkill == null` 분기를 타서 기존과 동일하게 미장착 상태로 남는다. 즉
  이 이슈의 코드 변경만 머지해서는 **런타임 동작이 전혀 바뀌지 않는다**. 구체적인 유닛→스킬
  매핑(예: Tower_Unit1 → Fireball 등)이 확정되기 전까지는 이슈를 완료로 볼 수 없으며, 구현 PR에는
  코드 변경과 함께 실제 프리팹 데이터 할당까지 포함하거나(권장), 매핑이 아직 미정이라면 구현 PR
  단계에서 사용자에게 매핑을 확인받아야 한다.
- ~~`EquipSkill`은 `ShopSystem._ownedSkills` 보유 목록과 무관하게 동작하므로...~~ →
  **(Codex 리뷰로 확인/해결)** 이 점이 정확히 문제였다. `EquippedSkill != null`을 "보유 스킬 장착
  중"으로 간주하는 6개 지점(`InventorySystem.DeleteTower`, `InvenDropHandler`,
  `InvenSlotDragHandler`, `InvenUI`, `SkillSlotUI`, `SellConfirmPopup`/`ShopDropHandler`)에서
  기본 스킬을 삭제/해제/교체/판매하면 무료 스킬이 보유 목록에 편입되거나 큐브로 환전되어 반복
  악용이 가능했다. `Tower.IsDefaultSkillEquipped` 마커 + 6개 지점 가드로 해결 (위 1절 참고).
- 기존에 `EquippedSkill == null`을 "미장착" 상태로 가정하는 로직(`Tower.Update()`,
  `OwnedSkillsListUI` 등)이 있는지 재확인 — 기본 스킬 지급으로 인해 항상 non-null이 되는 유닛이
  생기면 해당 가정에 의존하는 UI/로직에 영향이 없는지 확인 필요.
- 마커 가드를 6개 지점에 개별적으로 추가하는 방식은 향후 "장착 스킬을 보유 목록으로 반환"하는
  새 코드 경로가 추가될 때 가드를 빠뜨릴 위험이 있다 (구현 PR 리뷰 시 유의). 장기적으로는
  `InventorySystem`에 `ReturnEquippedSkillIfOwned(Tower)` 같은 단일 헬퍼로 응집하는 리팩터링을
  고려할 수 있으나, 이번 이슈 범위에서는 기존 산재 패턴을 유지한 채 가드만 추가한다.
