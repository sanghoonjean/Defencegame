# Issue #185 — 냉기 저항 시스템 구현 (ColdResistance)

## 1. 시스템 구조

- `DamageType`에 `Cold` 추가
- `EnemyData.coldResistance` 필드 추가 (범위 -1.0 ~ 0.9)
- `Enemy.TakeDamage`에서 Cold 타입 저항 적용
- `FreezingPulseProjectile.OnHit` 피해 타입을 `DamageType.Cold`로 변경
- 계산식: `FinalColdDamage = ColdDamage × (1 - coldResistance)`

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/DamageType.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/EnemyData.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/FreezingPulseProjectile.cs`
- `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs`

## 3. 테스트 계획

- [ ] coldResistance=0.5 적에게 Cold 100 → 50 피해
- [ ] coldResistance=-0.25 적에게 Cold 100 → 125 피해
- [ ] Physical 피해는 저항 미적용

## 4. 위험 요소

- 기존 EnemyData 에셋은 coldResistance 기본값 0 → 기존 동작 유지
- fireResistance와 동일한 구조 패턴 적용
