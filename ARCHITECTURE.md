# ARCHITECTURE.md — MakeDefence

## 1. 시스템 목록

| 시스템 | 역할 | 중요도 |
|--------|------|--------|
| **GameStateSystem** | 전체 게임 상태 관리 (상태 머신) | 필수 |
| **WaveSystem** | 웨이브 생성, 난이도, 적 등급 비율 | 필수 |
| **EnemySystem** | 적 생성, 이동, 등급별 스탯 | 필수 |
| **TowerSystem** | 타워 배치, 공격 | 필수 |
| **CubeSystem** | 자원(큐브) 획득 및 소비 | 필수 |
| **ItemSystem** | 아이템 옵션 추가/제거/리롤/복제 | 필수 |
| **InventorySystem** | 타워별 3슬롯 인벤토리 관리 | 필수 |
| **PlayerSystem** | 플레이어 체력 관리 | 필수 |
| **BossSystem** | 라스트 보스 도전/처치 | 필수 |
| **MapTileSystem** | 타일 데이터, 타워 배치 가능 여부 | 필수 |
| **PathfindingSystem** | 웨이포인트 기반 적 이동 경로 | 필수 |
| **SaveLoadSystem** | 진행 상황 저장/불러오기 | 필수 |
| **SoundSystem** | BGM, 효과음 재생 | 필수 |
| **UISystem** | HUD, 인벤토리 UI, 웨이브 UI | 필수 |
| **ObjectPoolSystem** | 적/투사체 오브젝트 풀링 | 필수 |
| **VFXSystem** | 히트/스킬 파티클 이펙트 | 선택 |

---

## 2. 게임 상태 머신 (GameStateSystem)

```
[MainMenu]
    ↓ 게임 시작
[Playing]
    ↓ 웨이브 종료
[WaveResult]      ← 큐브 보상 수령, 인벤토리 정리
    ↓ 다음 웨이브 or 보스 도전 선택
[Playing] or [BossChallenge]
    ↓ 보스 처치        ↓ 보스 패배
[Victory]          [Defeat]
```

상태 전환은 `GameStateSystem`이 C# 이벤트로 브로드캐스트하고,
각 시스템은 해당 이벤트를 구독하여 동작.

---

## 3. 시스템 의존성

```
GameStateSystem
    ↑ (상태 변경 이벤트 구독)
    ├── WaveSystem
    │     ├── EnemySystem       → ObjectPoolSystem
    │     │     └── PathfindingSystem → MapTileSystem
    │     └── CubeSystem        (웨이브 클리어 시 큐브 드롭)
    │
    ├── TowerSystem             → MapTileSystem
    │     └── InventorySystem   → ItemSystem
    │                                └── CubeSystem
    │
    ├── PlayerSystem            (적 기지 통과 시 HP 감소)
    │
    ├── BossSystem              → PlayerSystem
    │
    ├── UISystem                (모든 시스템 상태 읽기)
    ├── SoundSystem             (이벤트 수신 후 재생)
    ├── VFXSystem               → ObjectPoolSystem
    └── SaveLoadSystem          (전체 상태 직렬화)
```

**의존성 원칙**
- 하위 시스템은 상위 시스템을 직접 참조하지 않음
- 시스템 간 통신은 C# 이벤트 또는 ScriptableObject 이벤트 채널 사용
- `UISystem`과 `SaveLoadSystem`은 다른 시스템을 읽기만 함 (단방향)

---

## 4. 시스템 간 통신 방식

### C# 이벤트 (기본)
같은 씬 내 시스템 간 통신에 사용.

```csharp
// 예시
public static event Action<int> OnWaveCleared;   // WaveSystem → CubeSystem
public static event Action<float> OnPlayerDamaged; // EnemySystem → PlayerSystem
public static event Action OnBossDefeated;         // BossSystem → GameStateSystem
```

### ScriptableObject 이벤트 채널 (씬 간 필요 시)
씬 전환이 있거나 에디터에서 디버깅이 필요한 이벤트에 사용.

```
Assets/Data/Events/
├── OnWaveClearedEvent.asset
├── OnPlayerDamagedEvent.asset
└── OnGameStateChangedEvent.asset
```

---

## 5. 핵심 데이터 흐름

### 웨이브 진행 흐름
```
WaveSystem.StartWave()
  → EnemySystem.SpawnEnemy()  (ObjectPool에서 꺼냄)
  → Enemy.Move()              (PathfindingSystem 웨이포인트 따라 이동)
  → Enemy 기지 도달           → PlayerSystem.TakeDamage()
  → Enemy 처치                → VFXSystem, SoundSystem
  → 전체 적 처치              → WaveSystem.OnWaveCleared()
                              → CubeSystem.DropCubes()   (가중치 랜덤)
```

### 큐브 사용 흐름
```
플레이어 큐브 사용
  → CubeSystem.UseCube(cubeType)
  → ItemSystem.ApplyEffect(cubeType, item)
      하위 큐브  → 옵션 전체 리롤
      상위 큐브  → 옵션 1개 추가
      최상위 큐브 → 옵션 1개 제거 + 상위 교체
      삭제 큐브  → 옵션 1개 랜덤 제거
      복제형 큐브 → 아이템 복제
  → InventorySystem.UpdateSlot()
  → TowerSystem.RefreshStats()
```

### 보스 도전 흐름
```
플레이어 보스 도전 선택
  → GameStateSystem.ChangeState(BossChallenge)
  → BossSystem.StartChallenge()
  → 보스 처치   → GameStateSystem.ChangeState(Victory)
  → 보스 패배   → GameStateSystem.ChangeState(Defeat)
```

---

## 6. 데이터 구조 (ScriptableObject)

```
Assets/Data/
├── Enemies/
│   ├── EnemyStatData.asset      # 등급별 기본 스탯
│   └── WaveEnemyConfig.asset    # 웨이브 등급 비율 설정
├── Towers/
│   └── TowerData.asset          # 타워 기본 스탯
├── Items/
│   ├── ItemOptionTable.asset    # 옵션 종류 테이블 (추후 확정)
│   └── CubeDropTable.asset      # 큐브 드롭 가중치
├── Map/
│   └── WaypointData.asset       # 웨이포인트 경로 데이터
└── Events/
    └── ...                      # SO 이벤트 채널
```

---

## 7. ObjectPool 구조

- 씬 시작 시 Enemy, Projectile, VFX 오브젝트를 미리 생성해 풀에 보관
- `ObjectPoolSystem.Get(type)` / `ObjectPoolSystem.Return(obj)` 인터페이스
- `EnemySystem`, `TowerSystem`, `VFXSystem`이 풀에서 꺼내고 반납

---

## 8. SaveLoad 대상 데이터

| 데이터 | 설명 |
|--------|------|
| 해금된 웨이브 난이도 단계 | 1~16단계 진행 현황 |
| 보유 큐브 수량 | 5종 큐브 각각 |
| 타워 배치 정보 | 위치, 종류, 인벤토리 아이템 |
| 아이템 옵션 상태 | 각 아이템의 현재 옵션 목록 |
| 플레이어 체력 | 현재 HP |

---

## 9. 미확정 항목 (추후 확정 필요)

- [ ] 타워 종류 및 스탯
- [ ] 아이템 옵션 전체 테이블
- [ ] 큐브 드롭 가중치 수치
- [ ] 맵 레이아웃 및 웨이포인트 경로
- [ ] 라스트 보스 패턴 및 스탯
- [ ] VFXSystem 상세 설계
