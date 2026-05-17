# Issue #109 — Added Fire Damage Support 보조 스킬 구현

## 1. 시스템 구조

```
[장착 흐름]
SupportSlotUI → InventorySystem.SetSupportOption(slot, IncendiaryRound 에셋)
  → Tower.SetSupportOption() → RefreshStats()
      → AccumulateSupportOption(IncendiaryRound)
          → Tower.AddedFireRatio += option.value (e.g. 0.30씩 누적)

[전투 흐름]
Tower.Attack() → SkillDispatcher.Execute()
  → 스킬별 물리 피해 적용 (기존)
  → tower.AddedFireRatio > 0 && 스킬이 Hit 가능한 경우:
      fireDamage = tower.AttackDamage * tower.AddedFireRatio
      → target.TakeDamage(fireDamage, armorPen=0, isCrit, DamageType.Fire)

[제외 조건]
SkillData.isDoTOnly == true → 화염 피해 미적용
  → CausticArrow (DoT 전용)
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `isDoTOnly` 필드 추가 |
| `Assets/Scripts/Gameplay/Tower/Tower.cs` | `AddedFireRatio` 프로퍼티 추가, `AccumulateSupportOption()` 추가, `RefreshStats()`에 보조 옵션 루프 |
| `Assets/Scripts/Gameplay/Enemy/Enemy.cs` | `TakeDamage` 오버로드 추가 — `DamageType` 파라미터 지원 |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | 각 스킬 실행 후 `ApplyFireDamage()` 헬퍼 호출 |

## 3. 신규 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Gameplay/DamageType.cs` | `enum DamageType { Physical, Fire }` 선언 |

## 4. 구현 세부

### DamageType.cs
```csharp
public enum DamageType { Physical, Fire }
```

### SkillData.cs 추가 필드
```csharp
[Header("Support Restrictions")]
public bool isDoTOnly;   // true → Added Fire Damage 미적용 (CausticArrow)
```

### Tower.cs 추가 내용
```csharp
public float AddedFireRatio { get; private set; }

// RefreshStats() 내:
AddedFireRatio = 0f;
for (int i = 0; i < _unlockedSupportSlots; i++)
{
    var opt = _supportSlots[i];
    if (opt == null) continue;
    AccumulateSupportOption(opt);
}

private void AccumulateSupportOption(SupportOptionData opt)
{
    switch (opt.optionType)
    {
        case SupportOptionType.IncendiaryRound: AddedFireRatio += opt.value; break;
        // 이후 다른 보조 옵션 추가
    }
}
```

### SkillDispatcher.cs 추가
```csharp
private static void ApplyFireDamage(Tower tower, Enemy target, bool isCrit)
{
    if (tower.AddedFireRatio <= 0f) return;
    var skill = tower.EquippedSkill;
    if (skill != null && skill.isDoTOnly) return;

    float fireDmg = tower.AttackDamage * tower.AddedFireRatio;
    target.TakeDamage(fireDmg, 0f, isCrit, DamageType.Fire);
}
```

각 Launch/DirectAttack 마지막에 `ApplyFireDamage(tower, target, isCrit)` 호출.

### Enemy.TakeDamage 확장
```csharp
public void TakeDamage(float damage, float armorPenRatio = 0f,
                       bool isCrit = false, DamageType type = DamageType.Physical)
{
    float effectiveDefense = (type == DamageType.Physical)
        ? _defense * (1f - Mathf.Clamp01(armorPenRatio))
        : 0f;   // 화염 피해는 방어력 무시
    float actual = Mathf.Max(0f, damage - effectiveDefense);
    CurrentHp -= actual;
    GameUIManager.ShowDamage(transform.position, actual, isCrit);
    if (CurrentHp <= 0f) Die();
}
```

## 5. 테스트 계획

- [ ] `IncendiaryRound` SupportOptionData 에셋 생성 (value = 0.30)
- [ ] 타워 AttackDamage = 100, IncendiaryRound 장착 → 공격 시 물리 100 + 화염 30 = 130 확인
- [ ] CausticArrow 장착 타워에 IncendiaryRound → 화염 피해 미적용 확인
- [ ] IncendiaryRound 2개 장착 시 AddedFireRatio = 0.60 → 화염 60 확인
- [ ] 화염 피해는 방어력 무시 확인 (방어력 높은 적에게도 30 그대로)

## 6. 위험 요소

- 기존 `TakeDamage(float, float, bool)` 호출부 전부 유지 (기본값 `DamageType.Physical` 으로 하위 호환)
- `CausticArrow`의 `isDoTOnly = true`는 Inspector에서 직접 설정 필요
- 향후 Ignite/저항 시스템 추가 시 `DamageType.Fire` 분기 확장 가능
- 다중 타겟(스플래시) 공격 시 각 타겟마다 `ApplyFireDamage` 호출 필요 → ProjectileBase 레벨에서도 처리 검토
