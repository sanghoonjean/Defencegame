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

### 핵심 흐름

```
[토글 ON 클릭]
 ↓
RepeatGenerateToggleButton.OnToggleOn()
 ├─ IsActive = true
 ├─ Image/Color 토글 ON 시각
 └─ TryConsumeNext()
       ↓
   인벤 stones.Count == 0 ?
       ├─ YES → Stop()  (즉시 OFF 복귀)
       └─ NO  → 첫 stone 을 SelectedRift 에 SetStone → rift.OpenRift()
                  OpenRift() == false ? Stop()
                  OpenRift() == true  ? 대기 (다음 WaveEnded 이벤트까지)

[WaveSystem.OnWaveEnded(true)]  — 클리어
 ↓
IsActive == true && IsRiftWaveActive 였음?
 └─ TryConsumeNext() 반복

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
  - `IsActive` 진입 시 `TryConsumeNext()` 즉시 실행 → 첫 차원석 장착 + OpenRift
  - `WaveSystem.OnWaveEnded(true)` 구독 → IsActive 이면 `TryConsumeNext()` 반복
  - `WaveSystem.OnWaveEnded(false)` / `GameStateSystem.OnStateChanged(non-Playing)` / `InventorySystem.OnRiftSelected(null)` 구독 → 자동 `Stop()`
  - `Button.interactable` 자동 갱신 — Rift 미선택 / non-Playing / 인벤토리 empty 면 비활성 (단 IsActive 중에는 OFF 누름 가능하도록 유지)
- 내부 상태:
  - `bool IsActive` — 토글 상태 (외부 노출 X)
  - `ColorBlock` 백업 — ON 시각 (예: pressedColor / selectedColor 사용) 적용 후 OFF 복귀
- 차원석 장착 로직:
  - `DimensionStoneSlot.EquipToRift(rift, stone)` 정적 메서드 **재사용** (swap 패턴 포함). 이미 검증된 경로라 신규 로직 추가 없음.
- 주의:
  - `OnWaveEnded(true)` 후 `EndWave` 가 `Playing` 상태를 유지하므로 `OpenRift` 가드 통과 OK.
  - `OpenRift()` 실패 시 (`StartRiftWave` 거부 등) `Stop()` 호출. **`SetStone` 후 OpenRift 실패하면 장착된 stone 이 rift 에 남게 됨** → 그 stone 은 다음 사용자 행동(인벤 회수 드래그 / 1회 Generatebtn)으로 처리. 자동 회수는 하지 않음 (UX 결정 — 단순성 우선).

### `MakeDefence/Assets/Scripts/UI/RepeatGenerateToggleButton.cs` — 시각 처리 메모

- 토글 ON 시 `Button.colors.normalColor = pressedColor`(또는 selectedColor) 로 강조.
- `OnDisable` 또는 `Stop()` 에서 원본 `ColorBlock` 복원.
- 별도 Sprite Swap 은 사용하지 않음 (씬에 ON/OFF 별도 sprite 없음 — 색만 변경).

## 4. 테스트 계획

### 수동 (Unity Editor)

전제: SampleScene 실행, 균열 생성기 1기 배치, `RiftGeneratorPlacer.autoPlaceCoord` 활성, 인벤토리에 차원석 ≥ 3.

- [ ] 균열 미선택 상태 → `RepeateGeneratebtn` 비활성
- [ ] 균열 선택 + 인벤토리 empty → 비활성
- [ ] 균열 선택 + 인벤토리 stone ≥ 1 → 활성
- [ ] 토글 ON 클릭 → 시각 변화(색) + GenerateSlot 에 첫 stone 장착 + 웨이브 시작
- [ ] 웨이브 클리어 → 다음 stone 자동 장착 + 웨이브 자동 시작 (인벤 카운트 -1, 반복)
- [ ] 인벤토리가 비면 자동 Stop → 토글 OFF 시각 복귀, 버튼 비활성
- [ ] 토글 ON 중 OFF 클릭 → 진행 중 웨이브는 끝까지 진행, 다음 웨이브 자동 시작 안 함
- [ ] 토글 ON 중 사용자가 `Generatebtn`(1회성) 클릭 → `IsWaveActive` 가드로 중복 실행 안 됨 (정상)
- [ ] Defeat 발생 → 자동 Stop
- [ ] 게임 일시정지 / WaveResult 진입 시 토글 OFF 복귀
- [ ] 동일 사이클을 3회 반복해도 stone 잔량/장착 상태 일관 (race 없음)

### EditMode

신규 컴포넌트는 Unity 이벤트 + 싱글톤 의존이라 단위 테스트 부담이 크다. `OpenRiftButton` 과 동일하게 EditMode 테스트는 추가하지 않고 수동 검증으로 커버.

## 5. 위험 요소

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

- **이벤트 누수**
  - `OnDisable` / `Stop()` 에서 모든 구독 해제. rift 교체 시 `OnStoneChanged` 재구독 패턴은 본 토글에서는 불필요 (LoadedStone 직접 추적 안 함, `IsActive` 만 관리).

- **씬 dirty 상태**
  - `RepeateGeneratebtn` 의 `OpenRiftButton` 제거 + `RepeatGenerateToggleButton` 추가는 UnityMCP `manage_components` 로 수행. 구현 단계에서 `manage_scene save` 로 baseline 확정.
