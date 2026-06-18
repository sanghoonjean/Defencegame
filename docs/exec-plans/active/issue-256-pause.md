# Issue #256 — [FEAT] 일시 정지 기능

> 게임 시뮬레이션 전체를 잠시 멈춘다. `Time.timeScale = 0` 기반 단일 시스템(`PauseSystem`) 으로 재생 ↔ 일시정지 토글. HUD 버튼 + `P` 단축키. 진입 시 현재 배속 기억 → 해제 시 복원. 상태 전이(게임오버/리셋/웨이브 클리어) 시 자동 해제.

## 1. 시스템 구조

```
[PauseSystem]  ← 신규
   ├─ public static PauseSystem Instance
   ├─ public bool IsPaused { get; }
   ├─ public event Action<bool> OnPauseChanged
   ├─ public void Toggle()
   ├─ public void Set(bool paused)
   │   ├─ paused == true  → _resumeSpeed = GameSpeedSystem.Current; Time.timeScale = 0
   │   └─ paused == false → Time.timeScale = _resumeSpeed (없으면 1f)
   │       — GameSpeedSystem.Set(_resumeSpeed) 도 같이 호출해 배속 시스템과 동기화
   ├─ private float _resumeSpeed = 1f
   ├─ private void Awake()
   │   ├─ Instance = this; IsPaused = false
   │   └─ GameStateSystem.OnStateChanged += HandleStateChanged
   ├─ private void OnDestroy() / OnApplicationQuit()
   │   ├─ GameStateSystem.OnStateChanged -= HandleStateChanged
   │   └─ Time.timeScale = 1f  ← 도메인 reload / 에디터 정지 안전
   └─ private void HandleStateChanged(GameState state)
       └─ if (IsPaused) Set(false)
          — 게임오버(Defeat) / 웨이브 클리어(WaveResult) / 리셋(Playing 재진입) 모두 자동 해제
          — 해제 시 _resumeSpeed 로 복원하지만 GameSpeedSystem 도 같은 이벤트로 Set(1f) 하므로
            결과적으로 1x 로 정착 (양쪽 호출 순서와 무관하게 마지막에 1x)

[InputManager.Update]  ← 기존 F 키 옆에 P 추가
   └─ if (Input.GetKeyDown(KeyCode.P))
       └─ PauseSystem.Instance?.Toggle()

[GameSpeedSystem]  ← 수정 없음
   — Pause 진입 시 PauseSystem 이 Current 를 읽어 _resumeSpeed 에 보관.
   — Pause 해제 시 PauseSystem.Set(false) 가 GameSpeedSystem.Set(_resumeSpeed) 호출 →
     OnSpeedChanged 가 자동 발행되어 배속 HUD 라벨도 동기화.

[GameStateSystem]  ← 수정 없음
   — 기존 OnStateChanged 이벤트로 충분. PauseSystem 이 구독해서 모든 상태 전이에서 자동 해제.

[PauseHudButton]  ← 신규 UI
   ├─ MonoBehaviour, [SerializeField] TMP_Text label
   ├─ Button.onClick → PauseSystem.Instance.Toggle()
   └─ OnPauseChanged 구독 → 라벨 갱신 ("⏸" / "▶" — 또는 텍스트 "Pause" / "Resume")
```

