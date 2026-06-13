# Issue #229 — [FEAT] AoE 표현 옵션 (단순 도형 vs 애니메이션)

## 1. 시스템 구조

플레이어가 모든 AoE 표현을 **단순 도형**(GameUIManager 내부 렌더링) 또는 **애니메이션**(`SkillData.aoeFxPrefab` 인스턴스화) 중 하나로 일괄 선택할 수 있도록 전역 옵션을 도입한다. PlayerPrefs로 영속화하고 신규 Settings 패널 UI 로 전환한다.

### 데이터 흐름

```
[게임 시작]
  ↓ SettingsSystem.Init() — PlayerPrefs 에서 AoeDisplayMode 로드
[Settings 패널 진입]
  ↓ Toggle 변경
  ↓ SettingsSystem.SetAoeDisplayMode(mode)
  ├─ PlayerPrefs.SetInt + Save
  └─ OnSettingsChanged 이벤트
[AoE 발동 시]
  ↓ SkillDispatcher.ExecuteFreezingPulse → AoeUtils.ShowAoeHit(..., skill.aoeFxPrefab)
  ↓ GameUIManager.ShowXxxAoeHit(pos, ..., fxPrefab)
  ↓ if SettingsSystem.AoeDisplayMode == SimpleShape → fxPrefab 무시, 도형 렌더
  ↓ else (Animation) → fxPrefab 있으면 spawn, 없으면 도형 fallback
```

### 핵심 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| 저장 위치 | `PlayerPrefs` (int 0/1) | 단일 옵션이라 ScriptableObject 까지 갈 필요 없음. 추후 옵션 증가 시 settings 컨테이너로 마이그레이션 가능 |
| 기본값 | `SimpleShape` (= 0) | 사용자 선택. 가볍게 시작, FX 미설정 스킬과도 일관 |
| 적용 시점 | **즉시** | 다음 AoE 호출부터 바로 반영. 진행 중인 AoE 는 그대로 둠 |
| 분기 위치 | `GameUIManager.ShowXxxAoeHit` 내부 | 호출자(SkillDispatcher, AoeUtils)는 모드를 몰라도 됨. 관심사 분리 |
| 진입 경로 | 게임 화면 우상단 톱니바퀴 버튼 → Settings 패널 | 메인 메뉴 의존 없이 인게임에서 즉시 토글 가능 |
| 확장성 | `SettingsSystem` 을 정적 클래스 + 이벤트로 단순 시작. 옵션 늘어나면 ScriptableObject 또는 MonoBehaviour 싱글톤으로 승격 | YAGNI. 지금은 옵션 1개 |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs`
  - `ShowAoeHit` / `ShowRectAoeHit` / `ShowConeAoeHit` 각각에서 `SettingsSystem.AoeDisplayMode` 체크 후 분기
  - `SimpleShape` 모드면 `fxPrefab` 인자를 무시하고 기존 내부 도형 경로 사용
- `MakeDefence/Assets/Scenes/SampleScene.unity` *(Unity Editor 작업)*
  - Canvas 하위 `SettingsPanel` 추가 (Toggle, Close 버튼)
  - 우상단 톱니바퀴 진입 버튼 추가 → SettingsPanel 활성화

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Systems/SettingsSystem.cs`
  - `public enum AoeDisplayMode { SimpleShape = 0, Animation = 1 }`
  - `public static AoeDisplayMode AoeDisplayMode { get; private set; }`
  - `public static event Action<AoeDisplayMode> OnAoeDisplayModeChanged`
  - `public static void SetAoeDisplayMode(AoeDisplayMode mode)` — PlayerPrefs 저장 + 이벤트
  - `private const string PREF_KEY = "settings.aoeDisplayMode"`
  - `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` 정적 초기화로 PlayerPrefs 로드
- `MakeDefence/Assets/Scripts/UI/SettingsPanelController.cs`
  - `[SerializeField] GameObject panel`
  - `[SerializeField] Toggle aoeAnimationToggle`
  - `[SerializeField] Button openButton`  (우상단 톱니바퀴)
  - `[SerializeField] Button closeButton`
  - Awake: 현재 모드로 toggle.isOn 초기화, listener 등록
  - Toggle.onValueChanged → `SettingsSystem.SetAoeDisplayMode(isOn ? Animation : SimpleShape)`

## 4. 테스트 계획

### 수동 검증
- [ ] 컴파일 OK (`read_console`)
- [ ] 첫 실행 시 기본 모드 = SimpleShape → FreezingPulse 발동 시 도형 표시
- [ ] Settings 패널 열기 → Toggle 켜기 (Animation 모드)
  - 즉시 다음 AoE 부터 `FX_Freezing.prefab` 애니메이션 표시
- [ ] Toggle 끄기 → 다음 AoE 부터 도형 복귀
- [ ] 게임 재시작 → 마지막 설정 유지 (PlayerPrefs 영속화)
- [ ] `aoeFxPrefab` 미설정 스킬: Animation 모드여도 도형 fallback (회귀 없음)

### 회귀 검증
- 기존 Fireball / CausticArrow 등 Circle splash: 변경 없음 (모드 분기는 모든 도형 공통 적용)
- FreezingPulse Circle/Rectangle/Cone: 모드별 정상 동작

## 5. 위험 요소

- **모드 전환 후 진행 중인 AoE** — 이미 spawn 된 prefab/도형은 계속 표시. 정책상 "다음 발동부터 적용". 사용자 혼란 가능하지만 단순 정책이라 수용. 안내 텍스트로 보완.
- **PlayerPrefs 키 충돌** — `settings.aoeDisplayMode` 키는 명시적이라 충돌 가능성 낮음. 추후 키 정리는 별도 이슈.
- **UI 추가 누락** — `.cs` 만으론 옵션 변경 불가. SampleScene 의 SettingsPanel/톱니바퀴 버튼 추가 필수. 미적용 시 기본값(SimpleShape) 로 동작 — fallback 안전.
- **`RuntimeInitializeOnLoadMethod` 정적 초기화 누락** — 첫 호출 전에 init 안되면 PlayerPrefs 미반영. 안전 fallback: getter 에서 lazy-load 추가.
- **모드 확장 시 PlayerPrefs 키 분산** — 옵션 늘면 `settings.audioVolume`, `settings.fps` 등 키가 늘어남. 본 PR 범위 아니지만 추후 settings ScriptableObject 도입 시점 잡기.
- **씬 외부에서 호출** — `GameUIManager._instance` 가 null 이면 기존처럼 조기 return. 모드 체크 로직은 _instance != null 이후 수행.

## 6. 후속 작업 (별도 이슈 후보)

- Rectangle/Cone 전용 FX 프리팹 디자인 (현재 `FX_Freezing.prefab` 은 원형)
- Settings 패널의 다른 옵션(사운드, FPS 표시 등) 확장 → ScriptableObject 기반 설정 시스템 검토
- 모드 전환 즉시 모든 진행 중 AoE 정리 (정책 변경 시)
