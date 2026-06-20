# Issue #150 — IncendiaryRound 불꽃 데미지가 메인 타격과 분리되어 두 번 발생

## 1. 시스템 구조

### 문제 흐름
```
Tower.Attack()
  → SkillDispatcher.Execute()
      → LaunchFireball()         // dmg = AttackDamage + skill.baseDamage
          → proj.Launch(dmg)     // 100 데미지 타격 (1번)
          → ApplyFireDamage()    // AttackDamage * AddedFireRatio = ~1 데미지 (2번)
```

`ApplyFireDamage`가 `tower.AttackDamage`만 기준으로 계산해 `skill.baseDamage`(100)가 누락되고,
별도 `TakeDamage` 호출로 두 번의 타격이 발생.

### 수정 흐름
```
LaunchFireball()
  → dmg = (AttackDamage + skill.baseDamage) * (1 + AddedFireRatio)
  → proj.Launch(dmg)     // 130 데미지 단일 타격
  // ApplyFireDamage 제거
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 테스트 계획

- [ ] Fireball + IncendiaryRound(value=0.3) 장착 타워 → 적 공격
- [ ] 데미지 숫자가 130 단일 타격으로 표시되는지 확인
- [ ] 다른 스킬(PreciseArrow, FreezingPulse, LightningArrow)도 동일하게 합산되는지 확인
- [ ] IncendiaryRound 미장착 시 기존 데미지(100)와 동일한지 확인

## 5. 위험 요소

- `AddedFireRatio` 값이 비율(0.3)이 아닌 퍼센트(30)로 설정된 경우 데미지 과다 발생
  → SupportOptionData `value` 필드를 0~1 범위(비율)로 통일 필요
- CausticArrow는 DoT 스킬이므로 `ApplyFireDamage` 적용 대상에서 제외 (기존대로 유지)
