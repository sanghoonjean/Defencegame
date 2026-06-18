# Issue #256 — [FEAT] 일시 정지 기능

> 게임 시뮬레이션 전체를 잠시 멈춘다. `Time.timeScale` 의 소유권을 **"일시정지 아님 → `GameSpeedSystem.Current`, 일시정지 → 0"** 으로 통합. HUD 버튼 + `P` 단축키. `GameSpeedSystem.Set` 에 한 줄 가드를 추가해 일시정지 중 배속 변경이 게임을 깨우지 않게 한다. **활성 웨이브 중에만 진입 허용** (비활성 시 토글 차단 — 스폰 코루틴 stall 방지). 상태 전이(게임오버/리셋/웨이브 클리어) 시 자동 해제.

## 1. 시스템 구조

```
[PauseSystem]  ← 신규
   ├─ public static PauseSystem Instance
   ├─ public bool IsPaused { get; }
   ├─ public event Action<bool> OnPauseChanged
   ├─ public void Toggle() => Set(!IsPaused)
   ├─ public void Set(bool paused)
   │   ├─ paused == true  →
   │   │     if (WaveSystem.Instance == null || !WaveSystem.Instance.IsWaveActive) return;
   │   │     IsPaused = true;  Time.timeScale = 0
   │   └─ paused == false → IsPaused = false; Time.timeScale = GameSpeedSystem.Current
   │       — 양쪽 끝에서 OnPauseChanged?.Invoke(paused)
   │       — 진입 게이트는 paused=true 에만 적용. 해제(paused=false) 는 게이트 없음 →
   │         HandleStateChanged 의 자동 해제는 어떤 상태에서든 동작.
   ├─ private void Awake()
   │   ├─ Instance = this; IsPaused = false
   │   └─ GameStateSystem.OnStateChanged += HandleStateChanged
   ├─ private void OnDestroy() / OnApplicationQuit()
   │   ├─ GameStateSystem.OnStateChanged -= HandleStateChanged
   │   └─ Time.timeScale = 1f  ← 도메인 reload / 에디터 정지 안전
   └─ private void HandleStateChanged(GameState state)
       └─ if (IsPaused) Set(false)
          — 게임오버 / 웨이브 클리어 / 리셋(Playing 재진입) 모두 자동 해제
          — Set(false) 가 Time.timeScale = GameSpeedSystem.Current 를 쓰지만,
            같은 이벤트의 GameSpeedSystem.HandleStateChanged 가 Current 를 1f 로
            되돌리므로 호출 순서와 무관하게 최종 timeScale = 1 보장 (아래 GameSpeedSystem 항)

[InputManager.Update]  ← 기존 F 키 옆에 P 추가
   └─ if (Input.GetKeyDown(KeyCode.P))
       └─ PauseSystem.Instance?.Toggle()

[GameSpeedSystem]  ← Set() 에 일시정지 가드 추가 (한 줄)
   public void Set(float speed)
   {
       Current = speed;
       if (PauseSystem.Instance == null || !PauseSystem.Instance.IsPaused)
           Time.timeScale = speed;        // 일시정지 중엔 timeScale 미터치
       OnSpeedChanged?.Invoke(speed);     // 일시정지 중에도 발행 → HUD 배속 라벨 갱신
   }
   — 기존 HandleStateChanged (모든 상태 전이에서 Set(1f)) 는 그대로 유지.
     일시정지 중 상태 전이가 일어나면: Current=1 만 갱신, timeScale 은 0 유지 →
     이후 PauseSystem.Set(false) 가 timeScale = Current = 1 로 정착시킴.

[GameStateSystem]  ← 수정 없음
   — 기존 OnStateChanged 이벤트로 충분. PauseSystem / GameSpeedSystem 둘 다 구독.

[PauseHudButton]  ← 신규 UI
   ├─ MonoBehaviour, [SerializeField] TMP_Text label
   ├─ Button.onClick → PauseSystem.Instance.Toggle()
   └─ OnPauseChanged 구독 → 라벨 갱신 ("⏸" / "▶" — 또는 텍스트 "Pause" / "Resume")
```

