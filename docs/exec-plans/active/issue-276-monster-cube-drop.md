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
   - Sorting Layer "Pickups" (Enemy/Tower 위)
   - SpriteRenderer (단색 사각형)
   - 자식: TextMesh (라벨)
   - 자식: LightBeamSprite (위로 솟은 반투명 컬럼, Additive)
   - 부유 + 알파 펄스
   - 카운트 미반영 (아직 안 주움)
   ↓
DroppedCubeSystem.PendingCounts[type]++
   ↓ (OnPendingChanged 이벤트)
PendingDropDisplay (HUD 상단)
   - "수확 대기: Lower×3 Upper×1 ..."


[웨이브 종료]
WaveSystem.OnWaveEnded(cleared)
   │
   ├── cleared = true  → DroppedCubeSystem.CollectAll()
   │       각 픽업 → HUD 카운터로 호 이동 → CubeSystem.Add → punch
   │
   └── cleared = false → DroppedCubeSystem.DiscardAll()
           각 픽업 → 페이드 아웃 → Destroy (Add 없음)

   양쪽 모두 종료 후 PendingCounts 리셋 → PendingDropDisplay 0 표시
```

### 가시성 설계 (A + B + D)

| 레이어 | 수단 | 효과 |
|--------|------|------|
| A | "Pickups" Sorting Layer (Enemy/Tower 위) | 같은 위치에서 깔리지 않음 |
| B | 픽업 위로 솟은 빛기둥 (Additive 컬럼) | 다른 오브젝트에 본체가 가려져도 컬럼 끝이 화면 상부에 보임 |
| D | `PendingDropDisplay` HUD 카운터 | 위치 못 봐도 수량은 항상 보임 (안전망) |

### 책임 분리

| 클래스                 | 역할                                              |
|------------------------|---------------------------------------------------|
| `Enemy`                | 변경 없음. `OnEnemyDied` 이벤트만 발행            |
| `CubeSystem`           | 카운트 보관 + 기존 웨이브-종료 일괄 드랍 그대로 유지 (가시성만 `internal` 노출) |
| `DroppedCubeSystem`    | **신규**. 픽업 생성/관리/수확/폐기 + Pending 집계 + 이벤트 발행 |
| `DroppedCubePickup`    | **신규**. 씬 픽업 (스프라이트 + 라벨 + 빛기둥 + 부유) |
| `CubeUIDisplay`        | 수확 도착 좌표 제공 + 도착 시 punch (메서드 추가) |
| `PendingDropDisplay`   | **신규**. 수확 대기 카운터 표시 (HUD 상단)        |
| `WaveSystem`           | 변경 없음. 기존 `OnWaveEnded(bool)` 이벤트 재사용 |

### 좌표 변환

- 픽업은 월드 공간 (`SpriteRenderer + TextMesh`).
- HUD 카운터는 `Canvas` (Screen Space) 의 `Text` 컴포넌트.
- 수확 시 `CubeUIDisplay.GetCounterWorldPoint(CubeType)` 가 카운터 `RectTransform` → 카메라 평면 월드 좌표를 반환.
- 픽업이 그 월드 좌표로 호 그리며 이동 후 Destroy.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/CubeSystem.cs`
  - **로직 변경 없음** (기존 웨이브-종료 일괄 드랍 유지)
  - `RollDrop()` 을 `internal` 로 노출해 `DroppedCubeSystem` 이 가중치 재사용
- `MakeDefence/Assets/Scripts/UI/CubeUIDisplay.cs`
  - `public Vector3 GetCounterWorldPoint(CubeType, Camera)` — Canvas 좌표 → 카메라 평면 월드
  - `public void PlayPunch(CubeType)` — 카운터 scale 1 → 1.3 → 1 (0.15s 코루틴)
