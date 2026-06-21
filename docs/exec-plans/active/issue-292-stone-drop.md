# Issue #292 — 차원석 드랍 시스템 (적 처치 시 일정 확률)

## 결정 사항
- **트리거**: 적 처치 시 일정 확률 드랍 — `DroppedCubeSystem` / `DroppedCubePickup` 패턴 그대로 재사용
- **수확**: 웨이브 클리어 시 일괄 → `DimensionStoneInventory.Add(DimensionStone.CreateRandom())`. 패배 시 폐기.
- **잠정 확률**: Normal 0.5% / Magic 2% / Rare 5% / Unique 20% / LastBoss 100% (Inspector 튜닝)
- **드랍 수**: 등급별 default 1 (LastBoss 2)

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
DroppedStoneSystem.CollectAll() ────── 각 픽업 → DimensionStoneInventory.Add(CreateRandom())
DroppedStoneSystem.DiscardAll()  ────── 패배 시 폐기
```

### 컴포넌트 역할
- **DroppedStoneSystem**: DroppedCubeSystem 과 동일 패턴의 컨트롤러. 등급별 확률/카운트 SerializeField, `_dropsBlocked` 가드, CollectAll/DiscardAll, OnPendingChanged 이벤트.
- **DroppedStonePickup**: DroppedCubePickup 의 시각 효과(spawn pop/pulse/collect arc/discard fade) 그대로 사용. 차원석 sprite + 보라 톤 라벨 스타일. 수확 도착 시점에 `DimensionStoneInventory.Add(DimensionStone.CreateRandom())` 1회.
- **수확 목적지**: 화면 우측 `DimesionStoneInventoryUI` 패널 위치 (`Camera.main.WorldToScreenPoint` 기반) — 좌표가 동적이라 CollectTarget 계산은 패널의 RectTransform 위치를 ScreenToWorld 로 변환.

### 데이터 흐름
```
Input
 ↓ 적 사망 (Enemy.OnEnemyDied)
DroppedStoneSystem.HandleEnemyDied
 ↓ 등급별 확률 roll
 ↓ pass → Instantiate(stonePickupPrefab)
 ↓ pickup.Initialize(deathPos)
 ↓ _activePickups.Add(pickup), _pending++
 ↓
[웨이브 진행]
 ↓ WaveSystem.OnWaveEnded(true)
DroppedStoneSystem.CollectAll
 ↓ pickup.StartCollect(target, duration, onArrived)
 ↓ onArrived → DimensionStoneInventory.Add(DimensionStone.CreateRandom())
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
  - SerializeField: stonePickupPrefab, 등급별 chance(5종)/count(5종), collect/discard 튜닝
  - Enemy.OnEnemyDied / WaveSystem.OnWaveStarted / OnWaveEnded 구독
  - `_dropsBlocked` 가드, _activePickups HashSet, _pending count
- `MakeDefence/Assets/Scripts/Gameplay/DroppedStonePickup.cs`
  - DroppedCubePickup 의 비주얼 패턴 차용 — body/beam SpriteRenderer + 라벨(Text)
  - LabelStyle 1종 (차원석 보라 톤) — 등급 구분 없음
  - PulseStyle / Spawn/Collect Arc / Discard Fade 동일
  - 수확 도착 시점에 외부 콜백 호출 (DroppedStoneSystem 이 인벤 추가)

### 신규 Unity 에셋 (UnityMCP)
- `MakeDefence/Assets/Prefabs/DroppedStonePickup.prefab`
  - DroppedCubePickup.prefab 의 구조 차용 → 색상만 보라 톤. SpriteRenderer + Collider2D + DroppedStonePickup 컴포

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
   - `RollDrop(EnemyGrade.Normal)` → 잠정 (0.005, 1)
   - 동일 패턴 Magic/Rare/Unique/LastBoss
   - Inspector 값을 reflection 으로 set 한 후 분기 검증 (또는 ctor injection)
2. **DimensionStoneInventoryDropTests**
   - Add 호출 → Count 증가
   - OnInventoryChanged 이벤트 1회 발행

### 수동/PlayMode 검증
- Normal 적 처치 시 차원석 픽업 거의 안 나옴 (0.5%) — 시각 확인
- Unique 처치 시 픽업 자주 발생
- 웨이브 클리어 시 수확 애니메이션 → DimesionStoneInventoryUI 카운트 증가
- 웨이브 실패 시 폐기 fade
- 균열 클릭 → 수확된 차원석을 슬롯에 장착 가능 (기존 RiftPanelToggle UI)

## 5. 위험 요소

### 사이드 이펙트
- **DroppedCubeSystem 과 동시 활성**: 두 시스템이 같은 `Enemy.OnEnemyDied` 구독. 둘 다 픽업 spawn → 시각적으로 겹침. spawnArrangementRadius + jitter 로 분산 처리 (DroppedCubeSystem 이미 사용 중). 두 시스템 동일 시드면 겹칠 위험 → DroppedStoneSystem 은 별도 jitter 패턴.
- **수확 도착 좌표**: 화면 우측 DimesionStoneInventoryUI 패널은 Rift 미선택 시 alpha 0 으로 숨겨져 있음 → CollectTarget 으로 부적합. fallback 으로 화면 우상단 fixed 좌표 사용.

### 미확정 항목
- 등급별 확률/카운트는 잠정값. 후속 밸런싱.
- 차원석 sprite — sprite-sheet.png 가 working tree 에 있으니 그 안에서 적당한 sprite 사용. 미정시 흰 동그라미 placeholder + 보라 틴트.
- LastBoss 의 count=2 가 타당한지 — 클리어 시 균열 2회 시동 가능. 게임 진행 속도에 영향.

### 주의사항
- **EditMode 테스트의 결정성**: Random 시드 고정 (`Random.InitState`) + SetUp 패턴.
- **수확 도착 콜백**: pickup.Collect 도착 시 `DimensionStoneInventory.Add` 가 한 번만 호출되도록 — 호출 위치를 시스템(콜백) 또는 pickup 자체에 명확히 분리.
- **UnityMCP prefab 생성 시 임시 GO 잔존 문제**: 이전 사이클에서 manage_gameobject save_as_prefab 후 씬에 잔존 RiftGenerator 인스턴스가 남은 적 있음. prefab 만들고 즉시 검증.
