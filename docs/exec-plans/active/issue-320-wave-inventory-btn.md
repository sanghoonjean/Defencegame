# Issue #320 — 웨이브 생성 버튼에서 차원석 인벤토리 직접 호출, RiftGenerator 시스템 전체 제거

## 1. 시스템 구조

### 현재 구조
```
InputManager.HandleClick()
  ├─ 빈 타일 클릭 + BuildMode.Rift → RiftGeneratorPlacer.TryPlace(coord)
  │     └─ RiftGenerator 월드 오브젝트 스폰 → MapTileSystem._placedRifts 등록
  ├─ RiftGenerator 오브젝트 클릭 → InventorySystem.SelectRift(rift)
  │     └─ OnRiftSelected 이벤트
  │           ├─ RiftPanelToggle: DimesionStoneInventoryUI CanvasGroup.alpha 토글 + rift 옆으로 위치 추종
  │           ├─ OpenRiftButton: interactable 갱신, 클릭 시 rift.OpenRift()
  │           ├─ RepeatGenerateToggleButton: 연속 생성 루프의 대상 rift 캐시
  │           └─ GenerateSlotDropTarget: 장착 슬롯 아이콘 갱신 + 드롭 시 rift 에 장착
  └─ RiftGenerator (MonoBehaviour, 월드 오브젝트)
        └─ LoadedStone 보유, ApplyCube(), OpenRift() → WaveSystem.StartRiftWave()
```
"웨이브를 생성하려면 먼저 Rift 를 월드에 배치하고 클릭해서 선택해야" 패널이 뜨는 다단계 구조.

### 변경 후 구조
```
WaveGeneraterbtn (UIToggleButton, targetPanel 재배선)
  └─ 클릭 → DimesionStoneInventoryUI GameObject.SetActive 토글 (선택/배치 불필요, 즉시 열림)

WaveGeneratorSystem (신규 싱글톤, RiftGenerator 의 상태/로직 이식)
  ├─ LoadedStone, OnStoneChanged
  ├─ SetStone / ClearStone
  ├─ ApplyCube(CubeType)
  └─ OpenRift() → WaveSystem.StartRiftWave()

DimesionStoneInventoryUI 내부 버튼들(GenerateSlotDropTarget, OpenRiftButton, RepeatGenerateToggleButton)
  └─ 이제 "선택된 rift" 없이 WaveGeneratorSystem.Instance 를 직접 참조
```
Rift 오브젝트를 월드에 배치/선택하는 개념 자체가 사라지고, 차원석 장착·큐브 적용·웨이브 오픈 상태는 `WaveGeneratorSystem` 싱글톤 하나로 이전한다. `DimesionStoneInventoryUI` 는 `WaveGeneraterbtn` 클릭으로 직접 여닫는 일반 패널이 된다 (SHOP_UI 와 동일한 `UIToggleButton` 패턴).

## 2. 수정 파일

