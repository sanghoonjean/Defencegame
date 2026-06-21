# Issue #286 — 균열 생성기 + 차원석 시스템 추가

## 1. 시스템 구조

신규 시스템은 기존 웨이브 / 큐브 / 아이템 시스템 위에 얇은 레이어를 얹는 형태로 구성한다.

```
┌────────────────────────────┐    consume    ┌──────────────────────┐
│   RiftGenerator (신규)     │ ─────────────▶│  WaveSystem (수정)   │
│  - DimensionStone 슬롯     │   StoneStats  │  - StartRiftWave()   │
│  - 큐브로 옵션 조작        │               │  - 옵션 보정 spawn   │
└──────────────┬─────────────┘               └──────────┬───────────┘
               │                                        │
               │ 큐브 소모/획득                         │ 적 스폰 (강화)
               ▼                                        ▼
        ┌───────────────┐                       ┌─────────────────┐
        │  CubeSystem   │                       │  EnemyData 보정 │
        │  (기존)       │                       │  (런타임 인자)  │
        └───────────────┘                       └─────────────────┘
```

### 컴포넌트 역할
- **DimensionStone**: `ItemData` 와 유사한 옵션 컨테이너. 차원석 전용 옵션 enum 사용.
- **DimensionStoneInventory**: 보유 차원석 목록을 관리하는 시스템 (단일 인스턴스).
- **RiftGenerator**: 차원석 슬롯 + 큐브 적용 API + "균열 개방" API. 슬롯의 옵션을 `RiftWaveModifiers` 로 환산해 `WaveSystem` 에 전달.
- **WaveSystem 확장**: 기존 `StartWave()` 와 별도로 `StartRiftWave(modifiers)` 진입점. 옵션 보정값을 받아 적 HP/방어/속도/수량을 곱연산 적용한 강화 스폰을 수행.
- **Enemy 확장**: `Initialize()` 가 보정 인자(곱연산 multiplier 4종)를 추가로 받도록 오버로드.
- **UI**: 균열 생성기 패널(차원석 슬롯, 옵션 표시, 큐브 적용 버튼, 균열 개방 버튼).

### 데이터 흐름
```
[플레이어] DimensionStone 1개 슬롯 장착
   ↓
RiftGenerator.SetStone(stone)
   ↓
(선택) 큐브 적용 → RiftGenerator.ApplyCube(CubeType)
                  └── CubeSystem 소비 + DimensionStone 옵션 변경
   ↓
"균열 개방" 클릭 → RiftGenerator.OpenRift()
   ↓
modifiers = BuildModifiers(stone.Options)
   ↓
WaveSystem.StartRiftWave(modifiers) → ObjectPoolSystem.Get() + Enemy.Initialize(data, stage, waypoints, modifiers)
   ↓
웨이브 클리어 시 옵션 보정에 비례한 큐브 보상 지급
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/WaveSystem.cs`
  - `RiftWaveModifiers` 구조체 추가
  - `StartRiftWave(RiftWaveModifiers)` 진입점 추가
  - `SpawnEnemies()` 가 modifiers 를 받도록 인자 추가 (기존 시그니처는 default modifier 로 호환)
  - 보상 지급 훅 (`OnWaveEnded` 이전 보너스 큐브 지급)
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
  - `Initialize()` 오버로드: `(EnemyData, int stage, Vector2[] waypoints, RiftWaveModifiers modifiers)`
  - 보정값을 곱연산으로 HP/Defense/Speed/PlayerDamage 에 적용
- `MakeDefence/Assets/Scripts/Systems/CubeSystem.cs`
  - 변경 없음 (기존 `TryConsume`/`Add` 그대로 사용)
- `MakeDefence/Assets/Scripts/Gameplay/Tower/ItemData.cs`
  - 변경 없음. 차원석은 별도 클래스(`DimensionStone`)로 분리해 옵션 enum 충돌 회피.
- `MakeDefence/Assets/Scenes/SampleScene.unity`
  - RiftGenerator 게임오브젝트 추가, UI 패널 hook (UnityMCP 로 수행)

## 3. 신규 클래스 / 파일

### 신규 C# 스크립트
- `MakeDefence/Assets/Scripts/Gameplay/Rift/DimensionStoneOptionType.cs`
  - enum: `MonsterHpBoost`, `MonsterDefenseBoost`, `MonsterSpeedBoost`, `MonsterCountBoost`, `RewardCubeBoost`, `EnemyDamageBoost`
- `MakeDefence/Assets/Scripts/Gameplay/Rift/DimensionStone.cs`
  - `ItemData` 와 유사한 구조 — Options 리스트, `Reroll`/`AddRandomOption`/`RemoveRandomOption`/`UpgradeRandomOption`/`Clone`
  - 옵션 수치 범위(Ranges) 정적 테이블 보유 (잠정값: HP/Defense/Speed +5~+30%, Count +0~+10마리, Reward +5~+30%, EnemyDamage +5~+25%)
  - `MaxOptions = 6`
- `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftWaveModifiers.cs`
  - struct: `HpMult`, `DefenseMult`, `SpeedMult`, `DamageMult`, `ExtraCount`, `RewardCubeMult`
  - `Default` 정적 멤버 (전부 1f / 0)
  - `FromOptions(IReadOnlyList<DimensionStoneOption>)` 정적 빌더
