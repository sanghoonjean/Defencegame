# Issue #298 — 차원석 인벤토리 슬롯에 웨이브 생성 버튼 추가

> **메모**: 차원석 인벤토리 UI 패널에는 **이미 버튼 GameObject 가 만들어져 있다** — 새 버튼을 생성하지 말고 기존 버튼을 재사용한다 (사용자 지시).

## 1. 시스템 구조

웨이브 생성(균열 개방) 백엔드는 이미 완성. 본 이슈는 **기존 UI 버튼의 동작을 OpenRift 로 교체** 하는 일.

### 현재 씬 상태 (조사 결과)

```
Canvas/DimesionStoneInventoryUI                 ← DimensionStoneInventoryView + 드롭타깃
├─ Scroll View                                  ← 차원석 슬롯 그리드
├─ Generatebtn          ★ 재사용 대상            ← Button + UIToggleButton(targetPanel=InventoryUI)
├─ RefGeneratebtn       (역할 모호 — PR 단계 결정) ← Button + UIToggleButton(targetPanel=InventoryUI)
└─ GenerateSlot                                 ← 차원석 장착 슬롯 (GenerateSlotDropTarget)
```

- `Generatebtn` 은 이름은 "Generate" 지만 현재 `UIToggleButton.targetPanel = InventoryUI` 에 묶여 단순히 **스킬 인벤토리 패널 토글** 역할. 차원석/웨이브 와는 연결되어 있지 않음 → 이 자리를 **웨이브 생성 트리거** 로 재배선.
- `RefGeneratebtn` 도 같은 토글 설정이라 임시 placeholder 로 추정. 본 작업에서 정리 여부 PR 에서 결정.

### 데이터 흐름

```
사용자 클릭 (Generatebtn)
 ↓
OpenRiftButton.OnClick()                        ★ 신규 컴포넌트
 ↓
InventorySystem.Instance.SelectedRift.OpenRift()
 ↓
RiftGenerator.OpenRift()                        (이미 구현됨)
 ├─ guard: LoadedStone != null
 ├─ guard: !WaveSystem.IsWaveActive
 ├─ guard: GameState == Playing
 ├─ RiftWaveModifiers.FromOptions(LoadedStone.Options)
 ├─ WaveSystem.StartRiftWave(mods)
 └─ DimensionStoneInventory.Remove(LoadedStone) + ClearStone()
 ↓
이벤트 fan-out → OpenRiftButton.RefreshInteractable()
 ├─ InventorySystem.OnRiftSelected
 ├─ RiftGenerator.OnStoneChanged
 └─ DimensionStoneInventory.OnInventoryChanged
```

## 2. 수정 파일

- `MakeDefence/Assets/Scenes/SampleScene.unity` (UnityMCP 로 편집)
  - `Generatebtn` 에서 `UIToggleButton` 컴포넌트 **제거**
  - `Generatebtn` 에 `OpenRiftButton` 컴포넌트 **추가**
  - (PR 결정) `RefGeneratebtn` 제거 또는 보조 트리거로 보존
- `MakeDefence/Assets/Scripts/UI/RiftGeneratorPanel.cs`
  - (PR 결정) `openRiftButton` 필드/리스너 제거 — 진입점을 인벤 버튼으로 단일화

