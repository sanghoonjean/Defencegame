# Issue #286 — 균열 생성기 + 차원석 시스템 추가

## 결정 사항
- **균열 생성기 접근 방식**: 맵 타일에 설치하는 오브젝트 (Tower 와 동일하게 Buildable 타일에 설치)
- **차원석 옵션 enum**: `ItemOptionType` 과 분리 (`DimensionStoneOptionType` 신규 enum)

## 1. 시스템 구조

신규 시스템은 기존 웨이브 / 큐브 / 타일 / 아이템 / 선택 시스템 위에 얇은 레이어를 얹는 형태로 구성한다.
`Tower` 와 동일한 배치 플로우(`TowerPlacer` 패턴)를 따른다.

```
[Buildable 타일 클릭]                ┌────────────────────┐
        │                            │  InventorySystem    │
        ▼                            │  - SelectedTower    │
┌───────────────────┐                │  - SelectedRift     │ (신규 필드)
│ InputManager      │ ─ 선택 분기 ─▶│                     │
│  - 배치 모드?     │                └─────────┬───────────┘
│  - 어떤 placer ?  │                          │
└────────┬──────────┘                          │
         │                                     │ OnRiftSelected
         ▼                                     ▼
┌────────────────────┐               ┌──────────────────────┐
│ TowerPlacer        │  vs           │ RiftGeneratorPlacer  │ (신규)
│ (기존)             │               │ - Lower N 큐브 소비  │
└────────────────────┘               └──────────┬───────────┘
                                                │ Instantiate
                                                ▼
                              ┌──────────────────────────────┐
                              │   RiftGenerator (신규 컴포)   │
                              │  - DimensionStone 슬롯       │
                              │  - 큐브 적용 API             │
                              │  - OpenRift()                │
                              │  - 클릭 → SelectRift()       │
                              └──────────┬───────────────────┘
                                         │ consume
                                         ▼
                              ┌──────────────────────────────┐
                              │   WaveSystem 확장            │
                              │  StartRiftWave(modifiers)    │
                              └──────────┬───────────────────┘
                                         │ Enemy.Initialize(..., modifiers)
                                         ▼
                              ┌──────────────────────────────┐
                              │   강화 적 스폰 + 클리어 보상 │
                              └──────────────────────────────┘
```

### 컴포넌트 역할
- **RiftGeneratorPlacer**: `TowerPlacer` 와 동일 구조. Buildable 타일에 RiftGenerator 인스턴스를 배치. 설치 비용은 Lower 큐브 N개(잠정 10).
- **RiftGenerator (MonoBehaviour)**: 타일 위에 존재하는 게임 오브젝트. 차원석 슬롯 + 큐브 적용 API + `OpenRift()` + 클릭 시 선택. `OnDestroy` 시 `MapTileSystem.RemoveTower` 처럼 자기 셀에서 등록 해제.
- **InventorySystem 확장**: `SelectedRift` 필드 + `OnRiftSelected` 이벤트 추가. Tower 선택과 상호 배타(둘 중 하나만 활성).
- **InputManager / 선택 분기**: Buildable 타일 클릭 시 (a) 빈 타일 → 현재 배치 모드의 placer 호출 (b) Tower 가 있음 → Tower 선택 (c) RiftGenerator 가 있음 → Rift 선택.
- **DimensionStone**: `ItemData` 와 유사한 옵션 컨테이너. 차원석 전용 enum 사용.
- **DimensionStoneInventory**: 보유 차원석 목록을 관리하는 시스템.
- **RiftWaveModifiers** struct: HP/Defense/Speed/Damage 곱연산 + 추가 스폰 수 + 보상 배율. `DimensionStone.Options` 로부터 변환.
- **WaveSystem 확장**: `StartRiftWave(modifiers)` 진입점. 일반 `StartWave()` 와 동시 활성 금지.
- **Enemy 확장**: `Initialize()` 가 modifiers 를 받도록 오버로드.
- **UI**:
  - `RiftGeneratorPanel` — Tower 의 UnitPanel 자리에 표시되는 선택 패널(차원석 슬롯, 옵션 표시, 큐브 적용 5종, "균열 개방" 버튼).
  - `BuildModeToggleButton` (또는 기존 HUD 확장) — Tower 배치 / Rift 배치 모드 전환.

