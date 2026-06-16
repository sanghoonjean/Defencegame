# Issue #249 — [FEAT] 게임 배속 기능 (1x / 2x / 3x)

> 웨이브 진행/관전 시간 단축. `Time.timeScale` 기반 단일 시스템(`GameSpeedSystem`) 으로 1x → 2x → 3x 순환. HUD 버튼 + `F` 단축키. `R` 리셋 / 게임오버 시 1x 자동 복귀.

## 1. 시스템 구조

```
[GameSpeedSystem]  ← 신규
   ├─ public static GameSpeedSystem Instance
   ├─ public float Current { get; }                  // 현재 배속 (1, 2, 3)
   ├─ public event Action<float> OnSpeedChanged
   ├─ public void Cycle()                             // 1→2→3→1 순환
   ├─ public void Set(float speed)                    // 직접 설정 (상태전환/리셋 등)
   ├─ private void Awake()
   │   ├─ Set(1f)   ← 시작 시 1x
   │   └─ GameStateSystem.OnStateChanged += HandleStateChanged
   ├─ private void OnDestroy() / OnApplicationQuit()
   │   ├─ GameStateSystem.OnStateChanged -= HandleStateChanged
   │   └─ Time.timeScale = 1f  ← 도메인 reload 안전
   └─ private void HandleStateChanged(GameState state)
       └─ if (state != GameState.Playing) Set(1f)
          — Defeat / Victory / WaveResult 진입 시 자동 1x 복귀

[InputManager.Update]  ← 기존 좌클릭 + F 키 추가
   └─ if (Input.GetKeyDown(KeyCode.F))
       └─ GameSpeedSystem.Instance.Cycle()

[GameStateSystem]  ← 수정 없음
   — 기존 OnStateChanged 이벤트로 충분. GameSpeedSystem 이 구독해서
     SetState(Defeat) / SetState(Victory) / SetState(WaveResult) 전이
     모두 자동 1x 복귀.
     ResetToPlaying() → Playing 전이는 Playing 이라 무동작 (이미 1x).

[GameSpeedHudButton]  ← 신규 UI
   ├─ MonoBehaviour, [SerializeField] TMP_Text label  (또는 Text)
   ├─ Button.onClick → GameSpeedSystem.Instance.Cycle()
   └─ OnSpeedChanged 구독 → 라벨 갱신 ("1x" / "2x" / "3x")
```

### 핵심 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| 구현 방식 | **A안 — `Time.timeScale`** | `Time.deltaTime` (Tower 공격타이머, Enemy 이동/스턴, Projectile 이동, CausticGround) + `WaitForSeconds` (WaveSystem 스폰, Enemy DoT 틱) 가 한 번에 가속. 코드 치환 0개. |
| UI 가속 부작용 | **사실상 없음** | `MakeDefence/Assets/Scripts/UI/` 폴더에 `Animator` / `Coroutine` / `CanvasGroup` 일절 사용 X — 인벤/상점/팝업은 단순 SetActive 토글이라 timeScale 영향 X. |
| 시스템 위치 | **`Systems/GameSpeedSystem.cs` 단일 MonoBehaviour** | 다른 시스템(`InventorySystem`/`ShopSystem`/`CubeSystem`) 과 동일 싱글톤 패턴 |
| 입력 진입점 | **`InputManager` 에 `F` 키 추가** | #224 에서 통합된 입력 진입점에 키도 모으는 게 자연스러움. TestRunner 디버그 키와 별개. |
| 리셋 훅 | **`GameStateSystem.OnStateChanged` 구독** | `WaveSystem.HandlePlayerDied()` 는 `SetState(Defeat)` 만 호출하고 `ResetToPlaying()` 을 거치지 않으므로, `ResetToPlaying` 에 hook 을 걸면 게임오버 즉시 1x 복귀가 안 됨 (Codex P2 지적). `OnStateChanged` 구독 → Playing 이외 상태 진입 시 `Set(1f)` 로 처리하면 Defeat / Victory / WaveResult 모두 자동 커버. `GameStateSystem.cs` 코드 변경 불필요. |
| HUD 진입점 | **Canvas 위 Button 1개 + 라벨 1개** | 기존 HUD 가 `GameUIManager` 의 OnGUI(체력바/데미지텍스트)만이라, 정식 Canvas 버튼은 신규 UI 컴포넌트. UnityMCP `manage_gameobject` + `manage_components` 로 씬에 추가 (AGENTS.md §7 완화 후) |
| 배속 단계 | **1 / 2 / 3 고정** | 이슈 명시. 추후 확장 시 `[SerializeField] float[] steps` 로 대체 가능하지만 본 이슈에선 코드 상수. |
| 일시정지 분리 | **본 이슈 외** | 일시정지는 별도 시스템에서 `Time.timeScale = 0` 또는 `IsPaused` 플래그. 본 이슈는 1/2/3 만. |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/InputManager.cs`
  - `Update()` 에 `F` 키 → `GameSpeedSystem.Instance?.Cycle()` 추가
- `MakeDefence/Assets/Scenes/SampleScene.unity` (UnityMCP 경유)
  - `GameSpeedSystem` GameObject 추가 (Systems 컨테이너 또는 루트)
  - Canvas 하위에 `GameSpeedButton` (Button + TMP_Text) 배치, `GameSpeedHudButton` 컴포넌트 부착 및 참조 연결

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Systems/GameSpeedSystem.cs`
  - 싱글톤 + 1x/2x/3x 순환 + `OnSpeedChanged` 이벤트 + `Time.timeScale` 셋