- `ProjectSettings/TagManager.asset`
  - **신규 Sorting Layer "Pickups"** 추가 (Enemy/Tower 보다 상위 순서)
  - **UnityMCP `manage_editor` 의 `add_sorting_layer` 액션 사용** (직접 YAML 편집 금지)

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/Systems/DroppedCubeSystem.cs` (신규)

```
- public static DroppedCubeSystem Instance
- public static event Action OnPendingChanged
- public IReadOnlyDictionary<CubeType, int> PendingCounts

- [SerializeField] DroppedCubePickup pickupPrefab
- [SerializeField] Grade별 dropChance/dropCount (10개 필드)
- [SerializeField] float collectStaggerSec = 0.05f
- [SerializeField] float collectArcDuration = 0.5f
- [SerializeField] float discardFadeDuration = 0.3f

- 이벤트 구독: Enemy.OnEnemyDied, WaveSystem.OnWaveEnded
- 활성 픽업 리스트 (HashSet<DroppedCubePickup>) + Pending Dictionary

- HandleEnemyDied(Enemy enemy)
   1) **가드**: WaveSystem.Instance == null || !WaveSystem.Instance.IsWaveActive 면 return
      (#2 — 실패 후 잔여 적이 죽을 때 픽업 생성 차단)
   2) Grade → (chance, count) 결정 + Random 롤
   3) count 회: CubeSystem.RollDrop() → SpawnPickup(type, pos)
   4) Pending++ → OnPendingChanged

- HandleWaveEnded(bool cleared)
   - cleared == true → CollectAll() (아래)
   - cleared == false → DiscardAll() (아래)

- CollectAll() — **데이터를 먼저 격리** (#3 — auto-wave 충돌 방지)
   1) 현재 _activePickups / _pendingCounts 를 로컬 변수로 swap, 멤버는 새 빈 컬렉션
   2) 즉시 CubeSystem.Add(type, count) 일괄 호출 (데이터 반영 완료)
   3) OnPendingChanged 발행 (PendingDropDisplay 0 표시)
   4) 로컬 픽업들에 StartCollect 호출 — 시각 효과만 진행, 활성 리스트 미참조
   5) 각 픽업 도착 시 CubeUIDisplay.PlayPunch (시각 효과만, Add 는 이미 끝남)
   6) 다음 웨이브가 즉시 시작되어도 새 픽업은 새 리스트에 누적 → 충돌 없음

- DiscardAll()
   1) 현재 _activePickups / _pendingCounts swap, 멤버는 새 빈 컬렉션
   2) OnPendingChanged 발행
   3) 로컬 픽업들에 StartDiscard 호출 → 페이드 아웃 후 Destroy
   4) CubeSystem.Add 호출 없음

- Register / Unregister Pickup (씬 정리용)
```

### `MakeDefence/Assets/Scripts/Gameplay/DroppedCubePickup.cs` (신규)

```
- CubeType Type
- SpriteRenderer body         (단색 사각형, sortingLayer="Pickups", order=10)
- SpriteRenderer beam         (Additive 위로 길쭉, sortingLayer="Pickups", order=9, 등급별 굵기/알파)
- SpriteRenderer labelBorder  (테두리 박스, sortingLayer="Pickups", order=11, 등급별 색)
- SpriteRenderer labelBg      (어두운 톤 박스, sortingLayer="Pickups", order=12)
- TextMesh       labelText    (큐브 이름, sortingLayer="Pickups", order=13, 등급별 색)

- Initialize(CubeType, Vector2 worldPos)
   → CubeStyleTable.Get(type) 로 색/굵기 룩업
   → body/beam/labelBorder/labelBg/labelText 색 일괄 세팅
