# Issue #114 — 스플래시 범위 내 모든 적에게 화염 피해 적용

## 1. 시스템 구조

- `ProjectileBase.ApplySplash`가 범위 내 적에게 물리 데미지만 적용
- IncendiaryRound(AddedFireRatio) 장착 시에도 스플래시 대상에는 화염 미적용 → 주 타겟만 받음
- 수정: 스플래시 대상에도 `ApplyFireOnHit` 호출

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs`

## 3. 신규 파일

- 없음

## 4. 구현 상세

`ApplySplash` 루프 내, `e.TakeDamage(splashDmg, ...)` 직후 `ApplyFireOnHit(e, isCrit)` 호출 추가.
기존 `ApplyFireOnHit`는 `AddedFireRatio <= 0` / 사망 enemy 가드를 이미 가지고 있으므로 그대로 재사용.

## 5. 테스트 계획

- [ ] Fireball + IncendiaryRound 장착 → 폭발 범위 내 모든 적 화염 피해 확인
- [ ] AddedFireRatio = 0 (서포트 미장착) → 화염 피해 없음 확인
- [ ] 단일 타겟 스킬(PreciseArrow 등) 동작 변화 없음 확인

## 6. 위험 요소

- ApplyFireOnHit는 사망 적 가드(`CurrentHp <= 0`) 있음 — splashDmg로 즉사한 적은 화염 안 받음 (의도된 동작)
