# Issue #371 — 유닛 생성 시 UnitBtn에 해당 유닛 아이콘 스프라이트 표시

## 1. 시스템 구조

### 현재 흐름

```
Unitbtn{N} (UnitSpawnButton, unitPrefab 보유)
 └─ OnClick()
     ├─ _placedTower 없음 → JobSelectPopup.Show(OnJobSelected)
     │      └─ 직업 선택 → ResolvePrefab(job) → EnterPlacement(jobPrefab)
     │            └─ TowerPlacer.EnterPlacementMode(prefab, OnUnitPlaced)
     │                  └─ OnUnitPlaced(tower): _placedTower = tower, deleteButton.SetActive(true)
     └─ _placedTower 있음 → EnterMoveMode (이동)
```

- 버튼 GameObject 구조: `Button` + 배경 `Image`(TargetGraphic) + `Text` 자식 1개 + `UnitSpawnButton`.
- **배치한 유닛을 나타내는 아이콘 표시가 없다.** 배치 후에도 버튼 외형이 그대로라 어떤 버튼이 어떤 유닛을 소유 중인지 알 수 없다.
- 직업 전용 프리팹(`Tower_Warrior`/`Tower_Mage`/`Tower_Archor`)은 각각 `jobClass`가 고정 저장돼 있으나, 유닛을 대표하는 **아이콘 스프라이트 필드는 없다**. (궁수 프리팹 파일명은 오타 `Archor` — 실제 에셋 이름 그대로 사용)

### 변경 후 흐름

```
Unitbtn{N} (UnitSpawnButton + iconImage 참조 추가)
 └─ OnUnitPlaced(tower)
     ├─ _placedTower = tower
     ├─ deleteButton.SetActive(true)  (기존)
     └─ iconImage.sprite = tower.UnitIcon; iconImage.enabled = (UnitIcon != null)   ← 신규
 └─ HandleTowerRemoved()
     ├─ _placedTower = null  (기존)
     ├─ deleteButton.SetActive(false)  (기존)
     └─ iconImage.sprite = null; iconImage.enabled = false   ← 신규 (빈 버튼 복귀)
```

- 아이콘 소스는 **배치된 Tower 인스턴스**(`tower.UnitIcon`)에서 가져온다 → 직업 선택 결과(전사/법사/궁수)에 따라 실제 배치된 프리팹의 아이콘이 그대로 반영된다. 버튼에 직업별 아이콘을 각각 wiring 할 필요 없이 프리팹 한 곳에만 아이콘을 지정하면 된다.
- 아이콘은 버튼 배경 Image(TargetGraphic)를 덮어쓰지 않고, **버튼 하위에 전용 자식 `UnitIcon` Image**를 새로 만들어 그 위에 표시한다(비파괴적). 유닛이 없을 때는 `enabled = false`로 숨긴다.

## 2. 수정 파일

### `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
- `[SerializeField] private Sprite unitIcon;` 필드 추가 (유닛/직업 대표 아이콘).
- `public Sprite UnitIcon => unitIcon;` getter 추가.
- 그 외 로직 변경 없음(순수 데이터 필드 추가).

### `MakeDefence/Assets/Scripts/UI/UnitSpawnButton.cs`
- `[SerializeField] private Image iconImage;` 필드 추가 (버튼에 표시할 아이콘 Image, `using UnityEngine.UI;`는 이미 존재).
- `Awake()`: 시작 시 유닛이 없으므로 `iconImage`를 비우고 숨긴다(`sprite = null; enabled = false`). `deleteButton` 숨김과 동일한 위치.
- `OnUnitPlaced(Tower tower)`: 끝에 `ApplyIcon(tower.UnitIcon)` 호출 — 스프라이트 세팅 + `enabled = (sprite != null)`.
- `HandleTowerRemoved()`: 끝에 `ApplyIcon(null)` 호출 — 스프라이트 제거 + 숨김.
- `iconImage`가 null(미연결)이어도 NRE 없이 안전 동작(널 가드).

