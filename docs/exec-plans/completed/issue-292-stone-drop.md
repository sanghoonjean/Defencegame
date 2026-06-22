# Issue #292 — 차원석 드랍 시스템 (적 처치 시 일정 확률)

## 결정 사항
- **트리거 A — 적 처치 시 확률 드랍**: Lower 큐브 (= 기존 `DroppedCubeSystem` 의 등급별 드랍 확률) 와 동일
  - Normal 8% / Magic 20% / Rare 40% / Unique 100% / LastBoss 100%
  - 드랍 수: 등급별 default 1 (LastBoss 2)
- **트리거 B — 웨이브 클리어 시 보장 1개**: 위 확률 드랍과 별개로 클리어 시 무조건 차원석 1개 인벤 추가
- **수확**: 적 사망으로 생긴 픽업은 웨이브 클리어 시 일괄 → `DimensionStoneInventory.Add(DimensionStone.CreateRandom())`. 패배 시 폐기.
- **클리어 보장 1개의 시각 처리**: pickup spawn 없이 `OnWaveEnded(true)` 시점에 직접 `Add(CreateRandom())` — 큐브 보너스(`RiftRewardCalculator` 가 부여하는 Lower 보너스) 와 동일한 모델.

## 1. 시스템 구조

큐브 드랍과 평행한 트랙으로 구성. Enemy 이벤트와 Wave 이벤트를 둘 다 구독해 같은 라이프사이클로 동작.

```
[적 사망]
  │ Enemy.OnEnemyDied
  ▼
┌─────────────────────────┐         ┌─────────────────────────┐
│   DroppedCubeSystem     │         │  DroppedStoneSystem (신규) │
│   (기존)                 │         │                          │
│   - 등급별 확률 → 큐브   │         │   - 등급별 확률 → 차원석 │
│   - SpawnPickup(cube)   │         │   - SpawnPickup()        │
└─────────────────────────┘         └────────────┬─────────────┘
                                                 │ Instantiate
                                                 ▼
                                       ┌──────────────────────┐
                                       │  DroppedStonePickup  │ (신규)
                                       │   - 보라 톤 비주얼   │
                                       │   - 수확 애니메이션  │
                                       └──────────┬───────────┘
                                                  │
[웨이브 종료]                                      │
  │ WaveSystem.OnWaveEnded(true)                  │
  ▼                                               │
DroppedStoneSystem.CollectAll() ────── 각 픽업: fade out + Inventory.Add(CreateRandom())
                                + GrantClearBonus() — 무조건 1개 추가 (픽업 없이 즉시)
DroppedStoneSystem.DiscardAll()  ────── 패배 시 fade out 만 (인벤 추가 없음)
```

### 컴포넌트 역할
- **DroppedStoneSystem**: DroppedCubeSystem 과 동일 패턴의 컨트롤러. 등급별 확률/카운트 SerializeField, `_dropsBlocked` 가드, CollectAll/DiscardAll, OnPendingChanged 이벤트.
- **DroppedStonePickup**: DroppedCubePickup 의 spawn pop / pulse / discard fade 만 차용. **수확 아크 애니메이션은 사용하지 않음** — 클리어 시 그 자리에서 fade out 하고 인벤에 즉시 +1. 수확 도착 좌표 계산 불필요.
- **수확 시점**: 클리어 → 픽업 fade 시작과 동시에 `DimensionStoneInventory.Add(CreateRandom())` 1회 호출. 도착 좌표 / Camera 의존 제거.
- **DimensionStoneInventoryView** (신규): 사용자 `Canvas/DimesionStoneInventoryUI` 안의 ScrollRect Content 에 부착. `DimensionStoneInventory.OnInventoryChanged` 구독 → 보유 차원석 1개당 슬롯 1개를 GridLayoutGroup 자식으로 인스턴스. 인벤이 늘면 슬롯도 추가, 줄면 비활성/Destroy. 슬롯 클릭 시 현재 선택된 RiftGenerator 의 슬롯에 장착(기존 RiftStoneSlot 동작 패턴).

### 데이터 흐름
```
Input
 ↓ 적 사망 (Enemy.OnEnemyDied)
DroppedStoneSystem.HandleEnemyDied
 ↓ 등급별 확률 roll
 ↓ pass → Instantiate(stonePickupPrefab) at deathPos
 ↓ _activePickups.Add(pickup), _pending++
 ↓
[웨이브 진행]
 ↓ WaveSystem.OnWaveEnded(true)
DroppedStoneSystem.CollectAll
 ↓ 각 픽업: pickup.StartCollectFade(duration) + Inventory.Add(CreateRandom())
 ↓ GrantClearBonus() — Inventory.Add(CreateRandom())
Output
 ↓ 인벤토리 UI 카운트 증가 (RiftPanelToggle 가 보일 때 자동 갱신)
```