### 상태 전이 시 timeScale 순서 검증 (구독 순서 무관 보장)

전제: `GameSpeedSystem.Awake` 가 먼저 → `GameSpeedSystem.HandleStateChanged` 가 먼저 구독. 시나리오: **3x → P 일시정지 → R 리셋**.

```
[리셋 진입 시점 상태]
  IsPaused = true, Current = 3, Time.timeScale = 0

[Case A — GameSpeedSystem 먼저 발화]
  1) GameSpeedSystem.HandleStateChanged → Set(1f)
     - Current = 1
     - IsPaused == true 이므로 Time.timeScale 미터치 (= 0 유지)
     - OnSpeedChanged(1) 발행 → HUD 배속 라벨 "1x"
  2) PauseSystem.HandleStateChanged → if (IsPaused) Set(false)
     - IsPaused = false
     - Time.timeScale = GameSpeedSystem.Current = 1   ✅
     - OnPauseChanged(false) 발행 → HUD 일시정지 라벨 "▶"

[Case B — PauseSystem 먼저 발화]
  1) PauseSystem.HandleStateChanged → if (IsPaused) Set(false)
     - IsPaused = false
     - Time.timeScale = GameSpeedSystem.Current = 3   (아직 GameSpeed 미발화)
     - OnPauseChanged(false)
  2) GameSpeedSystem.HandleStateChanged → Set(1f)
     - Current = 1
     - IsPaused == false 이므로 Time.timeScale = 1   ✅
     - OnSpeedChanged(1)

→ 양 경우 모두 같은 프레임 내 최종 timeScale = 1. Case B 의 중간 3 은 같은 프레임의
  Update/FixedUpdate 사이에 노출되지 않음 (이벤트는 동기적으로 모두 발화).
```

### 일시정지 중 배속 변경 시나리오

```
1x 재생 중 → P 일시정지:
  IsPaused=true, Current=1, timeScale=0, 일시정지 라벨 "⏸", 배속 라벨 "1x"

  F 키 (배속 순환):
    GameSpeedSystem.Cycle() → Set(2)
    - Current = 2
    - IsPaused == true 이므로 timeScale 미터치 (= 0 유지)
    - OnSpeedChanged(2) → 배속 라벨 "2x"
  → 시뮬레이션은 여전히 정지, 라벨만 "⏸ / 2x" — 상태 일관 ✅

  P 해제:
    PauseSystem.Set(false)
    - IsPaused = false
    - Time.timeScale = GameSpeedSystem.Current = 2
    - OnPauseChanged(false) → "▶"
  → 사용자가 일시정지 중 선택한 2x 로 즉시 재개 ✅
```

