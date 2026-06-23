# Issue #298 — 차원석 인벤토리 슬롯에 웨이브 생성 버튼 추가

## 1. 시스템 구조

웨이브 생성(균열 개방) 백엔드는 이미 완성되어 있다. 본 이슈는 **트리거 UI 위치 이동/추가**만 다룬다.

```
[차원석 인벤토리 패널]                       [균열 생성기 패널]
 ┌─────────────────────┐                    ┌─────────────────────┐
 │ DimensionStoneSlot…  │                    │  GenerateSlot       │
 │ DimensionStoneSlot…  │                    │  (LoadedStone 표시) │
 │ ...                  │                    │                     │
 │ [웨이브 생성] ◀── new │   click            │                     │
 └────────┬─────────────┘                    └─────────────────────┘
          │
          ▼
 InventorySystem.SelectedRift.OpenRift()
          │
          ▼
 RiftGenerator.OpenRift()                ─── 이미 존재
   ├─ guard: LoadedStone != null
   ├─ guard: !WaveSystem.IsWaveActive
   ├─ guard: GameState == Playing
   ├─ RiftWaveModifiers.FromOptions
   ├─ WaveSystem.StartRiftWave(mods)
   ├─ DimensionStoneInventory.Remove(LoadedStone)
   └─ ClearStone() + OnStoneChanged
```

관련 시스템:
- `RiftGenerator` — 차원석 장착/소모 + 균열 개방 진입점 (변경 없음)
- `WaveSystem` — 웨이브 라이프사이클 (변경 없음)
- `InventorySystem.SelectedRift` — 현재 선택된 균열 (변경 없음)
- `DimensionStoneInventoryView` — 차원석 인벤토리 그리드 UI **(버튼 추가 지점)**
- `RiftGeneratorPanel.openRiftButton` — 기존 트리거 위치 (제거 여부 결정 필요)

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/DimensionStoneInventoryView.cs`
  - "웨이브 생성" 버튼 참조 + 상태 갱신 책임 추가 (또는 신규 컴포넌트와 협업)
- `MakeDefence/Assets/Scripts/UI/RiftGeneratorPanel.cs`
  - `openRiftButton` 제거 결정 시 바인딩/Refresh 분기 삭제
- `MakeDefence/Assets/Scenes/*.unity` (UnityMCP 로 편집)
  - 차원석 인벤토리 패널 하위에 Button GameObject 추가 + 컴포넌트 연결
- `MakeDefence/Assets/Prefabs/UI/*` (해당하는 prefab 이 있을 경우 — 조사 후 결정)

## 3. 신규 클래스 / 파일

신규 클래스는 **선택지 2가지**. PR 단계에서 한 가지로 확정.

### 옵션 A — DimensionStoneInventoryView 에 인라인 추가 (최소 변경)
- 신규 클래스 없음
- `DimensionStoneInventoryView` 에 `[SerializeField] Button openRiftButton` 필드 + `OnEnable` 에서 이벤트 구독, `Rebuild` 에서 활성 상태 갱신

### 옵션 B — 신규 `OpenRiftButton.cs` 분리 (권장)
- 경로: `MakeDefence/Assets/Scripts/UI/OpenRiftButton.cs`
- 단일 책임: SelectedRift / LoadedStone / WaveSystem.IsWaveActive / GameState 변화 구독 → Button.interactable 갱신, 클릭 → `SelectedRift.OpenRift()`
- 인벤토리 패널과 결합하지 않으므로, 추후 다른 위치에서 동일 버튼을 재사용하기 쉬움

**기본 권장**: 옵션 B (단일 책임 + InventoryView 변경 최소화). 인스펙터 연결 부담이 부담스러우면 옵션 A 로 폴백.

## 4. 테스트 계획

### EditMode (가능하면)
- `RiftGenerator.OpenRift()` 의 기존 분기는 이미 충분한 가드를 갖고 있어 별도 추가 테스트는 불필요.
- 신규 `OpenRiftButton` 작성 시 — `Button.interactable` 상태가 다음 조합에 대해 기대대로 갱신되는지 (PlayMode/수동):
  | SelectedRift | LoadedStone | IsWaveActive | GameState | interactable |
  |--------------|-------------|--------------|-----------|--------------|
  | null         | -           | -            | -         | false        |
  | ok           | null        | false        | Playing   | false        |
  | ok           | ok          | true         | Playing   | false        |
  | ok           | ok          | false        | !Playing  | false        |
  | ok           | ok          | false        | Playing   | true ✅      |

### 수동 (Unity Editor / 빌드)
- [ ] 균열 생성기 배치 → 차원석 장착 → 차원석 인벤 패널의 "웨이브 생성" 버튼 활성화 확인
- [ ] 버튼 클릭 → 웨이브 시작 + GenerateSlot 비워짐 + 인벤 카운트 -1
- [ ] 웨이브 진행 중 버튼 비활성화 확인
- [ ] 웨이브 종료 후 다시 차원석 장착 → 버튼 재활성화 확인
- [ ] Rift 선택 해제 시 버튼 비활성화 확인
- [ ] (옵션 B 채택 시) 기존 `RiftGeneratorPanel.openRiftButton` 도 동시 동작/비활성 결정대로 동작하는지 확인

## 5. 위험 요소

- **트리거 중복**: `RiftGeneratorPanel.openRiftButton` 을 남겨두면 같은 액션의 진입점이 2개. PR 본문에서 한 가지로 정리.
  - 제거 권장. UX 단순화 + 상태 동기화 부담 감소.
- **이벤트 구독 누수**: 신규 컴포넌트에서 `OnRiftSelected` / `OnInventoryChanged` / `OnStoneChanged` / `WaveSystem` 관련 이벤트를 구독하면 `OnDisable` 에서 반드시 해제. 특히 `_current.OnStoneChanged` 는 rift 교체 시 이전 구독 해제 필요.
- **WaveSystem 상태 변경 이벤트 부재 가능성**: `WaveSystem.IsWaveActive` 변경 이벤트가 없을 경우 버튼이 즉시 비활성/재활성되지 않을 위험. 구현 단계에서 이벤트 유무 확인 → 없으면 Update 폴링 또는 새 이벤트 추가 (별도 이슈로 분리 가능).
- **씬/prefab 편집**: `.unity` / `.prefab` 직접 YAML 편집 비권장 — UnityMCP `manage_ui` / `manage_gameobject` / `manage_prefabs` 사용 (memory: feedback_unity_asset_edits).
- **UI 레이아웃**: 인벤토리 패널 내부에 버튼을 추가하면 슬롯 자식 enumeration 로직(`DimensionStoneInventoryView.Awake` 의 `foreach (Transform child in slotContainer)`) 에 영향. 버튼은 `slotContainer` 가 아닌 별도 부모 또는 InventoryView 외부에 배치 권장.
