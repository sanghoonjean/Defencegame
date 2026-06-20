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
  → if (IsBrutalityActive && skill.damageNature != Physical)
        → return; (발사 자체 차단)
  → Physical 스킬만 진입 — 현재는 Molten Strike 가 유일한 Physical 베이스
      → ExecuteMoltenStrike: IsBrutalityActive 면 physToFireRatio = 0 으로 강제
      → target.TakeDamage(dmg, armorPen, isCrit, DamageType.Physical)
      → ApplyFireDamage 스킵 (AddedFireRatio == 0 이므로 자동)
```

### Damage Type Filtering 정책

| 스킬 | skillType | 분류 | Brutality 와 함께 사용 |
|------|-----------|------|------------------------|
| Fireball | Fireball | Fire | ❌ 발사 차단 |
| FreezingPulse | FreezingPulse | Cold | ❌ |
| LightningArrow | LightningArrow | Lightning | ❌ |
| LightningSpear | LightningSpear | Lightning | ❌ |
| ParalysisMagic | ParalysisMagic | Lightning(Shock) | ❌ |
| PoisonCloud | PoisonCloud | Chaos/Poison | ❌ |
| CausticArrow | CausticArrow | Chaos DoT | ❌ |
| **MoltenStrike** | **MoltenStrike** | **Physical** (Fire 변환 60%) | ✅ **변환 차단 후 100% Physical** |

→ `SkillData` 에 `damageNature` enum 을 추가해 스킬마다 명시. **8 종 모두 Inspector 일괄 갱신**.
→ MoltenStrike 는 PoE 룰 그대로 Physical 베이스이며, Brutality 활성 시
   `ExecuteMoltenStrike` 가 `physToFireRatio` 를 0 으로 강제 → 전량 Physical 로 타격.

### 코덱스 P1 대응 (선행 이슈 #264 머지로 해소)

> Codex P1 — PR #263 line 41:
> "Tower.Update 는 `EquippedSkill == null` 이면 즉시 return 하므로 DirectAttack 경로 도달 불가.
>  현재 모든 스킬이 elemental/chaos 분류면 Brutality 가 데미지 경로 없음 → 죽은 옵션."

**해소**: 이슈 #264 머지 (Molten Strike) 로 Physical 베이스 스킬이 합류.
- Brutality + Molten Strike → Physical 100% 타격 경로 확보
- 이슈 본문 테스트 절차 ("Base Physical Damage = 100 → Brutality → 160") 는
  Molten Strike + Brutality 강제 `physToFireRatio = 0` 으로 직접 검증
- 향후 Cyclone / HeavyStrike / BladeVortex 등 추가 시 `damageNature = Physical` 만 분류하면 자동 호환

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

기존 8 종 SkillData 에셋의 `damageNature` 는 UnityMCP 로 일괄 세팅:
- Fireball → Fire / FreezingPulse → Cold / LightningArrow, LightningSpear, ParalysisMagic → Lightning
- PoisonCloud, CausticArrow → Chaos
- **MoltenStrike → Physical** (Fire 변환은 스킬 내부 처리, baseline 분류는 Physical)

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

// ExecuteMoltenStrike 내부:
float fireFrac = tower.IsBrutalityActive ? 0f : Mathf.Clamp01(skill.physToFireRatio);
// → phys = dmg, fire = 0 → Brutality 적용 시 fire conversion 완전 차단
```

> Physical 스킬이 추후 추가될 때도 `damageNature = Physical` 이면 자동 호환.
> Molten Strike 의 Fire 변환 비율도 Brutality 활성 시 0 으로 강제 (PoE 룰).

### UI 표기
- `SupportSlotUI` 가 Brutality 일 때 description 텍스트에 빨강 경고 ("Cannot Deal Elemental or Chaos Damage")
- Tower 상세 패널이 있다면 `IsBrutalityActive == true` 시 동일 경고 추가
- 구현 범위: 기존 description 필드 활용 + 슬롯 UI 색상 강조 (별도 위젯 없음)

## 5. 테스트 계획

- [ ] `BrutalitySupport.asset` 생성 (value = 0.60) — UnityMCP 사용
- [ ] 기존 SkillData 에셋 8 종 `damageNature` 일괄 설정 (MoltenStrike=Physical 포함)
- [ ] MoltenStrike + BrutalitySupport / Base 100 → 1차 근접 타격 phys = 160, fire = 0 확인
- [ ] MoltenStrike + BrutalitySupport + IncendiaryRound(0.3) → Fire/AddedFire 모두 0 확인
- [ ] MoltenStrike + BrutalitySupport + IgniteChance 옵션 → Ignite 발생 0 확인
- [ ] Fireball / FreezingPulse / LightningArrow / CausticArrow / PoisonCloud / LightningSpear / ParalysisMagic
       + BrutalitySupport → 발사 자체가 일어나지 않음 (no projectile / no AoE)
- [ ] EnergyDrain(DoT) + BrutalitySupport → DotDamageRatio = 0 으로 무효
- [ ] PiercingRound / ChainCircuit + BrutalitySupport + MoltenStrike 마그마 투사체 → 정상 동작 (Physical 만 영향)
- [ ] BrutalitySupport 미장착 시 모든 기존 동작 동일 확인 (회귀 없음)

## 6. 위험 요소

- 기존 8 개 SkillData 에셋 모두 `damageNature` 가 기본값 `Physical` 로 시작되므로
  Brutality + Fireball 같은 조합이 통과되는 회귀 위험 → 에셋 일괄 갱신 누락 시 표면화.
  → 안전장치: Inspector 에서 미세팅 시 콘솔 경고 추가 검토 (Phase 2).
- `BrutalityMultiplier` 가 곱연산(More)이므로 IncendiaryRound 같은 Added 합산과 달리 중복 장착 시 곱누적. value 0.60 두 개 → ×2.56. 의도된 동작이지만 슬롯 중복 정책([[issue-174-support-no-duplicate-slot]]) 으로 1 개로 제한됨을 확인.
- AddedFireRatio / DotDamageRatio 등을 RefreshStats 마지막에 0 으로 덮어쓰므로
  순서 의존성 발생 — 향후 보조 옵션 추가 시 동일 후처리 블록에서 처리 필요.
- Molten Strike 의 마그마 투사체 (2 차 폭발) 도 Brutality 활성 시 `BaseFireDamage = 0` 으로 발사돼야 함.
  → `ExecuteMoltenStrike` 의 `fireFrac = 0` 강제로 자동 해결 (fire = 0 → proj.BaseFireDamage = 0).

## 7. 참고

- 베이스: Path of Exile — Brutality Support
- Modifier Category: Physical / Restriction / Offensive Support
- 후속 작업 후보: Physical Scaling 시스템 (BasePhysical %, More Physical 등), Damage Type Filter 공통 인프라
