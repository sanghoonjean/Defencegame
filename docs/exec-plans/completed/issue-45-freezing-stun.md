# Issue #45 — [BUG] Freezing Pulse Stun 걸리지 않음

## 1. 시스템 구조

```
LaunchFreezingPulse()
  → proj.StunChance = tower.StunChance   ← 문제 원인
      tower.StunChance = 아이템 옵션 합산 값
      아이템 미장착 시 = 0 → 빙결 발동 확률 0%
```

**근본 원인:**
`SkillData`에 스킬 자체의 기본 스턴 확률 필드가 없어서,
`FreezingPulseProjectile.StunChance`가 오직 `tower.StunChance`(아이템)에만 의존한다.
사용자가 `stunDuration = 100`을 설정해도 이는 빙결 **지속시간**이지 **확률**이 아니므로 효과 없음.

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `baseStunChance` 필드 추가 |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `LaunchFreezingPulse`에서 `skill.baseStunChance + tower.StunChance` 합산 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] FreezingPulseSkill.asset의 `baseStunChance = 100` 설정 후 빙결 발동 확인
- [ ] `baseStunChance = 50` 설정 시 약 50% 확률로 빙결 발동 확인
- [ ] 아이템 StunChance + baseStunChance 합산 적용 확인
- [ ] Fireball / PreciseArrow 동작 간섭 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| baseStunChance 필드 추가 | 기존 asset에 기본값 0으로 추가됨 | 기존 스킬 동작 변화 없음 |
| stunDuration 필드 역할 혼동 | 사용자가 stunDuration을 확률로 오해 | baseStunChance 명칭으로 명확히 구분 |