- PlaySpawnEffect() — scale 0→1.2→1 + y bounce, 0.25s
- Update() — 부유 (sin wave y bob) + 알파 펄스 (Body+Beam 만, Label 은 가독성 위해 고정)
- StartCollect(Vector3 targetWorldPos, float duration, Action onArrived) — 호 보간 후 콜백
- StartDiscard(float fadeDuration) — alpha 1→0 후 Destroy
- OnDestroy → DroppedCubeSystem.Unregister(this)
```

### `MakeDefence/Assets/Scripts/Gameplay/CubeStyleTable.cs` (신규, 작은 헬퍼)

```
- public static class CubeStyleTable
- public readonly struct CubeStyle {
      Color bodyColor;
      Color beamColor;          // alpha 포함
      float beamWidth;
      Color labelBorderColor;   // 채도 높음 (테두리)
      Color labelBgColor;       // alpha 포함 (어두운 톤)
      Color labelTextColor;     // 밝은 톤
  }
- public static CubeStyle Get(CubeType type) — switch 로 5개 매핑 반환
```

CubeType → 시각 스타일 매핑을 한 곳에서 관리. 향후 인벤/숍 UI 에서도 재사용 가능.

### `MakeDefence/Assets/Scripts/UI/PendingDropDisplay.cs` (신규)

```
- [SerializeField] Text pendingText
  (또는 큐브 타입별 5개 Text 필드)
- OnEnable / OnDisable → DroppedCubeSystem.OnPendingChanged 구독
- Refresh() → PendingCounts 읽어 "Lower×3 Upper×1 ..." 또는
  타입별 카운터 갱신, 0인 타입은 숨김
- 카운트 변화 시 미세 punch (선택)
```

### `MakeDefence/Assets/Prefabs/DroppedCubePickup.prefab` (신규)
- 루트: 빈 GameObject + `DroppedCubePickup`
- 자식 `Beam`:        `SpriteRenderer` (Sprites/Default + Additive 머티리얼, scale x=0.06 y=4.0, pivot bottom, sortingOrder=9)
- 자식 `Body`:        `SpriteRenderer` (Sprites/Default 흰 사각형 + scale 0.4, sortingOrder=10)
- 자식 `LabelBorder`: `SpriteRenderer` (Sprites/Default 흰 사각형 + scale x=0.75 y=0.22, sortingOrder=11)
- 자식 `LabelBg`:     `SpriteRenderer` (Sprites/Default 흰 사각형 + scale x=0.70 y=0.18, sortingOrder=12)
- 자식 `LabelText`:   `TextMesh` (3D Text, Anchor=MiddleCenter, fontSize 24, characterSize 0.05, sortingOrder=13)
- UnityMCP `manage_prefabs` 로 생성
- `LabelBorder` 와 `LabelBg` 의 scale 차이(0.05/0.04) 가 시각적 테두리 두께가 됨

### 큐브 타입(=등급) 별 색 테이블

라벨에 카드 게임 스타일 3단 톤 적용: **테두리(채도↑) > 텍스트(밝음) > 배경(어두움)**.

| Type     | Body      | Beam (a)     | Beam Width | LabelBorder | LabelBg      | LabelText |
|----------|-----------|--------------|------------|--------------|---------------|-----------|
| Lower    | `#A0A0A0` | 회색 0.25    | 0.06       | `#A0A0A0`    | `#3A3A3AE6`   | `#E0E0E0` |
| Upper    | `#4A8BFF` | 파랑 0.35    | 0.07       | `#4A8BFF`    | `#1A3060E6`   | `#7AB3FF` |
| TopTier  | `#FFC93A` | 금색 0.55    | 0.10       | `#FFC93A`    | `#5C4316E6`   | `#FFD86F` |
| Delete   | `#E55050` | 빨강 0.35    | 0.07       | `#E55050`    | `#5A1E1EE6`   | `#FF8585` |
| Clone    | `#B07FFF` | 보라 0.55    | 0.10       | `#B07FFF`    | `#3D2A60E6`   | `#D0A9FF` |

희귀(TopTier/Clone) 일수록 굵고 진한 빛기둥 → 후반 노이즈 속에서도 도드라짐.
테두리·배경·텍스트가 같은 컬러군 + 명도 차이로 한눈에 등급 식별.