### `MakeDefence/Assets/Perfab/Tower_Warrior.prefab` / `Tower_Mage.prefab` / `Tower_Archor.prefab` (UnityMCP)
- 각 프리팹의 `Tower` 컴포넌트 `unitIcon` 필드에 직업별 아이콘 스프라이트를 연결.
  - 후보(`Assets/GUI_Parts/free_fantasy_rpg_icons/`): 전사 → `axe1`/`club` 등 근접 무기, 법사 → `fire`/`crystal` 등, 궁수 → `archery1`.
  - 최종 아이콘 선정은 인스펙터에서 조정 가능(placeholder 허용).

### `MakeDefence/Assets/Scenes/SampleScene.unity` (UnityMCP)
- 5개 버튼(`Unitbtn`, `Unitbtn1`~`4`) 각각에 자식 `UnitIcon` GameObject(`Image`) 추가.
  - RectTransform: 버튼 중앙 정렬, 적당한 크기(예: 48x48), 배경 위 레이어.
  - `Image.raycastTarget = false`(버튼 클릭을 가리지 않도록), 시작 시 `enabled = false`.
- 각 버튼 `UnitSpawnButton.iconImage` 필드에 해당 자식 `UnitIcon` Image 연결.

## 3. 신규 클래스 / 파일

- 신규 클래스 없음. `Tower`에 데이터 필드 1개, `UnitSpawnButton`에 참조 필드 1개 + 헬퍼(`ApplyIcon`) 추가.
- 신규 GameObject: 버튼별 자식 `UnitIcon` Image 5개(씬 내, UnityMCP로 생성).

## 4. 테스트 계획

### 수동 (Unity Editor)
- [ ] `Unitbtn` 클릭 → 직업 팝업에서 전사 선택 → 배치 완료 시 버튼에 전사 아이콘이 표시되는지.
- [ ] 법사/궁수 선택 시 각각 다른(해당 직업) 아이콘이 표시되는지.
- [ ] 유닛 삭제(전용 삭제 버튼) → 버튼 아이콘이 사라지고 빈 버튼으로 돌아가는지.
- [ ] 삭제 후 재배치 → 새로 고른 직업 아이콘으로 다시 표시되는지.
- [ ] 아이콘이 버튼 클릭을 가리지 않는지(raycastTarget=false 확인) — 배치 후 재클릭 시 이동 모드 정상 진입.
- [ ] 여러 버튼에 서로 다른 직업 배치 시 각 버튼이 독립적으로 자기 아이콘만 표시하는지.
- [ ] `iconImage` 미연결 버튼이 있어도 컴파일/런타임 에러 없이 동작하는지(널 가드 회귀).
- [ ] Unity 콘솔 컴파일 에러 0.

## 5. 위험 요소

- **아이콘 리소스 미확정**: 직업별 대표 아이콘은 우선 `free_fantasy_rpg_icons`의 placeholder로 지정하고, 최종 아트는 인스펙터에서 교체 가능. 아이콘이 스프라이트 시트/애니메이터 기반이라 프리팹 SpriteRenderer에서 런타임 추출이 어려워, 별도 `unitIcon` 스프라이트 필드를 명시적으로 둔다.
- **폴백(unitPrefab + SetJob) 경로**: 팝업 프리팹 미연결로 기본 `unitPrefab`이 배치되는 경우, 그 프리팹에도 `unitIcon`이 비어 있으면 아이콘이 안 뜬다 → 정상(빈 아이콘 숨김). 기본 프리팹에도 아이콘을 지정하면 폴백에서도 표시됨.
- **씬/프리팹 편집 경로**: `.prefab`/`.unity`/`.meta`는 UnityMCP 도구로만 편집(직접 YAML 편집 금지 — 프로젝트 가이드).
- **아이콘 레이어/클릭 간섭**: 아이콘 Image가 버튼 전체를 덮으면 클릭 raycast를 가로챌 수 있음 → `raycastTarget=false` 필수.
- **미저장 씬 변경 유실**: 스크립트 컴파일(도메인 리로드) 전에 씬을 저장해야 신규 GameObject가 유실되지 않음.