### 데이터 흐름
```
[빌드 모드: Rift]
플레이어가 Buildable 타일 클릭
  ↓
InputManager → RiftGeneratorPlacer.TryPlace(coord)
  ↓
Lower 큐브 N개 소비 → RiftGenerator prefab Instantiate → MapTileSystem.PlaceTower(coord, this)
  ↓
[선택 모드]
플레이어가 RiftGenerator 타일 클릭
  ↓
InventorySystem.SelectRift(rift) → RiftGeneratorPanel 표시
  ↓
(선택) 차원석 슬롯에 차원석 장착 / 해제
  ↓
(선택) 큐브 적용 → RiftGenerator.ApplyCube(CubeType)
        └── CubeSystem 소비 + DimensionStone 옵션 변경
  ↓
"균열 개방" 클릭 → RiftGenerator.OpenRift()
  ↓
modifiers = RiftWaveModifiers.FromOptions(stone.Options)
  ↓
WaveSystem.StartRiftWave(modifiers)
  ↓
SpawnEnemies(list + extraCount) → Enemy.Initialize(data, stage, waypoints, modifiers)
  ↓
웨이브 클리어 시 baseReward * RewardCubeMult 큐브 지급, 차원석 1개 소모
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
  - `StartRiftWave(RiftWaveModifiers)` 진입점 추가
  - `SpawnEnemies()` 가 modifiers 를 받도록 인자 추가
  - 균열 웨이브는 `_autoWave` 미연동(1회성), 클리어 보상 훅
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
  - `Initialize()` 오버로드: `(EnemyData, int stage, Vector2[] waypoints, RiftWaveModifiers modifiers)`
  - 보정값을 곱연산으로 HP/Defense/Speed/PlayerDamage 에 적용
- `MakeDefence/Assets/Scripts/Systems/InventorySystem.cs`
  - `SelectedRift` 필드 + `SelectRift(rift)` / `DeselectRift()` 메서드
  - `OnRiftSelected` 이벤트
  - 기존 `SelectTower` 와 상호 배타 처리
- `MakeDefence/Assets/Scripts/Systems/InputManager.cs`
  - Buildable 타일 클릭 분기에 RiftGenerator 처리 추가 (Tower 와 동일 패턴)
  - 배치 모드(Tower / Rift) 분기
- `MakeDefence/Assets/Scripts/Systems/MapTileSystem.cs`
  - `_placedTowers` 와 별도로 `_placedRifts` 추가 (또는 공통 인터페이스 `ITilePlaceable` 도입)
  - 현 단계는 dictionary 분리 방식으로 가장 작은 변경
- `MakeDefence/Assets/Scripts/UI/UnitPanelController.cs`
  - Rift 선택 시 RiftGeneratorPanel 토글 (기존 Tower 패널과 배타)
- `MakeDefence/Assets/Scenes/SampleScene.unity`
  - RiftGeneratorPlacer 컴포넌트 배치, RiftGeneratorPanel UI 연결 (UnityMCP)

## 3. 신규 클래스 / 파일

### 신규 C# 스크립트
- `MakeDefence/Assets/Scripts/Gameplay/Rift/DimensionStoneOptionType.cs`
  - enum: `MonsterHpBoost`, `MonsterDefenseBoost`, `MonsterSpeedBoost`, `MonsterCountBoost`, `RewardCubeBoost`, `EnemyDamageBoost`
- `MakeDefence/Assets/Scripts/Gameplay/Rift/DimensionStone.cs`
  - `ItemData` 와 유사한 구조 — Options 리스트, `Reroll`/`AddRandomOption`/`RemoveRandomOption`/`UpgradeRandomOption`/`Clone`
  - 옵션 수치 범위(Ranges) 정적 테이블 (잠정값: HP/Defense/Speed +5~+30%, Count +0~+10마리, Reward +5~+30%, EnemyDamage +5~+25%)
  - `MaxOptions = 6`
- `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftWaveModifiers.cs`
  - struct: `HpMult`, `DefenseMult`, `SpeedMult`, `DamageMult`, `ExtraCount`, `RewardCubeMult`
  - `Default` 정적 멤버 (전부 1f / 0)
  - `FromOptions(IReadOnlyList<DimensionStoneOption>)` 정적 빌더
- `MakeDefence/Assets/Scripts/Systems/DimensionStoneInventory.cs`
  - 보유 차원석 리스트 + 추가/제거/획득 이벤트
  - 디버그용 초기 차원석 1개 지급 토글
- `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftGenerator.cs`
  - MonoBehaviour. `Vector2Int TileCoord` 보유, `Place(coord)` / `OnDestroy()` 에서 MapTileSystem 등록·해제 (Tower 패턴)
  - 차원석 슬롯 1개 (장착/해제)
  - `ApplyCube(CubeType)` — ItemSystem.ApplyCube 와 동일 패턴
  - `OpenRift()` — 차원석 소모 + WaveSystem.StartRiftWave 호출
  - 클릭 처리 → InventorySystem.SelectRift(this)
  - 이벤트: `OnStoneChanged`, `OnRiftOpened`
- `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftGeneratorPlacer.cs`
  - `TowerPlacer` 와 동일 구조. `TryPlace(Vector2Int coord)` 가 Lower 큐브 N개(잠정 10) 소비 후 RiftGenerator prefab Instantiate
- `MakeDefence/Assets/Scripts/UI/RiftGeneratorPanel.cs`
  - 차원석 슬롯 표시, 옵션 리스트 표시, 큐브 적용 버튼 5종, "균열 개방" 버튼
  - DimensionStoneInventory 와 연동해 보유 차원석 선택 가능
- `MakeDefence/Assets/Scripts/UI/BuildModeToggleButton.cs` (또는 기존 HUD 에 토글 추가)
  - Tower 배치 / Rift 배치 모드 전환

### 신규 EditMode 테스트 (AGENTS.md §8 — 순수 C# 로직 자동 테스트 필수)
- `MakeDefence/Assets/Tests/EditMode/MakeDefence.Tests.EditMode.asmdef` (디렉터리/asmdef 신규)
  - `nunit.framework`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner` 참조