### 씬 / HUD 와이어링 (#1 — DroppedCubeSystem 씬 owner 필수)

스크립트/프리팹만 만들면 작동하지 않으므로 아래 단계 반드시 포함:

1. **`DroppedCubeSystem` 컴포넌트 부착**
   - 기존 `CubeSystem` 이 붙은 부트 GameObject (SampleScene 의 `Systems` 류) 에 같이 부착
   - UnityMCP `manage_components` 사용
2. **`pickupPrefab` 참조 연결**
   - 위 컴포넌트의 `pickupPrefab` SerializeField 를 `Assets/Prefabs/DroppedCubePickup.prefab` 으로 설정
   - UnityMCP `manage_components` (component property 설정) 사용
3. **`PendingDropDisplay` 부착**
   - 메인 HUD Canvas 의 `CubeUIDisplay` 가 붙은 GameObject 옆 (혹은 신규 자식) 에 부착
   - 내부 Text 참조도 같이 연결
4. **Sorting Layer "Pickups" 등록 검증**
   - 위 wiring 전에 `ProjectSettings/TagManager.asset` 의 sortingLayers 에 "Pickups" 존재 확인
   - UnityMCP `manage_editor` sorting layer 액션

이 단계가 빠지면 MonoBehaviour 의 `OnEnable` 이 호출되지 않아 `Enemy.OnEnemyDied` 구독이 안 되고 Play 모드에서 픽업이 단 한 개도 만들어지지 않음.

## 4. 테스트 계획

### 수동 검증 (Unity Play)

#### 사망 시 픽업 생성
1. Normal 적 다수 처치 → ~8% 비율로 픽업 등장 + 빛기둥 보임
2. Rare 적 처치 → ~40% 비율
3. Unique 적 처치 → 매번 1개
4. LastBoss 처치 → 매번 3개
5. 베이스 도달 적 → 픽업 없음 (`PlayerSystem` 데미지만)
6. 스폰 이펙트(scale punch + bounce) + 부유/알파 펄스 정상 재생

#### 가시성 (A+B+D)
7. 픽업 위에 적/타워가 지나가도 본체 위에 그려짐 (Sorting Layer)
8. 본체가 일시적으로 가려져도 빛기둥 끝이 화면 상부에 보임
9. HUD `PendingDropDisplay` 가 픽업 생성/회수와 동기화되어 정확히 표시

#### 웨이브 클리어 수확
10. 픽업 다수 + 클리어 → 모두 HUD 카운터로 이동, stagger 적용
11. 도착 순서대로 `CubeSystem.Add` + `PlayPunch`
12. 클리어 후 씬 픽업 0개, `PendingDropDisplay` 0 표시

#### 웨이브 실패 폐기
13. 픽업 다수 + 베이스 파괴 → 모두 페이드 아웃 후 사라짐
14. 큐브 카운트 변화 없음, `PendingDropDisplay` 0 표시

#### 통합 회귀
15. 기존 `CubeSystem.HandleWaveEnded` 일괄 드랍이 클리어 시 정상 작동
16. 씬 재시작 / 게임 종료 시 픽업 잔존 0