- `MakeDefence/Assets/Scripts/UI/GameSpeedHudButton.cs`
  - Button + Label 컴포넌트, `OnSpeedChanged` 구독, 클릭 시 `Cycle()` 호출

## 4. 테스트 계획

수동 검증 (Unity Editor Play 모드):

1. **순환 동작**
   - [ ] Play 시작 → HUD 라벨 `1x`
   - [ ] 버튼 클릭 → `2x` 라벨 갱신, `Time.timeScale == 2`
   - [ ] 클릭 → `3x`
   - [ ] 클릭 → `1x` 복귀
2. **단축키**
   - [ ] `F` → 동일 순환 (`1x → 2x → 3x → 1x`)
3. **게임 시뮬레이션 가속**
   - [ ] 2x 상태에서 웨이브 시작 → 적 이동 / 타워 공격 쿨다운 / 투사체 / 스폰 인터벌이 비례해 가속
   - [ ] 3x 도 동일
   - [ ] CausticGround 등 지속 효과의 lifeTimer / tickTimer 비례 가속
4. **UI 영향 없음**
   - [ ] 2x/3x 상태에서 인벤/상점/팝업 토글 즉시 반응 (지연/가속 없음)
   - [ ] `OnGUI` 데미지 텍스트는 게임 시뮬레이션에 묶이므로 비례 가속되는 게 정상 (확인만)
5. **상태 전환 시 복귀**
   - [ ] 2x/3x 상태에서 `R` 키 → `ResetToPlaying` → Playing 전이 (이미 1x 라 무동작), 라벨 그대로
   - [ ] 2x/3x 상태에서 **플레이어 사망(Defeat 진입)** → 즉시 1x 복귀 + 라벨 갱신 (Codex P2 시나리오)
   - [ ] 2x/3x 상태에서 **웨이브 클리어(WaveResult 진입)** → 즉시 1x 복귀
   - [ ] 1x 복귀 후 다시 클릭/F 키 → 2x → 3x 정상 순환
6. **회귀 X**
   - [ ] InputManager 좌클릭 (#224) 정상
   - [ ] TestRunner Space/A/C/R 정상
   - [ ] D 키 삭제 팝업, 매도/언락 모달 정상

## 5. 위험 요소

- **씬 변경 포함** — `SampleScene.unity` 에 `GameSpeedSystem` GameObject + Canvas 하위 버튼 추가. **AGENTS.md §7 정책 완화 (PR #250) 머지 후 UnityMCP 로 작업.** #250 미머지 상태면 사용자에게 위임.
- **`Time.timeScale` 글로벌 부작용**
  - 도메인 reload / 에디터 정지 시 `timeScale` 이 남아 있을 수 있음 → `OnDestroy` / `OnApplicationQuit` 에서 1f 복귀
  - `WaitForSecondsRealtime` / `Time.unscaledDeltaTime` 사용처는 없음 (Grep 확인 완료)
- **3x 에서 물리/충돌 안정성** — Unity 2D 물리는 `Time.fixedDeltaTime * timeScale` 로 자동 스케일. 다만 매우 빠른 투사체가 한 프레임에 큰 거리를 이동하면 OverlapPoint/CircleCast miss 가능 — 본 게임 투사체 속도가 낮아 3x 도 안전 추정. 이상 발생 시 `Time.fixedDeltaTime` 도 비례 축소 검토.
- **OnGUI 데미지 텍스트 가속** — `Time.time` 기반 expireTime 이라 배속에 따라 빨라짐. 시뮬레이션 일부로 보고 의도된 동작으로 처리. UI 와 분리하려면 별도 처리 필요하지만 본 이슈 외.
- **UI Button 의존성** — Canvas/EventSystem 이 씬에 이미 있어야 함 (#224 작업으로 EventSystem 존재 확인됨)
- **카메라/Update 순서** — `Time.timeScale` 은 모든 `Update` / `FixedUpdate` 에 적용되므로 `InputManager.Update` 의 `F` 키 자체도 가속 영향 받지만, `GetKeyDown` 은 프레임 기반이라 동작 변화 없음
- **자동 테스트 한계** — `Time.timeScale` 외부 검증은 어려움. 컴파일 + Play 모드 진입 + 콘솔 에러 0 까지 자동, 시각적 가속 / UI 라벨 변화는 사용자 수동.
