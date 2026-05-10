# Issue #64 — [BUG] PreciseArrow, FreezingPulse 스플래시 데미지 적용 안됨 + Fireball 스플래시 적용

## 1. 시스템 구조

```
[PreciseArrow / FreezingPulse 스플래시 미작동]
LaunchPreciseArrow()
  proj.SplashRadius = skill.aoeRadius   ← aoeRadius가 이 스킬 asset에서 0
  → SplashRadius = 0 → ApplySplash 스킵

[Fireball 스플래시 미적용]
LaunchFireball()
  proj.SplashRadius 미설정 → 0 유지
  FireballProjectile.OnHit() → 100% AoE (모든 적)  ← 스플래시 아님
```

**근본 원인:**
- `aoeRadius`는 원래 Fireball 전용 필드였으므로, PreciseArrow/FreezingPulse 등 단일 타겟 스킬 asset에서 0으로 설정되어 있음
- Fireball은 `LaunchFireball`에서 `SplashRadius`를 설정하지 않아 스플래시 미작동

**수정 방향:**
- `SkillData`에 `splashRadius` 전용 필드 추가 (기존 aoeRadius와 분리)
- 모든 단일 타겟 Launch 메서드에서 `proj.SplashRadius = skill.splashRadius` 사용
- Fireball: `SplashRadius = skill.aoeRadius` 설정 + `OnHit()` AoE 루프 제거 → 주 타겟 100% + 스플래시 50%

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `splashRadius` 필드 추가 |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | PreciseArrow/FreezingPulse/CausticArrow: `splashRadius` 사용, Fireball: `SplashRadius = skill.aoeRadius` 설정 |
| `Assets/Scripts/Gameplay/Skills/Projectiles/FireballProjectile.cs` | OnHit에서 AoE 루프 제거, 주 타겟만 풀 데미지 → 스플래시가 나머지 처리 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] PreciseArrow skill asset에 `splashRadius > 0` 설정 후 인접 적 50% 데미지 확인
- [ ] FreezingPulse skill asset에 `splashRadius > 0` 설정 후 인접 적 50% 데미지 확인
- [ ] Fireball: 주 타겟 100%, AoE 범위 내 다른 적 50% 데미지 확인
- [ ] Fireball: 기존처럼 모든 적 100% 피해 받지 않음 확인
- [ ] LightningArrow: 기존 AoE 동작 유지 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| Fireball 동작 변경 | 기존 100% AoE → 100% 주타겟 + 50% 범위 | 의도된 변경 (이슈 요구사항) |
| splashRadius 기본값 0 | 기존 asset에서 0 → 스플래시 없음 | 사용자가 에디터에서 직접 설정 필요 |
| aoeRadius vs splashRadius 혼용 | Fireball은 aoeRadius를 splash에 사용 | LaunchFireball만 aoeRadius 사용, 나머지는 splashRadius |
