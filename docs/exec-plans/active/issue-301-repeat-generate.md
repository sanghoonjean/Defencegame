# Issue #301 — 웨이브 연속 생성 모드 (RepeateGeneratebtn 토글)

## 1. 시스템 구조

기존 1회성 진입점 `Generatebtn` + `OpenRiftButton` 옆에 **토글형 "연속 생성" 진입점**을 추가한다. 토글 ON 동안 인벤토리의 차원석을 자동 소모하며 웨이브를 연속으로 개방하고, 소모 실패(잔량 0 / 게임 상태 변화 등) 시 자동 OFF 시각 복귀.

### 현재 씬 상태 (조사 결과 — UnityMCP `find_gameobjects`)

```
Canvas/DimesionStoneInventoryUI
├─ Scroll View                          ← 차원석 인벤 그리드
├─ Generatebtn                          ← Button + OpenRiftButton (1회성, 그대로 유지)
├─ GenerateSlot                         ← GenerateSlotDropTarget (장착 슬롯)
└─ RepeateGeneratebtn  ★ 신규           ← Button + OpenRiftButton (잘못된 컴포넌트 — 재배선 필요)
```

- `RepeateGeneratebtn` 은 현재 `OpenRiftButton` 이 부착돼 있음 (복사 흔적). 이 컴포넌트를 제거하고 신규 `RepeatGenerateToggleButton` 으로 교체.

> ⚠️ **씬 커밋 상태 주의** (Codex P2 회신): 본 플랜 작성 시점에 `RepeateGeneratebtn` GameObject 는 사용자가 Unity Editor 에서 추가한 **로컬 미커밋 변경**(working tree `M MakeDefence/Assets/Scenes/SampleScene.unity`) 상태로 존재한다. UnityMCP `find_gameobjects` 로 `instanceID=-35718`, path `Canvas/DimesionStoneInventoryUI/RepeateGeneratebtn` 직접 확인 완료. `git grep` 으로 origin/main 의 씬을 검색하면 발견되지 않는데, 이는 커밋 누락 때문이며 누락된 객체가 아니다. **구현 PR 단계에서 씬을 함께 커밋**해 origin 에 반영한다.

### 핵심 흐름

```
[토글 ON 클릭]
 ↓
RepeatGenerateToggleButton.OnToggleOn()
 ├─ IsActive = true
 ├─ _remaining = DimensionStoneInventory.Instance.Count   ← ON 시점 스냅샷 (Codex P1 반영)
 ├─ Image/Color 토글 ON 시각
 └─ TryConsumeNext()
       ↓
   _remaining <= 0 || inv.Count == 0 ?
       ├─ YES → Stop()  (즉시 OFF 복귀)
       └─ NO  → _remaining-- → 첫 stone 을 SelectedRift 에 SetStone → rift.OpenRift()
                  OpenRift() == false ? stone 회수 + Stop()
                  OpenRift() == true  ? 대기 (다음 WaveEnded 이벤트까지)

[WaveSystem.OnWaveEnded(true)]  — 클리어
 ↓
IsActive == true && IsRiftWaveActive 였음?
 └─ TryConsumeNext() 반복  (DroppedStoneSystem 의 클리어 보너스는 _remaining 카운터로 무시됨)

[WaveSystem.OnWaveEnded(false)]  — 패배
 ↓
Stop()

[GameStateSystem.OnStateChanged → Playing 이외]
 ↓
Stop()

[InventorySystem.OnRiftSelected(null)]
 ↓
Stop()

[토글 OFF 클릭]
 ↓
Stop()  — 진행 중인 웨이브는 그대로 두고 다음 사이클 진입만 차단
```

### 데이터 흐름

```text
사용자 → RepeateGeneratebtn (Toggle)
            ↓
RepeatGenerateToggleButton (신규)
            ↓ (Active 동안 반복)
DimensionStoneInventory.Stones[0] → SelectedRift.SetStone
            ↓
RiftGenerator.OpenRift() → WaveSystem.StartRiftWave
            ↓
WaveSystem.OnWaveEnded(cleared)
            ↓
 cleared && IsActive ? 다음 stone 소모 : Stop()
```

