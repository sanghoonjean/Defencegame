# Issue #23 — [Bug] 웨이브 시작 동작 안됨

## 1. 시스템 구조

```
TestRunner.Update()
  → Input.GetKeyDown(KeyCode.A)
      → WaveSystem.SetAutoWave(true)   ← 플래그만 설정, StartWave() 미호출
      → (웨이브 시작 없음)

TestRunner.Update()
  → Input.GetKeyDown(KeyCode.R)
      → GameStateSystem.ResetToPlaying()  ← GameState만 변경
      → WaveSystem.StopWave() 미호출
      → Enemy 제거 없음
      → PlayerSystem.ResetHp() 미호출
```

**근본 원인:**
- `A키`: `SetAutoWave(true)`는 웨이브 종료 시 자동 재시작 플래그만 세움. 현재 웨이브가 없으면 아무것도 시작되지 않음
- `R키`: `GameStateSystem.ResetToPlaying()`은 상태값만 변경. 진행 중인 웨이브·적·HP는 그대로 남음

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/TestRunner.cs` | A키: `StartWave()` 추가 호출 / R키: `StopWave()` + 적 전체 반납 + `ResetHp()` 추가 |

---

## 3. 신규 클래스 / 파일

없음 (기존 TestRunner.cs 수정만)

---

## 4. 테스트 계획

- [ ] A키 누르면 즉시 웨이브 스폰 시작 확인
- [ ] A키 후 웨이브 종료 시 자동으로 다음 웨이브 시작 확인
- [ ] R키 누르면 진행 중인 웨이브 중단 확인
- [ ] R키 후 화면에서 모든 적 제거 확인
- [ ] R키 후 플레이어 HP 초기화 확인
- [ ] R키 후 GameState가 Playing으로 복귀 확인
- [ ] Space키 기존 동작 영향 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| R키 적 제거 중 예외 | `Enemy.ActiveEnemies` 순회 중 Return() 호출 → 리스트 변경 | `ToArray()` 복사본으로 순회 |
| TestRunner는 개발 전용 | 빌드 포함 시 문제 없으나 의도치 않은 키 입력 가능 | 기존 주석(`빌드 전 삭제`) 유지 |