| 파일 | 변경 내용 |
|---|---|
| `MakeDefence/Assets/Scripts/Systems/InventorySystem.cs` | `SelectedRift`/`OnRiftSelected`/`SelectRift()` 및 `SelectTower`/`Deselect` 의 rift 분기 제거. `EquipStoneToRift(rift, stone)` → `EquipStone(stone)` (`WaveGeneratorSystem.Instance` 대상), `TryUnloadStoneFromRift(source)` → `TryUnloadStone(source)` 로 시그니처 단순화 |
| `MakeDefence/Assets/Scripts/Systems/MapTileSystem.cs` | `_placedRifts` 딕셔너리, `CanPlaceRift`/`PlaceRift`/`RemoveRift`/`GetRiftAt` 제거. `CanPlaceTower`/`HasVacantBuildableTile` 에서 `_placedRifts` 참조 제거 |
| `MakeDefence/Assets/Scripts/Systems/InputManager.cs` | 클릭 시 `RiftGenerator` 컴포넌트 검사 분기 제거, 빈 타일 클릭 시 `RiftGeneratorPlacer.TryPlace` 호출 제거(Tower 배치만 남김). `BuildMode.Rift` → `BuildMode.None` 으로 rename (아래 "위험 요소" 참고) |
| `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs` | `InputManager.Instance?.SetBuildMode(BuildMode.Rift)` 2곳 → `BuildMode.None` |
| `MakeDefence/Assets/Scripts/UI/BuildModeToggleButton.cs` | `BuildMode.Rift` → `BuildMode.None` |
| `MakeDefence/Assets/Scripts/TestRunner.cs` | 디버그 단축키(B/O/1~5)가 참조하던 `SelectedRift`/`BuildMode.Rift` → `WaveGeneratorSystem.Instance`/`BuildMode.None` |
| `MakeDefence/Assets/Scripts/UI/OpenRiftButton.cs` | `InventorySystem.OnRiftSelected`/`SelectedRift` 구독 제거, `WaveGeneratorSystem.Instance.OnStoneChanged` 직접 구독, 클릭 시 `WaveGeneratorSystem.Instance.OpenRift()` 호출 |
| `MakeDefence/Assets/Scripts/UI/RepeatGenerateToggleButton.cs` | `_cachedRift`/`HandleRiftSelected`/`InventorySystem.OnRiftSelected` 제거, `WaveGeneratorSystem.Instance` 직접 참조로 교체 |
| `MakeDefence/Assets/Scripts/UI/GenerateSlotDropTarget.cs` | `_current`(RiftGenerator)/`HandleRiftSelected` 제거, `WaveGeneratorSystem.Instance.OnStoneChanged` 직접 구독, 드롭 시 `InventorySystem.EquipStone(stone)` 호출 |
| `MakeDefence/Assets/Scripts/UI/InvenUI.cs` | 차원석 클릭 장착 로직에서 `SelectedRift` 참조 제거, `InventorySystem.EquipStone(stone)` 호출로 교체 |
| `MakeDefence/Assets/Scripts/UI/InvenDropHandler.cs` | `TryUnloadStoneFromRift` → `TryUnloadStone` 호출명 교체 |
| `MakeDefence/Assets/Scripts/UI/InvenSlotDragHandler.cs` | 위와 동일 |
| `MakeDefence/Assets/Scenes/SampleScene.unity` (UnityMCP 로만 편집) | 1) `WaveGeneraterbtn` 의 `UIToggleButton.targetPanel` 을 `SHOP_UI` → `DimesionStoneInventoryUI` 로 재배선. 2) `DimesionStoneInventoryUI` GameObject 초기 `m_IsActive` 를 `1` → `0` (SHOP_UI 와 동일하게 기본 숨김). 3) `DimesionStoneInventoryUI` 에서 `RiftPanelToggle` 컴포넌트 제거. 4) `RiftGeneratorPlacer` GameObject 및 컴포넌트 제거. 5) `Generatebtn`/`RepeateGeneratebtn` 에 남아있는 스크립트 참조는 클래스가 유지되므로(내용만 변경) 그대로 둠 |

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Systems/WaveGeneratorSystem.cs`
  - 싱글톤 `MonoBehaviour` (`Instance`), 다른 Systems(`ShopSystem`, `CubeSystem`, `WaveSystem`)와 동일한 패턴으로 씬 상의 Systems 오브젝트에 부착
  - `DimensionStone LoadedStone { get; }`, `event Action OnStoneChanged`
  - `SetStone(DimensionStone)`, `ClearStone()` — `RiftGenerator` 에서 그대로 이식
  - `ApplyCube(CubeType)`/`CanApply(CubeType)` — `RiftGenerator.ApplyCube`/`CanApply` 로직 그대로 이식 (큐브 사전 검증 포함)
  - `OpenRift()` — `RiftGenerator.OpenRift` 로직 이식 (`RiftWaveModifiers.FromOptions` + `WaveSystem.StartRiftWave` 호출). 로그 prefix 만 `[WaveGeneratorSystem]` 으로 교체
  - `TileCoord`, `Place()`, `OnDestroy() → MapTileSystem.RemoveRift`, 정적 이벤트 `OnRiftPlaced`/`OnRiftOpened` 는 이식하지 않음 (외부 구독자 없음, grep 으로 확인됨)

## 4. 제거 대상

- `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftGenerator.cs` (+ `.meta`)
- `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftGeneratorPlacer.cs` (+ `.meta`)
- `MakeDefence/Assets/Scripts/UI/RiftPanelToggle.cs` (+ `.meta`)
- `MakeDefence/Assets/Scripts/UI/RiftGeneratorPanel.cs` (+ `.meta`) — 이미 씬/프리팹 어디에도 부착되지 않은 dead code
- `MakeDefence/Assets/Prefabs/RiftGenerator.prefab` (+ `.meta`)
- 씬 내 `RiftGeneratorPlacer` GameObject (UnityMCP)

**유지**: `MakeDefence/Assets/Scripts/Gameplay/Rift/Core/*` (`DimensionStone`, `DimensionStoneOptionType`, `RiftWaveModifiers`, `RiftRewardCalculator`, `StoneDropChanceTable`) — RiftGenerator 오브젝트와 무관한 순수 데이터/보상 로직이며 `WaveGeneratorSystem`/`ShopSystem`/`WaveSystem`이 계속 사용. `OpenRiftButton.cs`/`RepeatGenerateToggleButton.cs`/`GenerateSlotDropTarget.cs` 는 삭제하지 않고 내부 구현만 교체(파일명이 "Rift" 를 포함하지만 UI 버튼 자체의 역할(웨이브 생성 버튼)은 유지되는 기능이므로).

## 5. 테스트 계획

- [ ] Unity 컴파일 에러 없음 (`read_console`)
- [ ] EditMode 테스트 전체 통과 — 특히 `DimensionStoneTests`, `RiftWaveModifiersTests`, `RiftRewardCalculatorTests` (Rift Core 로직 무변경 확인)
- [ ] Play 모드 수동 확인
  - [ ] 씬 시작 시 `DimesionStoneInventoryUI` 패널이 보이지 않음
  - [ ] `WaveGeneraterbtn` 클릭 → 패널 열림 / 다시 클릭 → 닫힘
  - [ ] 패널 내에서 인벤토리 차원석을 GenerateSlot 에 드래그/클릭 장착 → 아이콘 갱신
  - [ ] `Generatebtn`(OpenRiftButton) 클릭 → 웨이브 시작, 장착된 차원석 소모
  - [ ] `RepeateGeneratebtn`(연속 생성) 토글 → 클리어마다 자동 재장착 + 웨이브 시작, 인벤 소진 시 자동 정지
  - [ ] 큐브(1~5 또는 UI 버튼) 적용이 장착된 차원석에 정상 반영
  - [ ] 인벤 패널 배경/슬롯으로 드래그하여 장착된 차원석 회수
  - [ ] 씬에 더 이상 `RiftGenerator` 오브젝트가 스폰/배치되지 않음, 클릭으로 rift 선택 UI가 나타나지 않음
  - [ ] 타워 배치 모드 토글(`BuildModeToggleButton`)이 기존과 동일하게 동작 (rename 만 있었고 동작 변화 없음 확인)

## 6. 위험 요소

- **`BuildMode.Rift` → `BuildMode.None` rename**: 이 enum 값은 실제로 "Rift 배치 모드"가 아니라 `TowerPlacer`/`InputManager` 에서 "Tower 배치 중이 아닌 기본 상태"를 가리키는 용도로도 쓰이고 있었음(#316 에서 라벨 텍스트는 이미 제거됨). 이번 이슈 범위에서 이름만 정리하며 동작은 바꾸지 않음 — 리뷰 시 이름을 유지할지(`Rift` 그대로) vs `None`/`Idle` 로 바꿀지 확인 필요.
- **`WaveGeneratorSystem` 은 싱글톤이라 "장착 슬롯이 1개"라는 기존 게임 디자인(Rift 도 원래 1개만 자동 배치되던 구조)과 동일함** — 다만 향후 "여러 개의 웨이브 생성기"로 확장할 계획이 있다면 이번 설계로는 지원 불가. 현재 `autoPlaceOnStart` 로 사실상 1개만 쓰였으므로 문제 없다고 판단.
- **씬 파일은 CLAUDE.md 지침에 따라 UnityMCP 로만 편집** — YAML 직접 수정 금지. `RiftGeneratorPlacer` GameObject 삭제, `WaveGeneraterbtn.targetPanel` 재배선, `DimesionStoneInventoryUI` 초기 비활성화, `RiftPanelToggle` 컴포넌트 제거를 Unity 에디터가 열려있는 상태에서 MCP 도구로 수행.
- **작업 브랜치에 커밋되지 않은 기존 변경사항 존재**: `WaveGeneraterbtn` GameObject 자체가 이미 에디터에서 생성되어 있었고(uncommitted), `UIToggleButton.targetPanel` 이 임시로 `SHOP_UI` 를 가리키고 있었음 — 이번 작업으로 `DimesionStoneInventoryUI` 로 재배선하면 됨.
- **`RiftGeneratorPanel.cs`** 는 현재 씬/프리팹 어디에도 붙어있지 않은 완전 dead code 로 확인됨 — 삭제해도 씬에 영향 없음.
- `RepeatGenerateToggleButton`/`GenerateSlotDropTarget`/`OpenRiftButton` 파일명에는 여전히 "Rift" 가 남지만, 클릭 동작(웨이브 생성 버튼) 자체는 유지 대상이라 삭제하지 않고 내부만 교체함 — 파일명까지 리네임할지는 이번 범위에서 제외(별도 이슈로 분리 가능).
