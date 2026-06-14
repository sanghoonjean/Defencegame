# Issue #222 — [FEAT] 단축키로 타워 삭제 (D)

> #218 타워 삭제 기능의 후속. 마우스 클릭 대신 키보드 단축키 `D` 로 삭제 확인 팝업을 띄우고, 팝업 내에서 Enter/Esc 로 빠르게 확정/취소한다. 다수 타워 정리 시 UX 개선.
>
> _이슈 본문은 Delete/Backspace 로 명시되어 있으나, 사용자 결정으로 트리거 키를 `D` 단일 키로 변경._

## 1. 시스템 구조

키 입력은 두 단계로 분리한다 — **팝업 트리거**와 **팝업 내 확정/취소**.

```
[게임 화면 / 타워 선택 상태]
   ↓
[TowerDeleteConfirmPopup.Update]  ← 본 작업에서 추가
   ├─ panel.activeSelf == false  (팝업 닫혀 있음)
   │    ├─ D 키 눌림 + 입력 가드 통과
   │    │    └─ InventorySystem.SelectedTower != null
   │    │         └─ ShowForSelectedTower()  ← 기존 API 재사용
   │    └─ (그 외) 무시
   │
   └─ panel.activeSelf == true   (팝업 열려 있음)
        ├─ Enter / KeypadEnter  → OnConfirm()
        └─ Esc                  → Hide()

[입력 가드 — IsTextInputFocused()]
   EventSystem.current.currentSelectedGameObject
     ├─ InputField (UnityEngine.UI) 컴포넌트 보유 → true (무시)
     ├─ TMP_InputField 컴포넌트 보유            → true (무시)
     └─ 그 외 / null                              → false (처리)
```