- `MakeDefence/Assets/Tests/EditMode/Rift/DimensionStoneTests.cs`
  - `CreateRandom` 후 Options.Count == 1
  - `AddRandomOption` 을 반복 호출하면 **5회까지 성공, 6번째 호출에서 false** 이고 Options.Count == MaxOptions (== 6)
  - `RemoveRandomOption` 마지막 1개에서는 false
  - `UpgradeRandomOption` 값이 1.5배 (Ranges.max clamp)
  - `Clone` 후 옵션 동일성
- `MakeDefence/Assets/Tests/EditMode/Rift/RiftWaveModifiersTests.cs`
  - `Default` 값: HpMult/DefenseMult/SpeedMult/DamageMult == 1f, ExtraCount == 0, RewardCubeMult == 1f
  - `FromOptions` 가 각 옵션 타입을 의도한 필드에 곱연산/가산으로 누적
  - 빈 옵션 리스트 → `Default` 와 동일
- `MakeDefence/Assets/Tests/EditMode/Rift/RiftRewardCalculatorTests.cs`
  - 보상 큐브 수 = `baseReward * RewardCubeMult` 의 라운딩/Clamp 동작

### 신규 Unity 에셋 (UnityMCP 로 작성)
- `MakeDefence/Assets/Prefabs/RiftGenerator.prefab` — RiftGenerator 컴포넌트 + SpriteRenderer + Collider2D 부착
- `MakeDefence/Assets/Prefabs/UI/RiftGeneratorPanel.prefab` — 패널 UI
- SampleScene 에:
  - RiftGeneratorPlacer 컴포넌트가 붙은 게임오브젝트 추가
  - RiftGeneratorPanel UI 패널 추가 + UnitPanelController 와 연결
  - BuildModeToggleButton 또는 기존 HUD 확장