### 핵심 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| 구현 방식 | **A안 — `Time.timeScale = 0`** | #249 와 동일 메커니즘. `WaitForSecondsRealtime` / `Time.unscaledDeltaTime` 사용처 없음(#249 플랜에서 검증) → 시뮬레이션 완전 정지. UI Button/EventSystem 은 `timeScale` 영향 없어 해제 입력 정상. |
| 배속과 분리 | **별도 `PauseSystem`** | `GameSpeedSystem` 에 통합하면 책임 혼탁(배속 토글 + 정지 토글). 진입/해제 시 `_resumeSpeed` 만 한 번 교환하면 되므로 결합도 낮음. |
| 단축키 | **`P`** | `Space` 는 `TestRunner.cs:9` 에서 "웨이브 시작" 디버그 키로 이미 점유 — 충돌 회피 위해 `P` 선택. TestRunner 는 "빌드 전 삭제" 임시 도구이지만 현재 활성, 본 이슈 범위 밖이라 손대지 않음. `P` = Pause 의 일반 관례. 좌클릭/`F` 와 충돌 없음. |
| 배속 복원 | **진입 시 `Current` 저장, 해제 시 `Set` 으로 복원** | 사용자가 3x 로 빠르게 진행하다 잠깐 멈췄을 때 다시 3x 로 돌아오는 게 자연스러움. 단, 상태 전이로 자동 해제될 땐 `GameSpeedSystem` 이 1x 로 같이 강제하므로 의도와 일치. |
| 상태 전이 시 해제 훅 | **`GameStateSystem.OnStateChanged` 구독, `if (IsPaused) Set(false)`** | (1) 게임오버 화면에서 멈춰있는 채로 남으면 R 리셋 입력은 받지만 혼란. (2) 리셋(R) 후 일시정지 잔여 상태 방지. 분기 없이 모든 상태 전이에서 무조건 해제하는 게 단순/안전. |
| HUD 진입점 | **Canvas 위 Button 1개 + 라벨 1개** | #249 배속 버튼과 동일 패턴. Canvas 하위에 옆에 나란히 배치. |
| 일시정지 중 상호작용 | **인벤/상점/매도/배치 모두 가능** | `timeScale = 0` 이어도 UI Button onClick 및 좌클릭 배치(`InputManager`) 는 `Update` 기반으로 동작. 의도된 동작 — 일시정지 중 정비/배치 검토가 본 기능의 목적 중 하나. |
| 자동 테스트 | **수동만** | `Time.timeScale` 외부 검증 어려움. 컴파일 + Play 모드 진입 + 콘솔 에러 0 까지 자동, 정지/복원 동작은 사용자 수동. |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/InputManager.cs`
  - `Update()` 에 `P` 키 → `PauseSystem.Instance?.Toggle()` 추가 (기존 `F` 키 블록 옆)
- `MakeDefence/Assets/Scenes/SampleScene.unity` (UnityMCP 경유)
  - `PauseSystem` GameObject 추가 (Systems 컨테이너)
  - Canvas 하위에 `PauseButton` (Button + TMP_Text) 배치, `PauseHudButton` 컴포넌트 부착 및 참조 연결

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Systems/PauseSystem.cs`
  - 싱글톤 + `IsPaused` + `Toggle()` / `Set(bool)` + `OnPauseChanged` 이벤트 + `Time.timeScale` 셋
  - 진입 시 `GameSpeedSystem.Current` 저장, 해제 시 `GameSpeedSystem.Set(_resumeSpeed)` 호출
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
5. **배속 복원**
   - [ ] 1x 에서 일시정지 → 해제 → 1x
   - [ ] `F` 로 2x 설정 → `P` 일시정지 → `P` 해제 → 2x 복원 + 배속 라벨 `2x`
   - [ ] 3x → 일시정지 → 해제 → 3x 복원
6. **상태 전이 시 자동 해제**
   - [ ] 일시정지 중 `R` 리셋 → 자동 해제 + 1x (`GameSpeedSystem` 1x 복귀와 합쳐져)
   - [ ] (이론상) 일시정지 중 게임오버/웨이브 클리어 도달 — 시뮬레이션이 멈춰 있어 직접 트리거 어려우나, `TestRunner` 등으로 강제 호출 시 자동 해제 확인
7. **회귀 X**
   - [ ] `F` 배속 순환 (#249) 정상
   - [ ] 좌클릭 배치 / 타워 선택 (#224) 정상
   - [ ] TestRunner Space/A/C/R 정상 (`Space` 충돌은 `P` 선택으로 회피)
   - [ ] D 키 삭제 팝업, 매도/언락 모달 정상

## 5. 위험 요소

- **씬 변경 포함** — `SampleScene.unity` 에 `PauseSystem` GameObject + Canvas 하위 버튼 추가. AGENTS.md §7 정책상 UnityMCP 경유 필수.
- **`TestRunner` 의 `Space` 키 충돌 확인됨** — `TestRunner.cs:9` 가 `Space` 를 "웨이브 시작" 디버그 키로 점유 중. 본 플랜은 **Pause 단축키를 `P` 로 선택해 회피**. TestRunner 는 "빌드 전 삭제" 임시 도구로 명시되어 있으나 현재 활성, 본 이슈 범위 밖이라 손대지 않음.
- **`Time.timeScale = 0` 글로벌 부작용**
  - 도메인 reload / 에디터 정지 시 `timeScale = 0` 이 남으면 다음 Play 시 멈춘 상태로 시작 → `OnDestroy` / `OnApplicationQuit` 에서 1f 복귀
  - `WaitForSecondsRealtime` / `Time.unscaledDeltaTime` 사용처 없음(#249 플랜 검증 결과) → 멈춰야 할 게 안 멈추는 케이스 없음
- **`GameSpeedSystem` 과의 순서 의존** — 일시정지 진입 시 `GameSpeedSystem.Current` 를 읽으므로 `GameSpeedSystem` 이 먼저 `Awake` 되어야 함. Unity Execution Order 기본 순서로 충분하나, 안전을 위해 `Current` null/0 가드 (기본값 1f 폴백) 처리.
- **상태 전이 자동 해제 시 이중 호출** — `OnStateChanged` 가 발생하면 `PauseSystem` 이 `GameSpeedSystem.Set(1f)` 호출(복원), `GameSpeedSystem` 자체도 `Set(1f)` 호출 → 둘 다 같은 값이라 멱등. `OnSpeedChanged` 가 두 번 발행될 수 있으나 UI 라벨 갱신만 두 번이라 사이드 이펙트 없음.
- **UI Button 의존성** — Canvas/EventSystem 이 씬에 이미 있어야 함 (#224 작업으로 확인)
- **자동 테스트 한계** — `Time.timeScale` 외부 검증 어려움. 컴파일 + Play 모드 진입 + 콘솔 에러 0 까지 자동, 정지/복원 동작은 사용자 수동.