### 핵심 정책 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| 입력 처리 위치 | **`TowerDeleteConfirmPopup.Update()` 단일 진입점** | 팝업이 항상 씬에 1개 존재(#218). 트리거(Delete/Backspace)와 확정(Enter/Esc)이 모두 "타워 삭제 흐름" 책임이라 같은 컴포넌트에서 응집. 별도 InputManager/GameObject 추가 시 씬 수정 필요 → 본 이슈 범위 초과 |
| `TestRunner` 에 추가 안 함 | `TestRunner.cs`는 주석상 "개발 테스트용 — 빌드 전 삭제" 대상. 영구 기능은 별도 컴포넌트에 둠 |
| 트리거 키 | **`KeyCode.D` 단일** | 사용자 결정 (이슈 본문의 Delete/Backspace 대신). 한 손 조작 / 마우스 위치 유지에 유리. Mac/Windows 플랫폼 차이 없음 |
| Enter 키 | `KeyCode.Return` **+** `KeyCode.KeypadEnter` 둘 다 받음 | 넘버패드 Enter도 자연스럽게 동작해야 함 |
| 즉시 삭제 X | **반드시 팝업 경유** | 이슈 위험 요소 — 오삭제 방지. `D` 키는 팝업을 띄울 뿐 삭제 X |
| 텍스트 입력 가드 | **`EventSystem.current.currentSelectedGameObject` 의 `InputField`/`TMP_InputField` 보유 시 무시** | 이슈 요구사항. 현재 코드베이스에 InputField는 없지만 향후 채팅/이름입력 도입 대비. **`D` 키는 일반 문자 입력 키라 가드 누락 시 입력 중 팝업이 뜸 → 가드 중요도가 Delete 키 대비 더 높음** |
| 팝업 오픈 중 트리거 키 무시 | **`panel.activeSelf == true` 면 `D` 키 무시** | 팝업이 이미 열려 있으면 `D` 키로 새 팝업 트리거하지 않음 (현 팝업 대상이 무엇이었는지 혼동 방지) |
| 키 충돌 검사 | **없음** | `KeyCode.D/Return/KeypadEnter/Escape` grep → 0 hit. 기존 `TestRunner`는 `Space/A/C/R` 만 사용. WASD 카메라 이동 등 미도입 |
| 한 프레임 다중 키 처리 | **첫 매치만 처리** | 한 프레임에 Enter+Esc 동시 입력은 비현실적이나, 분기 순서로 명시 (Esc 우선 → 취소가 안전) |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/TowerDeleteConfirmPopup.cs`
  - `Update()` 메서드 신설 — 팝업 활성 여부에 따라 분기 처리
  - `IsTextInputFocused()` private 헬퍼 — `EventSystem.current.currentSelectedGameObject` 검사
  - `OnConfirm()` 가시성 `private` 유지 (Update에서 직접 호출 가능)
  - `Hide()` 가시성 `private` 유지 (동일)
  - 기존 동작(`Show`, `ShowForSelectedTower`, 버튼 onClick 핸들러) **변경 없음**

수정 파일 외 추가 작업 없음.

## 3. 신규 클래스 / 파일

신규 파일 없음. 입력 처리는 기존 `TowerDeleteConfirmPopup` 내부에 응집.

## 4. 테스트 계획

수동 검증 체크리스트 (Unity Editor Play 모드):

1. **트리거 동작**
   - [ ] 타워 미선택 + `D` → 팝업 안 뜸 (`SelectedTower == null` 가드)
   - [ ] 타워 선택 + `D` → 팝업 표시
   - [ ] 팝업이 이미 열린 상태 + `D` → 무시 (중복 트리거 없음)
2. **팝업 내 키 처리**
   - [ ] 팝업 열림 + `Enter` (메인 키보드) → 타워 삭제 + 팝업 닫힘
   - [ ] 팝업 열림 + `KeypadEnter` (넘버패드) → 타워 삭제 + 팝업 닫힘
   - [ ] 팝업 열림 + `Esc` → 팝업만 닫힘 (삭제 X, `SelectedTower` 유지)
   - [ ] 팝업 열림 + 마우스 Confirm 클릭 → 기존 동작 그대로
3. **입력 가드** (수동, 임시 InputField 추가하여 검증)
   - [ ] Hierarchy에 임시 `InputField` 추가 → 포커스 → `D` 입력 → 팝업 안 뜸, InputField에 "d" 글자 입력됨
   - [ ] `TMP_InputField` 동일 확인 (TextMeshPro 패키지 사용 시)
   - [ ] InputField 포커스 해제 (다른 곳 클릭) → `D` 정상 동작
4. **사이드 이펙트**
   - [ ] 팝업 오픈 중 다른 타워 클릭(`InventorySystem.SelectTower`) → 캡처된 원본 타워 삭제 (#218 캡처 패턴 유지 — 본 작업 무관)
   - [ ] 웨이브 진행 중 `D` → 팝업 정상, 확정 시 적 추적 영향 없음
   - [ ] `EventSystem` 미설정 씬에서 NRE 없음 (`EventSystem.current == null` 가드 — `IsTextInputFocused`에서 처리)
5. **회귀 (#218 기능)**
   - [ ] 마우스로 삭제 버튼 클릭 → 기존대로 정상 동작
   - [ ] 팝업 Cancel 버튼 → 기존대로 정상 동작

## 5. 위험 요소

- **`EventSystem.current` Null 가능성** — 일부 씬에 EventSystem 미배치 시 `IsTextInputFocused()`에서 NRE 위험. `if (EventSystem.current == null) return false;` 가드 필수. 현재 게임플레이 씬은 `TestRunner.HandleClick`에서 `EventSystem.current != null` 체크하므로 EventSystem 존재가 사실상 보장되지만, 방어 코드 유지.
- **TMP_InputField 의존성** — `TMPro` 네임스페이스를 쓰면 패키지 미설치 시 컴파일 에러. **대안**: 컴포넌트 타입을 직접 참조하지 않고 `gameObject.GetComponent("TMP_InputField") != null` 식의 문자열 검사로 우회. 단, GetComponent(string) 오버로드는 deprecated. **현 시점에는 `using TMPro;` + 직접 타입 참조** — `TMP Essentials`/`TextMeshPro` 패키지가 Unity 표준 UI 워크플로우의 일부로 이미 설치되어 있을 가능성이 높음. 확인 후 미설치면 `using TMPro;` 제거하고 UI.InputField만 검사 (현 코드베이스에 둘 다 사용처 없음).
- **`OnConfirm()` 가시성** — 현재 `private`. `Update()`가 같은 클래스 내부라 호출 가능. **외부 노출 안 함** (다른 컴포넌트가 임의 호출하면 캡처된 `_pendingTower` 정합성 깨질 위험).
- **키 입력 우선순위** — 만약 같은 프레임에 Esc + Enter 가 들어오면 Esc 분기를 먼저 두어 **취소 우선**. (이론적 케이스, 실제 사용자가 만들기 어려움)
- **포커스 가드의 보수성** — `InputField`가 포커스됐어도 `currentSelectedGameObject`에 그대로 남아있지 않은 케이스(Tab 키로 포커스 이동 후 등)는 가드 누락 가능. 현재 코드베이스에 InputField가 없으므로 즉시 문제는 없으나, 향후 도입 시 추가 가드(`UnityEngine.UI.InputField.GetComponent<InputField>().isFocused`)를 검토. **`D` 는 일반 문자 키라 가드 미동작 시 텍스트 입력 중 팝업이 뜨는 회귀가 발생** — 향후 텍스트 입력 도입 시 본 가드의 동작을 우선적으로 회귀 테스트.
- **WASD 카메라 이동 도입 시 충돌** — 현재 코드베이스에 카메라 이동 키 미구현. 향후 WASD/방향키 카메라 이동 도입 시 `D` 가 두 기능에 동시 바인딩되어 충돌. 그 시점에 단축키 재설계 필요.
- **`Input.GetKeyDown` 의 Update 의존** — Unity의 `Input` 폴링은 `Update()` 외 (`FixedUpdate`)에서 키 입력 누락 가능. 본 작업은 `Update()` 에 둠 — 표준 패턴.
- **씬 수정 불요** — `TowerDeleteConfirmPopup` 패널/버튼/위치는 #218에서 이미 추가됨. 본 작업은 스크립트 수정만으로 동작.
- **신 입력 시스템 (Input System Package) 미사용** — 본 프로젝트는 `UnityEngine.Input` 레거시 사용 중 (`TestRunner.cs:12,23,35,47`). 본 작업도 동일 API 사용으로 일관성 유지.
