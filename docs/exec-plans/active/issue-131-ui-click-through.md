# Issue #131 — UI 버튼 클릭 시 월드 클릭 이벤트 통과 — 타워 설치 오작동

## 1. 시스템 구조

`TowerPlacer.Update()`가 매 프레임 `Input.GetMouseButtonDown(0)`을 폴링하여 타워 배치를 처리한다.
Unity UI 이벤트 시스템(`EventSystem`)과 물리 인풋(`Input`)은 독립적으로 동작하므로,
UI 버튼 클릭 시 UI 이벤트와 동시에 `GetMouseButtonDown`도 true가 되어 월드 클릭으로 처리된다.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`

## 3. 신규 클래스 / 파일

없음.

## 4. 테스트 계획

- [ ] UI 팝업(보조 슬롯 해금) 확인 버튼 클릭 → 타워 설치 안 됨 확인
- [ ] UI가 없는 빈 타일 클릭 → 타워 설치 정상 작동 확인
- [ ] 인벤토리, 상점 등 다른 UI 버튼 클릭 → 타워 설치 안 됨 확인

## 5. 위험 요소

- `EventSystem.current`가 null인 경우 null 체크 필요 (이미 처리 예정)
- 모바일 터치 환경에서는 `IsPointerOverGameObject(-1)` 사용 필요 (현재 PC 전용으로 문제 없음)