## 2. 수정 파일

- `MakeDefence/Assets/Scenes/SampleScene.unity`
  - DroppedStoneSystem GO 추가 + stonePickupPrefab 참조 연결 (UnityMCP)
- `docs/exec-plans/active/issue-292-stone-drop.md` (본 플랜)

기존 코드는 수정 없이 신규 시스템만 추가 (DroppedCubeSystem 의 추상화/확장 없음 — 별도 트랙).

## 3. 신규 클래스 / 파일

### 신규 C# 스크립트
- `MakeDefence/Assets/Scripts/Systems/DroppedStoneSystem.cs`
  - DroppedCubeSystem 과 동일 구조의 MonoBehaviour 싱글톤
  - **`[DefaultExecutionOrder(-100)]` 필수** — WaveSystem(기본 0) 보다 먼저 `Enemy.OnEnemyDied` 를 구독해, 마지막 킬 → 픽업 등록 → `OnWaveEnded(true)` → `CollectAll` 순서 보장 (DroppedCubeSystem 과 동일 모델)
  - SerializeField: stonePickupPrefab, 등급별 chance(5종)/count(5종), collect/discard 튜닝
  - Enemy.OnEnemyDied / WaveSystem.OnWaveStarted / OnWaveEnded 구독
  - `_dropsBlocked` 가드, _activePickups HashSet, _pending count
- `MakeDefence/Assets/Scripts/Gameplay/DroppedStonePickup.cs`
  - DroppedCubePickup 의 비주얼 패턴 일부 차용 — body SpriteRenderer (sprite 는 사용자가 인스펙터에서 직접 설정)
  - 보라 톤 placeholder color
  - Spawn pop (pop-in scale) + alpha pulse 만 사용. **수확 아크 미사용** — 그 자리에서 fade out 후 Destroy.
  - `StartCollectFade(duration)` / `StartDiscardFade(duration)` — 둘 다 같은 alpha fade. 차이는 인벤 추가 여부(시스템 측 책임).
- `MakeDefence/Assets/Scripts/UI/DimensionStoneInventoryView.cs`
  - SerializeField: `RectTransform slotContainer` (ScrollRect Content), `DimensionStoneSlot slotPrefab`
  - OnEnable: DimensionStoneInventory.OnInventoryChanged 구독 + 즉시 Rebuild
  - 인벤 슬롯 수만큼 자식 slot 인스턴스. 보유 차원석 1개와 1:1 매핑.
- `MakeDefence/Assets/Scripts/UI/DimensionStoneSlot.cs`
  - 1칸 슬롯. Image + Button + Bind(DimensionStone)
  - 클릭 시 `InventorySystem.SelectedRift` 가 있을 때 **swap 패턴**으로 장착 (Codex P2 반영):
    1. `rift.LoadedStone != null` → `DimensionStoneInventory.Add(rift.LoadedStone)` 후 `rift.ClearStone()` (기존 stone 회수)
    2. `DimensionStoneInventory.Remove(stone)` + `rift.SetStone(stone)` (새 stone 장착)
  - 단순 덮어쓰기는 기존 stone 소실 위험이 있어 금지.

### 신규 Unity 에셋 (UnityMCP)
- `MakeDefence/Assets/Prefabs/DroppedStonePickup.prefab`
  - 최소 구성 — SpriteRenderer (sprite null, 보라 placeholder color) + DroppedStonePickup 컴포
  - **sprite 는 사용자가 인스펙터에서 직접 잡음**. 본 작업은 placeholder color 만.
- `MakeDefence/Assets/Prefabs/UI/DimensionStoneSlot.prefab`
  - RectTransform + CanvasRenderer + Image + Button + DimensionStoneSlot
  - 보라 placeholder 색. sprite 는 사용자 설정.
- SampleScene 의 `Canvas/DimesionStoneInventoryUI` 안 ScrollRect Content 에 `DimensionStoneInventoryView` 컴포 부착 + slotContainer/slotPrefab 연결

### 신규 EditMode 테스트 (AGENTS.md §8)
- `MakeDefence/Assets/Tests/EditMode/Drop/DroppedStoneSystemTests.cs`
  - 등급별 확률 분기 — Random 시드 고정 + 100회 시뮬레이션 후 분포가 잠정값 ±tolerance 안인지
  - count > 1 케이스 (LastBoss) 검증
  - _dropsBlocked 가드: WaveEnded(failed) 후 HandleEnemyDied 호출 시 SpawnPickup 무발생
