# Issue #391 — EnemyData_lastBoss 의 fixedStats 미설정

## 0. 전제 (선행 의존성)

**구현 브랜치는 PR #390 (feat #388, 몬스터 레벨) 머지 후 main 에서 생성한다.**

- lastBoss `grade: 0 → 4(LastBoss)` 수정과 `EnemyLevelTests` 는 PR #390 에 포함되어 있음
- #390 이 머지되지 않은 상태로 구현하게 될 경우, grade 수정을 이 이슈 스코프에 포함한다

## 1. 시스템 구조

`Enemy.Initialize()` 는 `data.fixedStats` 가 true 면 스테이지/레벨 기반 난이도 공식을
건너뛰고 baseHp/baseDefense/baseSpeed 를 그대로 사용한다 (Rift 배율만 곱 적용).

`docs/product-specs/boss-system.md` 의 LastBoss 스펙과 실제
`EnemyData_lastBoss.asset` 직렬화 값이 불일치한다:

| 필드 | 스펙 | 현재 에셋 | 판정 |
|------|------|-----------|------|
| baseHp | 5000 | 5000 | OK |
| baseDefense | 200 | 0 | **수정 필요** |
| baseSpeed | 2 | 2 | OK |
| playerDamage | 100 | 100 | OK |
| fixedStats (난이도 공식 미적용) | true | 0 | **수정 필요** |
| grade | LastBoss | 0 → PR #390 에서 4 로 수정 | #390 의존 |

- 데이터 전용 수정 — 코드 변경 없음
- 현재 `WaveSystem` 스폰 슬롯(normal/magic/rare/unique)에 lastBoss 미연결이라
  즉시 체감되는 버그는 아니지만, 향후 LastBoss 스폰 도입 시 잘못된 스탯으로 등장하게 됨

## 2. 수정 파일

- `MakeDefence/Assets/EnemyData_lastBoss.asset` (UnityMCP `manage_scriptable_object` 로 수정)
  - `fixedStats: 0` → `1`
  - `baseDefense: 0` → `200` (boss-system.md 스펙 준수)

## 3. 신규 클래스 / 파일

- 없음

## 4. 테스트 계획

- [ ] 에셋 diff 에서 `fixedStats: 1`, `baseDefense: 200` 확인
- [ ] Unity 콘솔 에러/경고 없음
- [ ] execute_code 검증: lastBoss EnemyData 에셋을 직접 로드해 `Enemy.Initialize(stage=12)`
      호출 시 MaxHp == 5000 (공식 미적용) 확인 — 신규 테스트 파일 없이 에디터에서 직접 검증
- [ ] EditMode 테스트 전체 통과 (#390 머지 후이므로 `EnemyLevelTests` 의
      fixedStats 케이스 포함 — CreateInstance 기반이라 에셋 값과는 독립)

## 5. 위험 요소

- LastBoss 는 현재 스폰 경로에 연결되어 있지 않아 게임플레이 영향 없음 —
  향후 보스 웨이브 도입 시점에 스펙 수치 자체의 적정성은 별도 밸런싱 필요
- baseDefense 200 적용 시 물리 데미지가 크게 감쇄됨 (`TakeDamage` 의 방어 차감) —
  스펙 문서 기준 값이므로 의도된 동작이나, 보스 웨이브 도입 시 체감 확인 필요
- grade 필드는 PR #390 에서 수정 — 전제(§0) 참조
