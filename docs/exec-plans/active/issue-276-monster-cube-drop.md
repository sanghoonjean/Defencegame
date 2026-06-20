# Issue #276 — 몬스터 처치 시 큐브 드랍 기능 (픽업 → 클리어 수확)

## 1. 시스템 구조

### 전체 흐름

```
[웨이브 진행 중]
Enemy.Die()
   │ (OnEnemyDied)
   ▼
DroppedCubeSystem.HandleEnemyDied(enemy)
   │ 1) Grade → (chance, count) 결정
   │ 2) 확률 롤
   │ 3) count 회 RollDropType() → DroppedCubePickup 스폰
   ▼
DroppedCubePickup (씬에 존재)
   - SpriteRenderer + 라벨 텍스트
   - 스폰 이펙트 (scale 펀치 + bounce)
   - 카운트 미반영 (아직 안 주움)


[웨이브 종료]
WaveSystem.OnWaveEnded(cleared)
   │
   ├── cleared = true  → DroppedCubeSystem.CollectAll()
   │       각 픽업 → HUD 도착 트윈 → CubeSystem.Add → 카운터 punch
   │
   └── cleared = false → DroppedCubeSystem.DiscardAll()
           각 픽업 → 페이드 아웃 → Destroy (Add 없음)
```

### 책임 분리

| 클래스                 | 역할                                              |
|------------------------|---------------------------------------------------|
| `Enemy`                | 변경 없음. `OnEnemyDied` 이벤트만 발행            |
| `CubeSystem`           | 카운트 보관 + 기존 웨이브-종료 일괄 드랍 그대로 유지 (변경 없음) |
| `DroppedCubeSystem`    | **신규**. 픽업 생성/관리/수확/폐기                |
| `DroppedCubePickup`    | **신규**. 씬에 떠 있는 단일 픽업 (스폰/이동/소멸) |
| `CubeUIDisplay`        | 수확 도착 좌표 제공 + 도착 시 punch (메서드 추가) |
| `WaveSystem`           | 변경 없음. 기존 `OnWaveEnded(bool)` 이벤트 재사용 |

### 좌표 변환

- 픽업은 월드 공간 (`SpriteRenderer + TextMesh`) 으로 존재.
- HUD 카운터는 `Canvas` (Screen Space) 의 `Text` 컴포넌트.
- 수확 시 `CubeUIDisplay.GetCounterWorldPoint(CubeType)` 가 카운터 `RectTransform` → 카메라 평면 월드 좌표를 반환.
- 픽업이 그 월드 좌표로 호 그리며 이동 후 Destroy.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/CubeSystem.cs`
  - **변경 없음** (기존 웨이브-종료 일괄 드랍 유지)
  - 단, `RollDrop()` 메서드를 `internal` 또는 `public` 으로 노출해 `DroppedCubeSystem` 이 가중치 재사용 가능하도록 변경
- `MakeDefence/Assets/Scripts/UI/CubeUIDisplay.cs`
  - 큐브 타입별 카운터의 월드 좌표를 반환하는 API 추가
    - `public Vector3 GetCounterWorldPoint(CubeType, Camera)` — Canvas 좌표를 카메라 평면으로 변환
  - 카운터 punch 애니메이션 (scale 1 → 1.3 → 1, 0.15s 코루틴)
    - `public void PlayPunch(CubeType)`

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/Systems/DroppedCubeSystem.cs` (신규)

```
- public static DroppedCubeSystem Instance
- [SerializeField] DroppedCubePickup pickupPrefab
- [SerializeField] Grade별 dropChance/dropCount (10개 필드)
- [SerializeField] float collectStaggerSec = 0.05f
- [SerializeField] float collectArcDuration = 0.5f
- [SerializeField] float discardFadeDuration = 0.3f

- 이벤트 구독: Enemy.OnEnemyDied, WaveSystem.OnWaveEnded
- 활성 픽업 리스트 관리 (HashSet<DroppedCubePickup>)
- HandleEnemyDied(Enemy) → 확률 롤 → SpawnPickup(type, pos)
- HandleWaveEnded(bool cleared) → CollectAll() or DiscardAll()
- Register / Unregister Pickup (씬 정리용)
```

### `MakeDefence/Assets/Scripts/Gameplay/DroppedCubePickup.cs` (신규)