### 핵심 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| 구현 방식 | **A안 — `Time.timeScale = 0`** | #249 와 동일 메커니즘. `WaitForSecondsRealtime` / `Time.unscaledDeltaTime` 사용처 없음(#249 플랜에서 검증) → 시뮬레이션 완전 정지. UI Button/EventSystem 은 `timeScale` 영향 없어 해제 입력 정상. |
| 배속과 분리 | **별도 `PauseSystem` (배속/정지는 직교 축)** | 시뮬레이션 속도와 시간 흐름 정지는 다른 축. 단, `timeScale` 의 소유권은 `PauseSystem` 으로 통합 (`GameSpeedSystem.Set` 은 일시정지 가드 한 줄로 보조). |
| `Time.timeScale` 소유권 | **단일 진실: `IsPaused ? 0 : GameSpeedSystem.Current`** | 두 시스템이 자유롭게 `timeScale` 을 쓰면 일관성이 깨짐. `PauseSystem` 만 `timeScale` 에 0 또는 `Current` 를 쓰고, `GameSpeedSystem.Set` 은 일시정지 중일 때 `Current` 만 갱신하고 `timeScale` 미터치. |
| 단축키 | **`P`** | `Space` 는 `TestRunner.cs:9` 에서 "웨이브 시작" 디버그 키로 이미 점유 — 충돌 회피 위해 `P` 선택. TestRunner 는 "빌드 전 삭제" 임시 도구이지만 현재 활성, 본 이슈 범위 밖이라 손대지 않음. `P` = Pause 의 일반 관례. 좌클릭/`F` 와 충돌 없음. |
| 배속 복원 방식 | **`_resumeSpeed` 필드 제거. `GameSpeedSystem.Current` 가 단일 진실** | 일시정지 중 `F` / 배속 버튼이 `Current` 만 갱신 (timeScale 가드) → 해제 시 `timeScale = Current` 면 자연스럽게 사용자가 마지막으로 선택한 배속으로 재개. 별도 스냅샷 불필요. |
| 일시정지 중 배속 입력 처리 | **무시 X, `Current`/HUD 라벨만 갱신** | **Codex P2 (PR #257, acfd7817)**: 일시정지 중 `F` 가 `Time.timeScale` 을 덮어쓰면 `IsPaused=true` 인데 시뮬레이션이 재개되어 상태 모순. 본 플랜은 `GameSpeedSystem.Set` 에 가드 추가 — `Current` 와 `OnSpeedChanged` 는 갱신해 HUD 배속 라벨이 일시정지 중에도 반응(미리보기), `timeScale` 만 미터치. |
| 일시정지 진입 게이트 | **`WaveSystem.IsWaveActive == true` 일 때만 허용** | **Codex P2 (PR #257, 810f510b)**: `WaveSystem.StartWave()` 가 `SetState` 를 호출하지 않으므로 `OnStateChanged` 가 발화되지 않음. 또 `SpawnEnemies()` 는 scaled `WaitForSeconds` 사용 ([WaveSystem.cs:133](MakeDefence/Assets/Scripts/Systems/WaveSystem.cs:133)) — WaveResult/Defeat 화면에서 일시정지 후 디버그 `Space` 로 StartWave 호출 시 `timeScale=0` 으로 스폰 코루틴 영구 stall. 비활성 시점엔 시뮬레이션이 멈출 것도 없으므로 의미가 없고 위험만 — 진입을 차단. 해제는 항상 허용. |
| 상태 전이 시 해제 훅 | **`HandleStateChanged → if (IsPaused) Set(false)`** | `Set(false)` 가 `timeScale = Current` 를 쓰지만, 같은 이벤트의 `GameSpeedSystem.HandleStateChanged` 가 `Current` 를 1f 로 되돌리므로 호출 순서와 무관하게 최종 `timeScale = 1` (위 "상태 전이 시 timeScale 순서 검증" 참조). 별도 분리 분기 불필요. |
| HUD 진입점 | **Canvas 위 Button 1개 + 라벨 1개** | #249 배속 버튼과 동일 패턴. Canvas 하위에 옆에 나란히 배치. |
| 일시정지 중 상호작용 | **인벤/상점/매도/배치 모두 가능** | `timeScale = 0` 이어도 UI Button onClick 및 좌클릭 배치(`InputManager`) 는 `Update` 기반으로 동작. 의도된 동작 — 일시정지 중 정비/배치 검토가 본 기능의 목적 중 하나. |
| 자동 테스트 | **수동만** | `Time.timeScale` 외부 검증 어려움. 컴파일 + Play 모드 진입 + 콘솔 에러 0 까지 자동, 정지/복원 동작은 사용자 수동. |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/InputManager.cs`
  - `Update()` 에 `P` 키 → `PauseSystem.Instance?.Toggle()` 추가 (기존 `F` 키 블록 옆)
- `MakeDefence/Assets/Scripts/Systems/GameSpeedSystem.cs`
  - `Set(float)` 본문에 일시정지 가드 한 줄 추가:
    `if (PauseSystem.Instance == null || !PauseSystem.Instance.IsPaused) Time.timeScale = speed;`
  - 기존 `Cycle()` / `HandleStateChanged` / `OnSpeedChanged` 로직 변경 없음.
- `MakeDefence/Assets/Scenes/SampleScene.unity` (UnityMCP 경유)
  - `PauseSystem` GameObject 추가 (Systems 컨테이너)
  - Canvas 하위에 `PauseButton` (Button + TMP_Text) 배치, `PauseHudButton` 컴포넌트 부착 및 참조 연결

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Systems/PauseSystem.cs`
  - 싱글톤 + `IsPaused` + `Toggle()` / `Set(bool)` + `OnPauseChanged` 이벤트
  - `Set(true)`: `WaveSystem.IsWaveActive == false 면 early return`; 통과 시 `IsPaused=true; Time.timeScale=0; OnPauseChanged(true)`
  - `Set(false)`: `IsPaused=false; Time.timeScale = GameSpeedSystem.Current ?? 1f; OnPauseChanged(false)` — 게이트 없음
  - `HandleStateChanged`: `if (IsPaused) Set(false)` — 상태 전이 자동 해제
  - `OnDestroy` / `OnApplicationQuit` 에서 `Time.timeScale = 1f` 안전 복원
- `MakeDefence/Assets/Scripts/UI/PauseHudButton.cs`
  - Button + Label 컴포넌트, `OnPauseChanged` 구독, 클릭 시 `Toggle()` 호출

## 4. 테스트 계획

수동 검증 (Unity Editor Play 모드):

1. **토글 동작**
   - [ ] Play 시작 → HUD 라벨 `▶` (재생 중)
   - [ ] 버튼 클릭 → `⏸` 라벨, `Time.timeScale == 0`
   - [ ] 클릭 → `▶` 복귀, `Time.timeScale == 1` (또는 직전 배속)
2. **단축키**
   - [ ] `P` → 동일 토글
3. **시뮬레이션 정지**
   - [ ] 일시정지 중 적 이동 / 타워 공격 / 투사체 / 웨이브 스폰 / CausticGround 모두 완전 정지
   - [ ] 해제 시 모두 즉시 재개
4. **UI 상호작용 정상**
   - [ ] 일시정지 중 인벤 열고/닫기 정상
   - [ ] 일시정지 중 타워 배치/매도/언락 모달 정상
   - [ ] 일시정지 중 상점 사용 정상
5. **배속 복원 (사용자 토글)**
   - [ ] 1x 에서 일시정지 → 해제 → 1x
   - [ ] `F` 로 2x 설정 → `P` 일시정지 → `P` 해제 → 2x 복원 + 배속 라벨 `2x`
   - [ ] 3x → 일시정지 → 해제 → 3x 복원
6. **일시정지 중 배속 변경 (Codex P2 #2 검증)**
   - [ ] 1x → `P` 일시정지 → `F` (2x) → **시뮬레이션 여전히 정지** + 일시정지 라벨 `⏸` + 배속 라벨 `2x` (모두 일관)
   - [ ] 위 상태에서 `F` 한 번 더 (3x) → 동일하게 정지 유지 + 배속 라벨 `3x`
   - [ ] 위 상태에서 `P` 해제 → 3x 로 즉시 재개 (사용자가 일시정지 중 선택한 배속이 적용)
   - [ ] 일시정지 중 배속 HUD 버튼 클릭도 동일 동작
7. **상태 전이 시 자동 해제 (Codex P2 #1 검증)**
   - [ ] **3x → `P` 일시정지 → `R` 리셋 → 1x 재개** (호출 순서와 무관하게 최종 timeScale=1)
   - [ ] 1x → `P` 일시정지 → `R` 리셋 → 1x
   - [ ] (이론상) 일시정지 중 게임오버/웨이브 클리어 도달 — 시뮬레이션이 멈춰 있어 직접 트리거 어려우나, `TestRunner` 등으로 강제 호출 시 자동 해제 + 1x 확인
8. **진입 게이트 (Codex P2 #3 검증)**
   - [ ] 초기 화면 (state=Playing, IsWaveActive=false) 에서 `P` / 버튼 → **무반응** (라벨 ▶ 유지)
   - [ ] Space 로 웨이브 시작 → IsWaveActive=true → `P` → 정상 일시정지
   - [ ] 일시정지 중 R 리셋 → 자동 해제 → IsWaveActive=false → `P` 다시 무반응 (회귀 게이트)
   - [ ] WaveResult 화면(웨이브 클리어 후) 에서 `P` → 무반응
   - [ ] Defeat 화면(플레이어 사망) 에서 `P` → 무반응
9. **회귀 X**
   - [ ] `F` 배속 순환 (#249) — 평시(일시정지 아님) 동작 변화 없음
   - [ ] 좌클릭 배치 / 타워 선택 (#224) 정상
   - [ ] TestRunner Space/A/C/R 정상 (`Space` 충돌은 `P` 선택으로 회피)
   - [ ] D 키 삭제 팝업, 매도/언락 모달 정상

## 5. 위험 요소

- **씬 변경 포함** — `SampleScene.unity` 에 `PauseSystem` GameObject + Canvas 하위 버튼 추가. AGENTS.md §7 정책상 UnityMCP 경유 필수.
- **`TestRunner` 의 `Space` 키 충돌 확인됨** — `TestRunner.cs:9` 가 `Space` 를 "웨이브 시작" 디버그 키로 점유 중. 본 플랜은 **Pause 단축키를 `P` 로 선택해 회피**. TestRunner 는 "빌드 전 삭제" 임시 도구로 명시되어 있으나 현재 활성, 본 이슈 범위 밖이라 손대지 않음.
- **`Time.timeScale = 0` 글로벌 부작용**
  - 도메인 reload / 에디터 정지 시 `timeScale = 0` 이 남으면 다음 Play 시 멈춘 상태로 시작 → `OnDestroy` / `OnApplicationQuit` 에서 1f 복귀
  - `WaitForSecondsRealtime` / `Time.unscaledDeltaTime` 사용처 없음(#249 플랜 검증 결과) → 멈춰야 할 게 안 멈추는 케이스 없음
- **비활성 시점 일시정지 시 스폰 코루틴 stall** — `WaveSystem.StartWave()` 는 `SetState` 미호출이라 `OnStateChanged` 가 발화되지 않음. `SpawnEnemies` 는 scaled `WaitForSeconds` 사용 ([WaveSystem.cs:133](MakeDefence/Assets/Scripts/Systems/WaveSystem.cs:133)) → WaveResult/Defeat/사전 셋업에서 일시정지 후 디버그 Space 로 StartWave 호출 시 스폰이 영구 멈춤 (Codex P2 #3, PR #257). 본 플랜은 `Set(true)` 에 `IsWaveActive` 게이트로 진입 자체를 차단해 회피.
- **`GameSpeedSystem.Set` 가드 누락 시 회귀** — `GameSpeedSystem.Set` 의 일시정지 가드 한 줄(`if (Pause...) timeScale=...`) 이 빠지면 일시정지 중 `F` 가 시뮬레이션을 깨움 (Codex P2 #2, PR #257). 본 플랜의 핵심 수정이므로 코드 리뷰 / 테스트 시 6번 체크리스트 필수.
- **상태 전이 자동 해제 시 구독 순서 무관** — `PauseSystem` 과 `GameSpeedSystem` 둘 다 `GameStateSystem.OnStateChanged` 를 구독. `GameSpeedSystem.Set` 이 일시정지 가드를 갖기 때문에:
  - GameSpeedSystem 먼저 발화 → `Current=1, timeScale 미터치(0 유지)` → PauseSystem 발화 → `timeScale = Current = 1` ✅
  - PauseSystem 먼저 발화 → `timeScale = Current = 3 (아직 옛 값)` → GameSpeedSystem 발화 → `Current=1`, 이제 `IsPaused=false` 라 `timeScale = 1` ✅
  - 두 경우 모두 같은 프레임 내 최종 `timeScale = 1` — 초기 Codex P2 #1 지적 해소.
- **순서 의존 (진입)** — 일시정지 진입 시 `GameSpeedSystem.Current` 는 평시에 갱신된 상태(`Awake` 에서 `Set(1f)`) 이므로 `null/0` 케이스는 `Set(false)` 에서 `?? 1f` 폴백으로 방어.
- **UI Button 의존성** — Canvas/EventSystem 이 씬에 이미 있어야 함 (#224 작업으로 확인)
- **자동 테스트 한계** — `Time.timeScale` 외부 검증 어려움. 컴파일 + Play 모드 진입 + 콘솔 에러 0 까지 자동, 정지/복원 동작은 사용자 수동.
