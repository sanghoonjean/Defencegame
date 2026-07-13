# Issue #391 — EnemyData_lastBoss 의 fixedStats 미설정

## 1. 시스템 구조

`Enemy.Initialize()` 는 `data.fixedStats` 가 true 면 스테이지/레벨 기반 난이도 공식을
건너뛰고 baseHp/baseDefense/baseSpeed 를 그대로 사용한다 (Rift 배율만 곱 적용).

`EnemyData.cs` 의 `fixedStats` 필드 주석은 "LastBoss는 난이도 공식 미적용" 이라고
명시하지만, 실제 `EnemyData_lastBoss.asset` 은 `fixedStats: 0` 으로 직렬화되어 있어
LastBoss 스폰 시 난이도 공식이 적용된다 — 데이터가 코드 의도와 불일치.

- 데이터 전용 수정 — 코드 변경 없음
- 현재 `WaveSystem` 스폰 슬롯(normal/magic/rare/unique)에 lastBoss 미연결이라
  즉시 체감되는 버그는 아니지만, 향후 LastBoss 스폰 도입 시 잘못된 스탯으로 등장하게 됨

## 2. 수정 파일

- `MakeDefence/Assets/EnemyData_lastBoss.asset`
  - `fixedStats: 0` → `1` (UnityMCP `manage_scriptable_object` 로 수정)

## 3. 신규 클래스 / 파일

- 없음

## 4. 테스트 계획

- [ ] 에셋 diff 에서 `fixedStats: 1` 확인
- [ ] Unity 콘솔 에러/경고 없음
- [ ] execute_code 검증: lastBoss EnemyData 로 `Enemy.Initialize(stage=12)` 호출 시
      MaxHp == baseHp (공식 미적용) 확인
- [ ] 기존 EditMode 테스트 전체 통과 (EnemyLevelTests 의 fixedStats 케이스 포함)

## 5. 위험 요소

- LastBoss 는 현재 스폰 경로에 연결되어 있지 않아 게임플레이 영향 없음 —
  향후 보스 웨이브 도입 시점에 baseHp(현재 값) 가 적정한지 별도 밸런싱 필요
- grade 필드는 PR #390 에서 이미 LastBoss(4) 로 수정됨 — 이 이슈는 fixedStats 만 다룸
