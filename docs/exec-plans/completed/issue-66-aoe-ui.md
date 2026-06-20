# Issue #66 — [BUG] Fireball, LightningArrow 제외한 스킬들 AoE UI 표시 안됨

## 1. 시스템 구조

```
[현재 ShowAoeHit 호출 위치]
FireballProjectile.OnHit()       → ShowAoeHit ✓
LightningArrowProjectile.OnHit() → ShowAoeHit ✓
CausticArrowProjectile.OnHit()   → ShowAoeHit ✓
PreciseArrowProjectile.OnHit()   → ShowAoeHit 없음 ✗
FreezingPulseProjectile.OnHit()  → ShowAoeHit 없음 ✗
```

**근본 원인:**
`PreciseArrow`, `FreezingPulse`의 `OnHit()`에 `GameUIManager.ShowAoeHit()` 호출이 없음.

**수정 방향:**
`ProjectileBase.Update()`에서 `OnHit` + `ApplySplash` 처리 후, `SplashRadius > 0`이면
`GameUIManager.ShowAoeHit(target.transform.position, SplashRadius)` 공통 호출.
→ 각 Projectile 클래스에서 중복 호출되어도 같은 위치/반경이므로 무해.

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs` | `Update()` 착탄 블록에 `ShowAoeHit` 공통 호출 추가 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] PreciseArrow에 aoeRadius 설정 후 착탄 시 AoE 원 표시 확인
- [ ] FreezingPulse에 aoeRadius 설정 후 착탄 시 AoE 원 표시 확인
- [ ] Fireball: 기존 원 표시 정상 동작 확인
- [ ] LightningArrow: 기존 원 표시 정상 동작 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| 중복 호출 | Fireball/LightningArrow/CausticArrow는 OnHit + ProjectileBase 양쪽에서 호출 | 같은 위치/반경 중복 → 동일 원 두 번 추가, GameUIManager에서 0.5초 후 소멸로 무해 |
| SplashRadius = 0 | aoeRadius 미설정 스킬은 ShowAoeHit 호출 안 함 | 의도된 동작 |
