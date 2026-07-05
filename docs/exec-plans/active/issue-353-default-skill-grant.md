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

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
  - `[SerializeField] private SkillData defaultSkill;` 필드 추가
  - `Place(Vector2Int coord)`에서 `EquippedSkill == null`이고 `defaultSkill != null`인 경우
    `EquipSkill(defaultSkill)` 호출 추가

## 3. 신규 클래스 / 파일

없음. 기존 `Tower`/`SkillData` 구조 재사용.

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

## 5. 위험 요소

- **유닛 타입 ↔ 기본 스킬 매핑은 게임 디자인 결정 사항** — 어떤 유닛에 어떤 스킬을 기본 지급할지는
  코드에서 유추 불가. 코드 구현 후 각 프리팹의 `defaultSkill` 필드를 Unity Editor에서 실제 값으로
  채우는 별도 데이터 작업이 필요하다 (플랜 PR 리뷰 시 확인 필요).
- `EquipSkill`은 `ShopSystem._ownedSkills` 보유 목록과 무관하게 동작하므로, 기본 스킬은 플레이어의
  "보유 스킬" 목록에는 잡히지 않는다 (상점에서 판매/교체 대상 아님). 이 스킬을 인벤토리 UI 상 보유
  스킬처럼 취급해야 하는지 여부는 별도 확인 필요.
- 기존에 `EquippedSkill == null`을 "미장착" 상태로 가정하는 로직(`Tower.Update()`,
  `OwnedSkillsListUI` 등)이 있는지 재확인 — 기본 스킬 지급으로 인해 항상 non-null이 되는 유닛이
  생기면 해당 가정에 의존하는 UI/로직에 영향이 없는지 확인 필요.