## 4. 테스트 계획

AGENTS.md §8 정책에 따라 **순수 C# 로직은 EditMode 테스트로 자동화**, `MonoBehaviour`/씬/UI 의존 시나리오는 수동 검증으로 분리한다.

### EditMode 자동 테스트 (필수)
대상은 `MonoBehaviour` 의존이 없는 순수 로직만.

1. **DimensionStone 옵션 CRUD** (`DimensionStoneTests.cs`)
   - `CreateRandom` 후 `Options.Count == 1`
   - `AddRandomOption` 반복 호출 — **5회까지 true, 6번째 false** (CreateRandom 으로 이미 1개 존재 + `MaxOptions == 6`)
   - `AddRandomOption` 종료 후 `Options.Count == 6` 이고 옵션 타입 중복 없음
   - `RemoveRandomOption` 마지막 1개에서는 false (최소 1개 보장)
   - `UpgradeRandomOption` 후 선택 옵션 값이 1.5배 (해당 Ranges.max 로 clamp)
   - `Clone` 후 옵션 리스트 deep copy 동일
2. **RiftWaveModifiers 변환** (`RiftWaveModifiersTests.cs`)
   - `Default` — HpMult/DefenseMult/SpeedMult/DamageMult == 1f, ExtraCount == 0, RewardCubeMult == 1f
   - `FromOptions` — 각 옵션 타입이 의도한 필드에 곱연산(HpMult 등) / 가산(ExtraCount) 으로 누적
   - 빈 옵션 리스트 → `Default` 와 동일
3. **보상 계산** (`RiftRewardCalculatorTests.cs`)
   - 보상 큐브 수 = `baseReward * RewardCubeMult` 라운딩/Clamp 동작 (음수/0 입력 가드 포함)

### 수동/PlayMode 검증 (MonoBehaviour·씬 의존)
4. **RiftGeneratorPlacer 설치**
   - Buildable 타일 + Lower 큐브 충분 → 설치 성공, 큐브 N개 소비, MapTileSystem 등록
   - 이미 Tower 또는 다른 Rift 가 있는 타일 → 설치 실패
   - Path / Decoration 타일 → 설치 실패
   - 큐브 부족 → 설치 실패, 차원석/큐브 변경 없음
5. **RiftGenerator 큐브 적용 (UI 흐름)**
   - Lower → Reroll, Upper → 옵션 추가, TopTier → 삭제+업그레이드, Delete → 삭제, Clone → 복제
   - (각 옵션 변경 후 패널 옵션 리스트가 갱신되는지)
6. **선택 상호 배타**
   - Rift 선택 시 Tower 선택 해제 및 패널 닫힘, 그 반대도 동일
7. **균열 개방 → 강화 스폰 (런타임 통합)**
   - HP Boost +30% 옵션 → 첫 적 MaxHp ≈ `baseHp * stageMult * 1.3` (디버그 로그)
   - MonsterCountBoost +5 → 스폰 적 수 = base + 5
   - 이미 일반 웨이브 진행 중이면 OpenRift 실패
8. **보상 지급**
   - RewardCubeBoost +20% → 클리어 후 보너스 큐브 지급 (CubeSystem 카운트 증가 확인)
9. **씬 정리**
   - Scene 종료/씬 전환 시 RiftGenerator `OnDestroy` 에서 MapTileSystem `_placedRifts` 가 비워지는지

### 수동 검증 체크리스트