#### 엣지 케이스 (Codex 리뷰 반영)
17. **실패 후 잔여 적 처치 (Guard #2)**: 베이스 파괴 후 살아있는 적을 타워가 마저 죽임 → 신규 픽업 생성 안 됨, 다음 시도에 잔존 0
18. **Auto-wave 충돌 (Isolation #3)**: 자동 웨이브 ON 상태에서 다수 픽업 보유 → 클리어 → 즉시 다음 웨이브 시작 → 이전 픽업의 시각 효과가 진행되는 와중에 새 적 처치 → 새 픽업이 **새 리스트/Pending 에만 쌓이고**, 이전 픽업 효과는 자기 트윈 끝까지 진행 후 destroy. PendingDropDisplay 카운트가 새 픽업만 반영
19. **Auto-wave 클리어 데이터 반영**: 클리어 즉시 `CubeSystem` 카운트가 증가 (시각 효과 종료를 기다리지 않음)
20. **씬 owner 누락 회귀 방지 (#1)**: `DroppedCubeSystem` 컴포넌트 미부착 상태로 Play → Editor 로그에 명확한 에러/경고 출력 (예: `Debug.LogError` 가드)

## 5. 위험 요소

- **후반 인플레이션**: 다수 적 동시 사망 → 픽업 다수. Normal 8% 보수적 시작.
- **빛기둥 노이즈**: 다수 픽업의 빛기둥이 동시에 깔리면 시야 방해. 등급별 알파/굵기 차등으로 완화. 그래도 부족하면 상한값 도입(예: 동시 25개 초과 시 가장 오래된 픽업의 빛기둥 알파 감소).
- **라벨 가독성**: 어두운 배경(90% 알파) + 밝은 색 텍스트라 카메라 거리/줌 변화에도 읽힘. `TextMesh` 의 폰트가 작은 카메라에서 흐릿할 수 있어, 필요 시 TMP(`TextMeshPro`) 로 교체 검토. 1차에는 기본 `TextMesh` + 충분한 `characterSize` 로 시작.
- **HUD 좌표 변환**: Canvas Render Mode (Overlay/Camera/World) 에 따라 변환 로직 다름. 메인 HUD Canvas Mode 확인 후 `RectTransformUtility.ScreenPointToWorldPointInRectangle` 또는 `Camera.ScreenToWorldPoint` 분기.
- **씬 전환 시 픽업 누수**: `DontDestroyOnLoad` 미사용 가정. 씬 언로드 시 자동 정리. 단, 같은 씬 재시작 시 `DroppedCubeSystem.Awake` 에서 활성 리스트 초기화 필요.
- **Sorting Layer 추가**: `ProjectSettings/TagManager.asset` 변경은 UnityMCP `manage_editor` 의 sorting layer 액션으로 처리 (메모리 [[feedback_unity_asset_edits]] — 직접 YAML 편집 금지).
- **카운터 punch 애니메이션 겹침**: stagger 로 완화 (`collectStaggerSec` 기본 50ms).
- **`PendingDropDisplay` 위치**: 메인 HUD 의 `CubeUIDisplay` 와 가까운 별도 영역 권장 (수확 흐름이 시각적으로 연결되도록). 정확한 위치는 UnityMCP `manage_ui` 로 배치하며 결정.
- **에셋 부재**: 큐브 타입별 아이콘 스프라이트 없음 → 1차는 단색 사각형 + 라벨 텍스트. 후속 이슈에서 아이콘 교체.
- **테스트 자동화 부재**: 수동 Play 모드 검증에 의존.
- **메모리 (Unity 에셋 편집은 UnityMCP)**: 프리팹/씬/Sorting Layer 변경은 모두 UnityMCP 도구로.
- **씬 owner 누락 (Codex #1)**: 스크립트만으로는 작동하지 않음. SampleScene 의 부트 GameObject 에 `DroppedCubeSystem` + `PendingDropDisplay` 컴포넌트 부착 단계가 필수 (구현 PR 본문에도 명시).
- **실패 후 잔여 처치 (Codex #2)**: `WaveSystem.StopWave` 가 스폰만 멈추므로 `HandleEnemyDied` 첫 줄에 `IsWaveActive` 가드 필수. 누락 시 다음 시도에 픽업 잔존 가능.
- **Auto-wave 데이터 오염 (Codex #3)**: `CollectAll`/`DiscardAll` 진입 시 `_activePickups` / `_pendingCounts` 를 즉시 로컬 swap. 시각 효과는 로컬 픽업에서 self-contained 로 진행, 새 웨이브는 새 멤버 컬렉션 사용 → WaveSystem 미변경으로도 격리 보장.