- `MakeDefence/Assets/Tests/EditMode/Drop/DimensionStoneInventoryDropTests.cs`
  - Add(CreateRandom()) 호출 시 Count 증가, OnInventoryChanged 발행

테스트는 MonoBehaviour 의존을 최소화 — 시스템 클래스의 순수 분기 로직만 추출해 테스트 가능하도록 일부 `internal` 메서드 노출. 또는 `RollDrop(EnemyGrade) → (chance, count)` 정도의 작은 헬퍼만 외부 노출.

## 4. 테스트 계획

### EditMode 자동 테스트 (필수)
1. **DroppedStoneSystemTests**
   - `GetGradeDrop(EnemyGrade.Normal)` → (0.08, 1)
   - 동일 패턴 Magic(0.20)/Rare(0.40)/Unique(1.0)/LastBoss(1.0, 2)
   - Inspector 값을 reflection 으로 set 한 후 분기 검증 (또는 ctor injection)
   - 시드 고정 후 100회 시뮬레이션 분포가 잠정값 ±tolerance 안인지
2. **클리어 보장 1개**
   - `GrantClearBonus()` 호출 → DimensionStoneInventory.Count 가 1 증가
   - 패배 경로(`DiscardAll`)에선 보장 미발급
3. **DimensionStoneInventoryDropTests**
   - Add 호출 → Count 증가
   - OnInventoryChanged 이벤트 1회 발행
   - Remove 후 Count 감소 + 이벤트 1회 발행

### 수동/PlayMode 검증
- Normal 적 처치 시 차원석 픽업 가끔 등장 (8%) — 시각 확인
- Unique 처치 시 픽업 항상 발생
- 웨이브 클리어 시 픽업 fade out + 인벤 카운트 증가
- 균열 클릭 → DimesionStoneInventoryUI 가 알파 1 로 표시되고 ScrollRect Content 에 슬롯이 늘어나 있음
- 슬롯 클릭 → 차원석이 RiftGenerator 에 장착, 슬롯 사라짐
- 웨이브 실패 시 폐기 fade — 인벤 추가 없음

## 5. 위험 요소

### 사이드 이펙트
- **DroppedCubeSystem 과 동시 활성**: 두 시스템이 같은 `Enemy.OnEnemyDied` 구독. 둘 다 픽업 spawn → 시각적으로 겹침. spawnArrangementRadius + jitter 로 분산 처리 (DroppedCubeSystem 이미 사용 중). 두 시스템 동일 시드면 겹칠 위험 → DroppedStoneSystem 은 별도 jitter 패턴.
- **구독 순서 의존성** (Codex P1 반영): WaveSystem (기본 ExecutionOrder 0) 의 `HandleEnemyRemoved` 가 마지막 적 사망 시 즉시 `OnWaveEnded(true)` 를 발행한다. DroppedStoneSystem 이 그보다 늦게 활성되면 픽업 등록 전에 `CollectAll` 이 실행되어 마지막 픽업이 누락된다. → `[DefaultExecutionOrder(-100)]` 으로 명시적으로 WaveSystem 앞에 두어 구독 등록 순서 보장 (DroppedCubeSystem 의 검증된 패턴).
- **차원석 swap** (Codex P2 반영): DimensionStoneSlot 클릭 시 RiftGenerator 에 이미 LoadedStone 이 있으면 단순 덮어쓰기 → 기존 stone 소실. 반드시 기존 stone 을 인벤으로 반환(`Add` + `ClearStone`) 후 새 stone 장착(`Remove` + `SetStone`) 순으로 swap.

### 미확정 항목
- 등급별 확률/카운트는 잠정값. 후속 밸런싱.
- 차원석 sprite — 인스펙터에서 사용자가 직접 잡음 (본 작업 범위 밖, placeholder color 만).
- LastBoss 의 count=2 가 타당한지 — 클리어 시 균열 2회 시동 가능. 게임 진행 속도에 영향.

### 주의사항
- **EditMode 테스트의 결정성**: Random 시드 고정 (`Random.InitState`) + SetUp 패턴.
- **수확 도착 콜백**: pickup.Collect 도착 시 `DimensionStoneInventory.Add` 가 한 번만 호출되도록 — 호출 위치를 시스템(콜백) 또는 pickup 자체에 명확히 분리.
- **UnityMCP prefab 생성 시 임시 GO 잔존 문제**: 이전 사이클에서 manage_gameobject save_as_prefab 후 씬에 잔존 RiftGenerator 인스턴스가 남은 적 있음. prefab 만들고 즉시 검증.