- `MakeDefence/Assets/Scripts/Systems/DimensionStoneInventory.cs`
  - 보유 차원석 리스트 + 추가/제거/획득 이벤트
  - 초기 차원석 1개 지급(인스펙터 토글, 디버그용)
- `MakeDefence/Assets/Scripts/Gameplay/Rift/RiftGenerator.cs`
  - 차원석 1개 슬롯 (장착/해제)
  - `ApplyCube(CubeType)` — `ItemSystem.ApplyCube` 와 동일 패턴으로 큐브 소비 + 차원석 옵션 변경
  - `OpenRift()` — 차원석 소모, WaveSystem.StartRiftWave 호출
  - 이벤트 `OnStoneChanged`, `OnRiftOpened`
- `MakeDefence/Assets/Scripts/UI/RiftGeneratorPanel.cs`
  - 차원석 슬롯 표시, 옵션 리스트 표시, 큐브 적용 버튼 5종 (Lower/Upper/TopTier/Delete/Clone), "균열 개방" 버튼
  - `DimensionStoneInventory` 와 연동해 보유 차원석 선택 가능

### 신규 Unity 에셋 (UnityMCP 로 작성)
- `MakeDefence/Assets/Prefabs/RiftGenerator.prefab` — RiftGenerator 컴포넌트 부착
- `MakeDefence/Assets/Prefabs/UI/RiftGeneratorPanel.prefab` — 패널 UI
- SampleScene 에 RiftGenerator 인스턴스 배치 및 WaveSystem/CubeSystem 참조 연결

## 4. 테스트 계획

### 단위 검증 (PlayMode 테스트 또는 수동)
1. **DimensionStone 옵션 CRUD**
   - `CreateRandom` 후 Options.Count == 1
   - `AddRandomOption` 6회까지 성공, 7번째 false
   - `RemoveRandomOption` 마지막 1개는 false
   - `UpgradeRandomOption` 값이 1.5배 (max clamp) 적용
   - `Clone` 후 옵션 동일
2. **RiftGenerator 큐브 적용**
   - Lower 큐브 → Reroll 동작, 큐브 1개 소비
   - Upper 큐브 → 옵션 추가, 큐브 1개 소비
   - 큐브 부족 시 false 반환, 차원석 변경 없음
3. **균열 개방 → 강화 스폰**
   - HP Boost +30% 옵션 차원석으로 개방 → 적 MaxHp 가 (baseHp * stageMult * 1.3) 인지 검증
   - MonsterCountBoost +5 → 스폰 적 수가 base + 5 인지
4. **보상**
   - RewardCubeBoost +20% → 웨이브 클리어 후 추가 큐브 지급

### 수동 검증 체크리스트
- [ ] SampleScene 에서 RiftGenerator 오브젝트 인터랙션 가능
- [ ] 패널 UI 가 차원석 옵션 표시
- [ ] 5종 큐브 버튼이 활성/비활성 (보유 큐브 수에 따라)
- [ ] 균열 개방 시 WaveSystem 이 정상 활성화
- [ ] 강화된 적이 일반 적보다 HP/속도가 더 높음 (디버그 로그 확인)
- [ ] 클리어 후 보너스 큐브 지급
- [ ] 차원석이 소모되어 인벤토리에서 제거

## 5. 위험 요소

### 사이드 이펙트
- **WaveSystem 진입점 분기**: 기존 `StartWave()` 와 `StartRiftWave()` 가 공존. 두 흐름이 동시에 활성화되지 않도록 `IsWaveActive` 가드 유지.
- **Enemy.Initialize 시그니처 변경**: 기존 호출처(WaveSystem 일반 스폰)는 default modifier 로 호출되도록 오버로드 분리. 기존 동작 무변화 확인 필요.
- **자동 웨이브(`_autoWave`)**: 균열 웨이브 클리어 후 auto 가 켜져 있으면 일반 스테이지가 다시 시작될 수 있음 → 균열 웨이브는 auto 미연동 (1회성)으로 고정.

### 미확정 항목
- 옵션 수치 범위: HP/Defense/Speed Boost 의 min/max 는 잠정값(+5~+30%)으로 두고 Inspector 에서 후속 튜닝.
- 차원석 획득 경로: 본 이슈 범위에서는 디버그용 초기 지급 + 균열 클리어 시 일정 확률 드랍(잠정 30%)으로 두고, 별도 이슈에서 드랍 테이블 정식화.
- 균열 생성기 배치 방식: 본 이슈에서는 SampleScene 에 고정 배치(상점/UI 패널 형태). 맵에 직접 설치(Tower 처럼)는 후속 작업.

### 주의사항
- **차원석 옵션 enum 은 `ItemOptionType` 과 분리**: 같은 enum 을 재사용하면 Tower 의 `AccumulateOption` 이 차원석 옵션을 잘못 합산할 위험. 별도 enum `DimensionStoneOptionType` 으로 분리한다.
- **UnityMCP 로만 prefab/scene 수정**: AGENTS.md §7 가이드에 따라 `.prefab`/`.unity`/`.meta` 직접 YAML 편집 금지.
- **풀 시스템 영향**: 강화 스폰도 `ObjectPoolSystem` 을 그대로 사용. 보정값은 풀에서 꺼낸 후 `Initialize` 단계에서 한 번만 적용 (반환 시 자동 리셋되어야 함 — 현재 `Enemy.Initialize` 가 매번 덮어쓰므로 안전).
