# Issue #111 — Brutality Support 보조 스킬 구현

## 1. 시스템 구조

Path of Exile 의 Brutality Support 를 모방한 **고위험 고효율 Physical 전용 Modifier**.
연결된 스킬의 물리 피해를 ×1.60 으로 증폭하지만, 동시에
Fire / Cold / Lightning / Poison 피해를 전부 0 으로 만든다.

```
[장착 흐름]
SupportSlotUI → InventorySystem.SetSupportOption(slot, BrutalitySupport 에셋)
  → Tower.SetSupportOption() → RefreshStats()
      → AccumulateSupportOption(BrutalitySupport)
          → BrutalityMultiplier *= (1f + opt.value)        // 1.60 (60% More)
          → IsBrutalityActive = true                       // 원소/카오스 차단 플래그

[스탯 확정 단계]  (RefreshStats 후반)
if (IsBrutalityActive)
{
    AttackDamage *= BrutalityMultiplier        // Physical 증폭
    // 원소/카오스 보조 옵션 효과 무효화
    AddedFireRatio = 0f
    IgniteChance   = 0f
    DotDamageRatio = 0f   // EnergyDrain(독계열 DoT)
    DotDuration    = 0f
}

[전투 흐름]
Tower.Attack() → SkillDispatcher.Execute(tower, target)
  → if (skill != null && IsBrutalityActive && skill.HasElementalOrChaos)
        → return; (아예 발사 안 함, 또는 Physical Fallback)
  → DirectAttack(applyFire:false) 또는 Physical 스킬만 실행
      → target.TakeDamage(dmg, armorPen, isCrit, DamageType.Physical)
      → ApplyFireDamage 스킵 (AddedFireRatio == 0)
```

### Damage Type Filtering 정책

| 스킬 | skillType | 분류 | Brutality 와 함께 사용 |
|------|-----------|------|------------------------|
| (스킬 미장착) | — | Physical | ✅ 증폭 |
| Fireball | Fireball | Fire | ❌ 발사 차단 |
| FreezingPulse | FreezingPulse | Cold | ❌ |
| LightningArrow | LightningArrow | Lightning | ❌ |
| LightningSpear | LightningSpear | Lightning | ❌ |
| ParalysisMagic | ParalysisMagic | Lightning(Shock) | ❌ |
| PoisonCloud | PoisonCloud | Chaos/Poison | ❌ |
| CausticArrow | CausticArrow | Chaos DoT | ❌ |
| (향후) Cyclone / HeavyStrike / BladeVortex | — | Physical | ✅ |

→ `SkillData` 에 `damageNature` enum 을 추가해 스킬마다 명시한다.
   현재 7 종 모두 elemental/chaos 이므로 Inspector 일괄 갱신.

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `MakeDefence/Assets/Scripts/Gameplay/Tower/SupportOptionData.cs` | `SupportOptionType` 에 `BrutalitySupport` 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Tower/SkillData.cs` | `SkillDamageNature` enum + `damageNature` 필드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs` | `BrutalityMultiplier`, `IsBrutalityActive` 프로퍼티 / `AccumulateSupportOption` 분기 / `RefreshStats` 후처리 (원소 무효화 + Physical 증폭) |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `Execute` 진입부에서 `IsBrutalityActive && skill.damageNature != Physical` 인 경우 발사 차단 (no-op) |
| `MakeDefence/Assets/Scripts/UI/SupportSlotUI.cs` (or 툴팁) | Brutality 장착 슬롯 / 타워 상세 패널에 "Cannot Deal Elemental or Chaos Damage" 표기 |

## 3. 신규 클래스 / 파일

없음 — 기존 enum / 구조 확장으로 충분.

> 단, 에셋 `BrutalitySupport.asset` (SupportOptionData ScriptableObject) 은
> UnityMCP 로 별도 생성 필요. value = 0.60 (= 60% More Physical).
> 메모리 노트 [[feedback_unity_asset_edits]] 에 따라 `.asset` 은 MCP 도구로만 편집.

## 4. 구현 세부

### SupportOptionData.cs
```csharp
public enum SupportOptionType
{
    OverloadModule, AccelChip, AoeAmplifier, MultiProjectile, ThresholdCircuit, CritAmplifier,
    EmpAmplifier, CoolantDevice, CorrosiveRound, IncendiaryRound,
    ChainCircuit, PiercingRound, EnergyDrain,
    BrutalitySupport,   // ← 추가
}
```

### SkillData.cs
```csharp
public enum SkillDamageNature
{
    Physical,           // Brutality 호환
    Fire, Cold, Lightning,
    Chaos,              // Poison / DoT 계열
}

[Header("Damage Classification")]
public SkillDamageNature damageNature = SkillDamageNature.Physical;
```

기존 7 종 SkillData 에셋의 `damageNature` 는 UnityMCP 로 일괄 세팅:
- Fireball → Fire / FreezingPulse → Cold / LightningArrow, LightningSpear, ParalysisMagic → Lightning
- PoisonCloud, CausticArrow → Chaos

