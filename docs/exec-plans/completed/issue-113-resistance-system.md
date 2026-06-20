# Issue #113 — 화염 저항 시스템 구현

## 1. 시스템 구조

- `EnemyData`에 `fireResistance` 필드 추가 (범위 -1.0 ~ 0.9)
- `Enemy.Initialize`에서 저항값 로드
- `Enemy.TakeDamage`에서 Fire 타입 피해에 저항 적용
- 계산식: `FinalDamage = Damage * (1 - fireResistance)`

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Enemy/EnemyData.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`

## 3. 저항 기본값 (Inspector 설정)

| 몬스터 타입 | fireResistance |
|------------|---------------|
| 일반 | 0 |
| 화염 저항형 | 0.5 |
| 화염 취약형 | -0.25 |
| 보스 | 0.3 |

## 4. 테스트 계획

- [ ] fireResistance=0.3 적에게 Fire 100 → 70 피해 확인
- [ ] fireResistance=-0.25 적에게 Fire 100 → 125 피해 확인
- [ ] Physical 피해는 저항 미적용 확인

## 5. 위험 요소

- 기존 EnemyData 에셋은 fireResistance 기본값 0 → 기존 동작 유지
- 냉기/번개/독 저항은 별도 이슈로 분리