> ⚠️ `.unity` / `.prefab` 직접 YAML 편집 비권장 — UnityMCP `manage_components` 로만 처리 ([feedback_unity_asset_edits](../../../../../.claude/projects/C--Users-kalon-Documents-GitHub-Defencegame/memory/feedback_unity_asset_edits.md)).

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/UI/OpenRiftButton.cs` (신규)

- `[RequireComponent(typeof(Button))]`
- 책임:
  - 클릭 → `InventorySystem.Instance?.SelectedRift?.OpenRift()`
  - 다음 이벤트 구독 → `Button.interactable` 자동 갱신
    - `InventorySystem.OnRiftSelected`
    - 선택된 rift 의 `OnStoneChanged` (rift 교체 시 재구독)
    - `DimensionStoneInventory.OnInventoryChanged` (필요 시)
  - `OnDisable` 에서 모든 구독 해제
- `interactable` 활성 조건 (전부 만족 시 true):
  - `SelectedRift != null`
  - `SelectedRift.LoadedStone != null`
  - `WaveSystem.Instance != null && !WaveSystem.Instance.IsWaveActive`
  - `GameStateSystem.Current == GameState.Playing`

> 별도 컴포넌트로 분리하는 이유: `DimensionStoneInventoryView` 의 그리드 재구성 책임과 격리, 다른 위치에서 동일 버튼 재사용 가능.

## 4. 테스트 계획

### 수동 (Unity Editor)

전제: SampleScene 실행, 균열 생성기 배치, 차원석 인벤에 stone ≥1.

- [ ] 차원석 미장착 상태 → `Generatebtn` 비활성 (시각적으로 dim)
- [ ] GenerateSlot 에 차원석 장착 → `Generatebtn` 활성
- [ ] `Generatebtn` 클릭 → 웨이브 시작 + GenerateSlot 비워짐 + 인벤 카운트 -1
- [ ] 웨이브 진행 중 차원석 장착해도 `Generatebtn` 비활성 유지
- [ ] 웨이브 종료(Result) 후 Playing 상태로 복귀하면 다시 활성화 검증
- [ ] Rift 선택 해제 → `Generatebtn` 비활성
- [ ] 게임 일시정지 / Defeat 상태 → `Generatebtn` 비활성
- [ ] (PR 결정에 따라) `RiftGeneratorPanel.openRiftButton` 제거 시 기존 진입점이 사라졌는지, 보존 시 두 진입점이 동일하게 동작하는지 확인

### EditMode

`OpenRiftButton` 자체는 Unity 이벤트 + 싱글톤 의존이라 단위 테스트 부담이 크다. 신규 로직이 단순하므로 EditMode 테스트는 추가하지 않고 수동 검증으로 커버 (필요 시 후속 이슈).

## 5. 위험 요소

- **`WaveSystem.IsWaveActive` 변경 이벤트 부재**
  - 사실 확인: `WaveSystem` 에 `IsWaveActive` 변경 시 발화되는 이벤트가 있는지 구현 시 grep.
  - 없으면 폴백:
    1. `Update()` 폴링 (가장 단순) — 1프레임 지연 허용 시 OK
    2. `OpenRiftButton.RefreshInteractable()` 를 `OpenRift` 성공 후 + `WaveSystem` 상태 변경 시점에서 명시 호출하도록 WaveSystem 에 이벤트 추가 (별도 이슈로 분리 가능)
  - 일단 폴링으로 진행 — 단순/저비용. 별도 이벤트 추가는 본 이슈 범위 밖.
- **`UIToggleButton` 제거 영향**
  - `Generatebtn` 의 `UIToggleButton.targetPanel = InventoryUI` 는 어디서도 다른 진입점이 없는지 확인 — `Invertorybtn` (52487 부근) 이 이미 InventoryUI 토글 담당. 중복이므로 제거해도 UX 손상 없을 것으로 추정. 구현 시 재확인.
- **`RefGeneratebtn` 처리**
  - 같은 영역에 동명 placeholder 가 둘. PR 단계에서 사용자에게 의도 확인 후 제거/유지 결정.
- **이벤트 누수**
  - rift 가 교체될 때 이전 rift 의 `OnStoneChanged` 구독을 반드시 해제해야 함 — `_current` 캐싱 + `HandleRiftSelected` 패턴 (이미 `RiftGeneratorPanel`, `GenerateSlotDropTarget` 에서 동일 패턴 사용 — 그대로 답습).
- **씬 dirty 상태**
  - 현재 SampleScene 이 isDirty=true. 구현 시작 전에 사용자에게 작업 중인 변경이 있는지 확인하거나, `manage_scene save` 로 baseline 정리.