### 수동 검증 체크리스트
- [ ] SampleScene 에서 Rift 배치 모드 토글이 동작
- [ ] Buildable 타일 클릭 → RiftGenerator 설치
- [ ] 설치된 RiftGenerator 클릭 → 패널 표시
- [ ] 차원석 슬롯에 보유 차원석 장착 / 해제
- [ ] 5종 큐브 버튼 활성/비활성 (보유 큐브 수에 따라)
- [ ] "균열 개방" 클릭 시 WaveSystem 활성화 + 강화 적 등장
- [ ] 강화 적이 일반 적보다 HP/속도가 더 높음 (디버그 로그)
- [ ] 클리어 후 보너스 큐브 지급
- [ ] 차원석 소모 후 인벤토리에서 제거
- [ ] Rift 선택과 Tower 선택이 동시에 활성화되지 않음

## 5. 위험 요소

### 사이드 이펙트
- **MapTileSystem 변경**: Tower 와 Rift 가 같은 Buildable 타일을 공유한다. `_placedTowers` dictionary 만 있어 Rift 등록을 위한 별도 컬렉션 또는 공통 추상화가 필요. 가장 작은 변경은 `_placedRifts` 별도 dictionary + `CanPlace` 시 양쪽 확인.
- **InputManager 클릭 분기**: 기존 Tower 클릭 로직과 Rift 클릭 로직이 동일 셀 단위로 동작. 셀에 Rift 가 있으면 Tower 분기로 빠지지 않도록 조건 순서 주의.
- **WaveSystem 분기**: 일반 `StartWave()` 와 `StartRiftWave()` 가 공존. `IsWaveActive` 가드로 동시 활성 금지. 균열 웨이브는 `_autoWave` 미연동(1회성)으로 고정해 다음 일반 웨이브가 자동 트리거되지 않게 한다.
- **InventorySystem 선택 상호 배타**: Rift 선택 시 Tower 선택을 해제하고 그 반대도 해제. UI 패널이 두 개 동시 표시되지 않도록 `UnitPanelController` 가 양쪽 이벤트를 구독해 한 번에 하나만 띄운다.
- **Enemy.Initialize 시그니처**: 오버로드로 분리. 기존 호출처는 default modifier 호출 (또는 그대로 호출 → 내부에서 `RiftWaveModifiers.Default` 사용).

### 미확정 항목
- **설치 비용**: 잠정 Lower 10개. 후속 튜닝.
- **옵션 수치 범위**: 잠정값(+5~+30% 등) → Inspector 튜닝.
- **차원석 획득 경로**: 본 이슈에서는 디버그용 초기 지급 + 균열 클리어 시 일정 확률 드랍(잠정 30%). 정식 드랍 테이블은 별도 이슈.
- **균열 웨이브의 스테이지 차원 처리**: 현재 `CurrentStage` 를 그대로 사용. 별도 "균열 난이도" 개념은 후속 이슈로 분리.

### 주의사항
- **차원석 옵션 enum 은 `ItemOptionType` 과 분리**: `DimensionStoneOptionType` 별도 enum. Tower 의 `AccumulateOption` 이 차원석 옵션을 잘못 합산할 위험 차단.
- **UnityMCP 로만 prefab/scene 수정**: AGENTS.md §7 가이드에 따라 `.prefab`/`.unity`/`.meta` 직접 YAML 편집 금지.
- **풀 시스템 영향**: 강화 스폰도 `ObjectPoolSystem` 그대로 사용. 보정값은 `Initialize` 단계에서 매번 덮어쓰므로 반환 후 재사용 시 잔존 위험 없음.
- **타워 삭제 팝업(`TowerDeleteConfirmPopup`) 와의 분리**: 본 이슈 범위에서 Rift 삭제는 다루지 않음. 셀에 Rift 가 있을 때 Tower 삭제 UI 가 잘못 활성화되지 않도록 `UnitPanelController` 가드.