```
- CubeType Type
- SpriteRenderer (단색 사각형 + tint by type)
- TextMesh / TextMeshPro (라벨)
- Initialize(CubeType, Vector2 worldPos)
- PlaySpawnEffect() — scale 0→1.2→1 + y bounce, 0.25s 코루틴
- StartCollect(Vector3 targetWorldPos, float duration, Action onArrived) — 호 보간 후 콜백
- StartDiscard(float fadeDuration) — alpha 1→0 후 Destroy
- OnDestroy → DroppedCubeSystem.Unregister(this)
```

### `MakeDefence/Assets/Prefabs/DroppedCubePickup.prefab` (신규)
- 루트: 빈 GameObject + `DroppedCubePickup` 컴포넌트
- 자식: `SpriteRenderer` (Sprites/Default 흰 사각형 + scale 0.4)
- 자식: `TextMesh` (3D Text, 라벨)
- UnityMCP `manage_prefabs` 로 생성

### 큐브 타입별 색 (1차)
| Type     | Color (RGBA hex) |
|----------|------------------|
| Lower    | `#A0A0A0` 회색   |
| Upper    | `#4A8BFF` 파랑   |
| TopTier  | `#FFC93A` 금색   |
| Delete   | `#E55050` 빨강   |
| Clone    | `#B07FFF` 보라   |

## 4. 테스트 계획

### 수동 검증 (Unity Play)

#### 사망 시 픽업 생성
1. Normal 적 다수 처치 → ~8% 비율로 픽업이 사망 위치에 등장
2. Rare 적 처치 → ~40% 비율로 등장
3. Unique 적 처치 → 매번 1개
4. LastBoss 처치 → 매번 3개
5. 베이스 도달 적 → 픽업 없음 (`PlayerSystem` 데미지만)
6. 스폰 이펙트(scale punch + bounce) 정상 재생

#### 웨이브 클리어 수확
7. 픽업 다수 + 클리어 → 모든 픽업이 HUD 카운터로 이동
8. 도착 순서대로 카운터 +1 + punch 애니메이션 stagger
9. 클리어 후 씬에 픽업 잔존 0개

#### 웨이브 실패 폐기
10. 픽업 다수 + 베이스 파괴 → 모든 픽업 페이드 아웃 후 사라짐
11. 큐브 카운트 변화 없음 (기존 wave-end 일괄 드랍도 cleared=false 이므로 미발생)

#### 통합 회귀
12. 기존 `CubeSystem.HandleWaveEnded` 일괄 드랍이 클리어 시 정상 작동
13. 씬 재시작 / 게임 종료 시 픽업 GameObject 잔존 없음

## 5. 위험 요소

- **후반 인플레이션**: 다수 적 동시 사망 → 픽업 다수 → 클리어 시 큐브 폭주. Normal 8% 보수적 시작, 플레이테스트 후 조정.
- **HUD 좌표 변환 정확도**: Canvas Render Mode(Overlay/Camera/World) 에 따라 변환 로직 다름.
  - 현 프로젝트의 메인 HUD Canvas Render Mode 확인 후 `RectTransformUtility.ScreenPointToWorldPointInRectangle` 또는 `Camera.ScreenToWorldPoint` 분기 적용.
- **씬 전환 시 픽업 누수**: `DontDestroyOnLoad` 미사용 가정. 씬 언로드 시 모두 같이 사라지므로 자연 정리됨. 단, 같은 씬에서 게임 재시작 시 `DroppedCubeSystem.Awake` 에서 활성 리스트 정리 필요.
- **카운터 punch 애니메이션 겹침**: stagger 로 완화 (`collectStaggerSec` 기본 50ms).
- **픽업 다수 시 시각 노이즈**: 1차에는 픽업끼리 무작위 오프셋(±0.3 유닛) 적용. 향후 자동 정렬 필요할 수 있음.
- **에셋 부재**: 큐브 타입별 아이콘 스프라이트 없음 → 단색 사각형 + 텍스트로 시작. 후속 이슈에서 아이콘 교체.
- **테스트 자동화 부재**: 수동 Play 모드 검증에 의존.
- **메모리 (Unity 에셋 편집은 UnityMCP)**: 프리팹/씬 생성·수정은 직접 YAML 편집 대신 `manage_prefabs` / `manage_scene` / `manage_components` 사용.