### 1회성 vs 연속 모드 동시 운영

- 기존 `Generatebtn`(OpenRiftButton) 는 그대로. 연속 모드와 별개로 작동.
- 연속 모드 ON 중 사용자가 `Generatebtn` 을 누르면 → `WaveSystem.IsWaveActive` 가드로 인해 중복 호출 차단됨 (이미 구현됨).

## 2. 수정 파일

- `MakeDefence/Assets/Scenes/SampleScene.unity` — UnityMCP `manage_components` 로 편집
  - `Canvas/DimesionStoneInventoryUI/RepeateGeneratebtn`
    - `OpenRiftButton` 컴포넌트 **제거** (복사로 부착된 것)
    - `RepeatGenerateToggleButton` 컴포넌트 **추가**
- (선택) `MakeDefence/Assets/Scripts/UI/OpenRiftButton.cs` — 변경 없음. 단 연속 모드와 동시 사용 시의 UX 검증 필요.

> ⚠️ `.unity` / `.prefab` 직접 YAML 편집 비권장 — UnityMCP `manage_components` 만 사용 ([feedback_unity_asset_edits](../../../../../.claude/projects/C--Users-kalon-Documents-GitHub-Defencegame/memory/feedback_unity_asset_edits.md)).

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/UI/RepeatGenerateToggleButton.cs` (신규)

- `[RequireComponent(typeof(Button))]`
- 책임:
  - 클릭 → `IsActive` 토글 (true ↔ false)
  - **ON 진입 시 인벤 카운트 스냅샷** `_remaining = inv.Count` (아래 P1 회신 참조) → `TryConsumeNext()` 즉시 실행
  - `WaveSystem.OnWaveEnded(true)` 구독 → IsActive 이면 `_remaining > 0 && inv.Count > 0` 일 때만 `TryConsumeNext()` 반복
  - `WaveSystem.OnWaveEnded(false)` / `GameStateSystem.OnStateChanged(non-Playing)` / `InventorySystem.OnRiftSelected(null)` 구독 → 자동 `Stop()`
  - `Button.interactable` 자동 갱신 — 아래 조건표 참조
- 내부 상태:
  - `bool IsActive` — 토글 상태 (외부 노출 X)
  - `int _remaining` — ON 시 인벤 카운트 스냅샷. 매 사이클 `--`. 0 도달 시 Stop.
  - `ColorBlock` 백업 — ON 시각 (예: pressedColor / selectedColor 사용) 적용 후 OFF 복귀
- `Button.interactable` 조건표 (Codex P2 반영):

  | 상태 | interactable |
  |---|---|
  | `IsActive == true` | **항상 true** (사용자가 OFF 누를 수 있어야 함) |
  | `IsActive == false` + 아래 모두 만족 | true |
  | 그 외 | false |

  비활성 모드 활성 조건 (전부 만족):
  - `SelectedRift != null`
  - `DimensionStoneInventory.Instance.Count > 0`
  - `WaveSystem.Instance != null && !WaveSystem.Instance.IsWaveActive` ← **신규 (Codex P2)**
  - `GameStateSystem.Current == GameState.Playing`

- 차원석 장착 로직:
  - `DimensionStoneSlot.EquipToRift(rift, stone)` 정적 메서드 **재사용** (swap 패턴 포함). 이미 검증된 경로라 신규 로직 추가 없음.
- 주의:
  - `OnWaveEnded(true)` 후 `EndWave` 가 `Playing` 상태를 유지하므로 `OpenRift` 가드 통과 OK.
  - `OpenRift()` 실패 시 (`StartRiftWave` 거부 등) **`SetStone` 한 stone 을 인벤토리로 자동 회수**한 뒤 `Stop()` 호출. 회수 코드 예: `if (rift.LoadedStone == lastEquipped) { DimensionStoneInventory.Instance.Add(rift.LoadedStone); rift.ClearStone(); }`.
  - 단 `IsWaveActive` 가드를 interactable 에 추가했으므로 토글 ON 시 진입 자체가 차단됨 — 회수 로직은 race 안전망일 뿐 정상 경로에서는 호출되지 않음.

### `MakeDefence/Assets/Scripts/UI/RepeatGenerateToggleButton.cs` — 시각 처리 메모

- 토글 ON 시 `Button.colors.normalColor = pressedColor`(또는 selectedColor) 로 강조.
- `Stop()` 에서 원본 `ColorBlock` 복원 (시각만 — 구독은 그대로 유지).
- `OnDisable` 에서도 원본 `ColorBlock` 복원 (안전망).
- 별도 Sprite Swap 은 사용하지 않음 (씬에 ON/OFF 별도 sprite 없음 — 색만 변경).

## 4. 테스트 계획

### 수동 (Unity Editor)

전제: SampleScene 실행, 균열 생성기 1기 배치, `RiftGeneratorPlacer.autoPlaceCoord` 활성, 인벤토리에 차원석 ≥ 3.

- [ ] 균열 미선택 상태 → `RepeateGeneratebtn` 비활성
- [ ] 균열 선택 + 인벤토리 empty → 비활성
- [ ] 균열 선택 + 인벤토리 stone ≥ 1 → 활성
- [ ] 토글 ON 클릭 → 시각 변화(색) + GenerateSlot 에 첫 stone 장착 + 웨이브 시작
- [ ] 웨이브 클리어 → 다음 stone 자동 장착 + 웨이브 자동 시작 (인벤 카운트 -1, 반복)
- [ ] **ON 시점 인벤 N=3 → 정확히 3회 사이클 후 자동 Stop** (Codex P1 — 클리어 보너스 stone 누적돼도 카운터로 차단)
- [ ] **ON 시점 인벤 N=3 → 3회 사이클 종료 후 인벤에 클리어 보너스 stone 만 잔존**
- [ ] 인벤토리가 비면 자동 Stop → 토글 OFF 시각 복귀, 버튼 비활성
- [ ] 토글 ON 중 OFF 클릭 → 진행 중 웨이브는 끝까지 진행, 다음 웨이브 자동 시작 안 함
- [ ] **일반 웨이브 진행 중에는 토글 자체가 비활성** (Codex P2 — `!IsWaveActive` 가드)
- [ ] 토글 ON 중 사용자가 `Generatebtn`(1회성) 클릭 → `IsWaveActive` 가드로 중복 실행 안 됨 (정상)
- [ ] Defeat 발생 → 자동 Stop
- [ ] 게임 일시정지 / WaveResult 진입 시 토글 OFF 복귀
- [ ] 동일 사이클을 3회 반복해도 stone 잔량/장착 상태 일관 (race 없음)

### EditMode

신규 컴포넌트는 Unity 이벤트 + 싱글톤 의존이라 단위 테스트 부담이 크다. `OpenRiftButton` 과 동일하게 EditMode 테스트는 추가하지 않고 수동 검증으로 커버.

## 5. 위험 요소

- **DroppedStoneSystem 클리어 보너스로 인한 무한 farming** 🔴 (Codex P1 회신)
  - `WaveSystem.EndWave` 가 `OnWaveEnded(true)` 를 발화. `DroppedStoneSystem` 은 `[DefaultExecutionOrder(-100)]` 으로 본 토글(기본 0)보다 **먼저** 핸들러가 호출 → `CollectAll()` + `GrantClearBonus()` (인벤 +1 보장, `DroppedStoneSystem.cs:149-154`).
  - 본 토글이 그 다음에 `OnWaveEnded(true)` 콜백을 받았을 때 단순히 `inv.Count > 0` 만 체크하면 보상 stone 이 항상 채워져 있어 **무한 반복 farming** 가능.
  - **해결 (채택)**: ON 진입 시 `_remaining = inv.Count` 스냅샷 → 매 사이클 `--`. `_remaining <= 0` 이면 Stop. 클리어 중 추가된 보너스 stone 은 큐 외 보유분으로 남아 다음 토글 ON 시 다시 카운트됨.
  - 검증 체크리스트: ON 시점 인벤 N개 → 정확히 N회 OpenRift 후 자동 Stop (보너스 stone 은 인벤에 누적된 채 유지).

- **`SetStone` 후 `OpenRift` 실패 시 stone 잔존**
  - `OpenRift()` 가 `IsWaveActive` / `GameState` 가드로 실패하면 이미 `SetStone` 한 stone 이 `LoadedStone` 에 남는다.
  - 완화: `Stop()` 호출 전에 `if (rift.LoadedStone == lastEquippedStone) DimensionStoneInventory.Add(LoadedStone); rift.ClearStone();` 으로 회수.
  - 단순화 트레이드오프: 회수 로직 추가 vs 사용자 수동 회수. 우선 **회수 자동화** 채택 — race 위험은 동일 frame 내 수행이라 낮음.

- **`OnWaveEnded(true)` 직후 즉시 `OpenRift` 호출의 안전성**
  - `WaveSystem.EndWave` 가 `IsWaveActive = false; OnWaveEnded?.Invoke(true);` 순서로 실행 → 콜백 시점에 `IsWaveActive == false` 보장. 안전.
  - 단 `_currentRiftMods` 도 동일 시점에 default 로 reset 된 후 콜백 발화 → race 없음.

- **`OnWaveEnded(true)` 콜백 시점에 `GameState` 가 아직 `Playing`?**
  - 균열 웨이브의 경우 `EndWave` 의 분기에서 Playing 유지 (`wasRift → no-op`). 일반 웨이브 종료 시에는 `WaveResult` 진입 → 본 토글은 균열 웨이브 전제이므로 안전.
  - 단 첫 `OpenRift` 가 균열 웨이브를 시작하지 못하고 일반 웨이브가 동시에 진행 중인 가능성? — `IsWaveActive` 가드로 사전 차단. 균열 웨이브만 본 토글 trigger.

- **`Button.interactable` 갱신 트리거 누락**
  - 인벤토리 변동(`OnInventoryChanged`) 구독 필수 — 다른 경로(드래그, 1회 Generatebtn 사용)로 인벤이 변동돼도 토글 활성 조건 재계산.
  - Rift 교체 / GameState 변화도 구독 (`OpenRiftButton` 과 동일 패턴 답습).

- **연속 모드 ON 중 사용자가 GenerateSlot 에 수동 드래그**
  - 사용자가 다른 stone 을 GenerateSlot 에 드롭하면 `EquipToRift` 의 swap 로직으로 기존 stone 이 인벤 복귀. 이후 자동 사이클이 다음 cycle 에 그 stone 을 다시 집을 수 있음. **의도된 동작** — 사용자 우선.

- **이벤트 구독 수명** (Codex P2 회신)
  - 모든 구독은 `OnEnable` 에서 등록 / `OnDisable` 에서 해제. **`Stop()` 은 런타임 전환**(인벤 empty / Defeat / Rift 해제 / 사용자 OFF)이라 구독 해제 금지 — 해제하면 인벤이 다시 채워지거나 Rift 가 재선택돼도 `Button.interactable` 이 갱신되지 않아 토글이 영구 비활성화된다.
  - `Stop()` 책임 = `IsActive=false` + 시각(`ColorBlock`) 복원 + `LoadedStone` 회수(필요 시) **그게 전부**.
  - rift 교체 시 `OnStoneChanged` 재구독 패턴은 본 토글에서는 불필요 (LoadedStone 직접 추적 안 함, `IsActive` 만 관리).
  - 참고: `OpenRiftButton.cs` 21-35 행도 동일 패턴(OnEnable 구독 / OnDisable 해제)으로 검증됨.

- **씬 dirty 상태**
  - `RepeateGeneratebtn` 의 `OpenRiftButton` 제거 + `RepeatGenerateToggleButton` 추가는 UnityMCP `manage_components` 로 수행. 구현 단계에서 `manage_scene save` 로 baseline 확정.
