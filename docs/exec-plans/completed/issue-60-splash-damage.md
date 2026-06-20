# Issue #60 — [Feat] AOE 영역만큼 스플래시 데미지 구현

## 1. 시스템 구조

```
ProjectileBase.Update()
  → OnHit(target)          ← 주 타겟 풀 데미지
  → ApplySplash(target)    ← AoE 반경 내 다른 적 50% 데미지

SkillDispatcher.Launch*()
  → proj.SplashRadius = skill.aoeRadius
```

**스플래시 메커니즘:**
- `ProjectileBase`에 `SplashRadius` 프로퍼티 추가
- `ApplySplash(Enemy primaryTarget)` 보호 메서드: `SplashRadius` 내 적 중 주 타겟 제외, `_damage * 0.5f` 적용
- `Update()` 내 `OnHit()` 직후 `ApplySplash()` 호출
- `SkillDispatcher` 각 Launch 메서드에서 `proj.SplashRadius = skill.aoeRadius` 설정

**스킬별 처리:**

| 스킬 | 처리 |
|------|------|
| PreciseArrow | SplashRadius = skill.aoeRadius → 범위 내 50% 스플래시 |
| FreezingPulse | SplashRadius = skill.aoeRadius → 범위 내 50% 스플래시 |
| CausticArrow | SplashRadius = skill.aoeRadius → 착탄 즉시 50% 스플래시 (장판과 별개) |
| Fireball | SplashRadius = 0 — 자체 OnHit에서 이미 100% AoE 처리 |
| LightningArrow | SplashRadius = 0 — 자체 OnHit에서 이미 100% AoE 처리 |

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs` | `SplashRadius` 프로퍼티, `ApplySplash()` 메서드, `Update()` 내 호출 |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | 각 Launch 메서드에서 `proj.SplashRadius = skill.aoeRadius` 설정 |
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `aoeRadius` 주석 "Fireball 전용" → 전 스킬 공용으로 변경 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] PreciseArrow: aoeRadius > 0 설정 시 인접 적 50% 데미지 확인
- [ ] FreezingPulse: aoeRadius > 0 설정 시 인접 적 50% 데미지 확인
- [ ] CausticArrow: 착탄 즉시 인접 적 50% 데미지 + 장판 DoT 별도 동작 확인
- [ ] Fireball: 기존 100% AoE 동작 유지, 이중 데미지 없음 확인
- [ ] LightningArrow: 기존 AoE 동작 유지 확인
- [ ] 주 타겟은 스플래시 데미지 미적용 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| 주 타겟 이중 데미지 | ApplySplash에서 주 타겟 포함될 수 있음 | primaryTarget 비교로 스킵 |
| Fireball/LightningArrow 이중 AoE | 기존 OnHit + 스플래시 중복 | SplashRadius = 0 설정으로 스킵 |
| ActiveEnemies 순회 중 제거 | TakeDamage → Die() → Remove | 역순 인덱스 순회 |
| aoeRadius 미설정 | 기존 asset에서 0일 경우 스플래시 없음 | SplashRadius = 0 → ApplySplash 스킵 |