### Tower.cs
```csharp
public float BrutalityMultiplier { get; private set; }  // 1.0 baseline
public bool  IsBrutalityActive   { get; private set; }

// RefreshStats 초기화
BrutalityMultiplier = 1f;
IsBrutalityActive   = false;
// ... 기존 보조 옵션 루프

// 마지막 단계에서 Physical 증폭 + 원소 무효화
if (IsBrutalityActive)
{
    AttackDamage  *= BrutalityMultiplier;
    AddedFireRatio = 0f;
    IgniteChance   = 0f;
    DotDamageRatio = 0f;
    DotDuration    = 0f;
}
```

```csharp
private void AccumulateSupportOption(SupportOptionData opt)
{
    switch (opt.optionType)
    {
        // ... 기존 케이스
        case SupportOptionType.BrutalitySupport:
            BrutalityMultiplier *= 1f + Mathf.Clamp01(opt.value);  // value=0.6 → ×1.6
            IsBrutalityActive   = true;
            break;
    }
}
```

### SkillDispatcher.cs
```csharp
public static void Execute(Tower tower, Enemy target)
{
    var skill = tower.EquippedSkill;
    if (skill == null) { DirectAttack(tower, target); return; }

    // Brutality + 원소/카오스 스킬 → 발사 차단
    if (tower.IsBrutalityActive && skill.damageNature != SkillDamageNature.Physical)
        return;

    switch (skill.skillType) { /* 기존 분기 유지 */ }
}
```

> `DirectAttack` 은 이미 Physical 이므로 별도 처리 불필요.
> Physical 스킬이 추후 추가될 때도 `damageNature = Physical` 이면 자동 호환.

### UI 표기
- `SupportSlotUI` 가 Brutality 일 때 description 텍스트에 빨강 경고 ("Cannot Deal Elemental or Chaos Damage")
- Tower 상세 패널이 있다면 `IsBrutalityActive == true` 시 동일 경고 추가
- 구현 범위: 기존 description 필드 활용 + 슬롯 UI 색상 강조 (별도 위젯 없음)

## 5. 테스트 계획

- [ ] `BrutalitySupport.asset` 생성 (value = 0.60) — UnityMCP 사용
- [ ] 기존 SkillData 에셋 7 종 `damageNature` 일괄 설정
- [ ] Base AttackDamage = 100 / 스킬 미장착 / BrutalitySupport 1 개 → AttackDamage = 160 확인
- [ ] BrutalitySupport + IncendiaryRound(value=0.3) 동시 장착
       → AddedFireRatio = 0, Fire Damage = 0, Total = 160 (Physical only)
- [ ] Fireball 스킬 + BrutalitySupport → 발사 자체가 일어나지 않음 (no projectile)
- [ ] CausticArrow + BrutalitySupport → 발사 차단, DoT 미적용
- [ ] EnergyDrain(DoT) + BrutalitySupport → DotDamageRatio = 0 으로 무효
- [ ] IgniteChance 옵션 + BrutalitySupport → Ignite 발생 0 확인
- [ ] PiercingRound / ChainCircuit + BrutalitySupport + (향후 Physical 스킬) → 정상 동작 (Physical 만 영향)
- [ ] BrutalitySupport 미장착 시 모든 기존 동작 동일 확인 (회귀 없음)

## 6. 위험 요소

- 기존 7 개 SkillData 에셋 모두 `damageNature` 가 기본값 `Physical` 로 시작되므로
  Brutality + Fireball 같은 조합이 통과되는 회귀 위험 → 에셋 일괄 갱신 누락 시 표면화.
  → 안전장치: Inspector 에서 미세팅 시 콘솔 경고 추가 검토 (Phase 2).
- 현재 Physical 전용 스킬이 존재하지 않으므로 Brutality 의 실효 가치는 "스킬 미장착 타워" + 향후 추가될 Physical 스킬에 한정.
  → 이슈 본문의 "Cyclone Hero / Heavy Strike Tower" 등은 별도 이슈에서 구현 예정.
- `BrutalityMultiplier` 가 곱연산(More)이므로 IncendiaryRound 같은 Added 합산과 달리 중복 장착 시 곱누적. value 0.60 두 개 → ×2.56. 의도된 동작이지만 슬롯 중복 정책([[issue-174-support-no-duplicate-slot]]) 으로 1 개로 제한됨을 확인.
- AddedFireRatio / DotDamageRatio 등을 RefreshStats 마지막에 0 으로 덮어쓰므로
  순서 의존성 발생 — 향후 보조 옵션 추가 시 동일 후처리 블록에서 처리 필요.

## 7. 참고

- 베이스: Path of Exile — Brutality Support
- Modifier Category: Physical / Restriction / Offensive Support
- 후속 작업 후보: Physical Scaling 시스템 (BasePhysical %, More Physical 등), Damage Type Filter 공통 인프라
