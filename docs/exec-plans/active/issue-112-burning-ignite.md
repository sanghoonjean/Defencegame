# Issue #112 — 화상(Burning/Ignite) 상태이상 시스템 구현

## 1. 시스템 구조

- Fire Damage Hit 발생 시 `IgniteChance` 판정
- 성공 시 대상에게 Burning(화상) 상태 부여
- Burning은 Fire DoT로 분류 (Hit 아님 → 재귀 Ignite 없음)
- 중첩 규칙: 가장 강한 Burning 1개만 유지 (새 DPS > 기존 DPS일 때만 갱신)

계산식:
- IgniteTotalDamage = FireHitDamage × 0.40
- IgniteDuration = 4초
- IgniteDPS = IgniteTotalDamage / 4

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/FireballProjectile.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Tower/ItemData.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs`

## 3. 신규 클래스 / 파일

없음 (기존 구조 확장)

## 4. 테스트 계획

- [ ] FireHitDamage=100, IgniteChance=100% → BurningDPS=10, 4초간 총 40 피해
- [ ] 낮은 DPS Burning 후 높은 DPS Burning → 갱신됨
- [ ] 높은 DPS Burning 후 낮은 DPS Burning → 기존 유지
- [ ] Burning 틱 피해가 다시 Ignite를 발생시키지 않음
- [ ] Physical/Energy 피해에는 Ignite 발생 안 함

## 5. 위험 요소

- 기존 ApplyDot(EnergyDrain)과 ApplyBurning은 별도 코루틴으로 분리 → 중복 없음
- Burning 틱은 TakeDamage 직접 호출 (ignite 체크 없음)
- 기존 EnemyData 에셋은 영향 없음
