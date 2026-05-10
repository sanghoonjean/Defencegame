# Issue #39 — Lightning Arrow 스킬 구현

## 1. 시스템 구조

```
[TestRunner OnGUI]
  → GUI.Button("Lightning Arrow") → _selectedSkill = lightningArrowSkill

[T키]
  → InventorySystem.EquipSkill(_selectedSkill)

Tower.Attack()
  → SkillDispatcher.Execute(tower, target)
        [SkillType.LightningArrow]
          → ObjectPoolSystem.GetProjectile<LightningArrowProjectile>()
          → LightningArrowProjectile.Launch(origin, target, damage, armorPen)
                → 단일 타겟 추적
                → 착탄 시 OnHit():
                    aoeRadius 내 모든 적에게 동일 피해
                    크리티컬 시 → 범위 내 모든 적 감전(스턴) 확정
```

**핵심 메커니즘 (B안):**
- 착탄 시 AoE 범위 내 모든 적에게 동일 피해 적용
- 크리티컬 발생 시 범위 내 모든 적에게 감전(ApplyStun) 확정 적용
- 크리티컬 미발생 시 감전 없음

**영향 시스템:**
- `SkillData.cs` — SkillType에 LightningArrow 추가
- `SkillDispatcher.cs` — LightningArrow 분기 추가
- `TestRunner.cs` — Lightning Arrow 버튼 UI 추가

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `LightningArrow = 6` 추가 |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `LightningArrow` 분기 추가 |
| `Assets/Scripts/TestRunner.cs` | `lightningArrowSkill` 필드, Lightning Arrow 버튼 UI |

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Gameplay/Skills/Projectiles/LightningArrowProjectile.cs` | `ProjectileBase` 상속. AoE 번개 피해, 크리티컬 시 감전 확정 |

### 클래스 인터페이스

```csharp
public class LightningArrowProjectile : ProjectileBase
{
    public float AoeRadius      { get; set; }
    public float ShockDuration  { get; set; }  // 감전 지속시간
    public float CritChance     { get; set; }
    public float CritDamage     { get; set; }
    protected override void OnHit(Enemy target);
    // 크리티컬 판정 → AoE 내 전체 피해 + 크리티컬 시 감전 확정
}
```

---

## 4. 테스트 계획

- [ ] OnGUI에 Lightning Arrow 버튼 표시 확인
- [ ] 버튼 → T키 → 스킬 장착 확인
- [ ] 착탄 시 aoeRadius 내 모든 적 피해 확인
- [ ] 크리티컬 발생 시 범위 내 전체 감전 확인
- [ ] 크리티컬 미발생 시 감전 없음 확인
- [ ] 기존 스킬 동작 간섭 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| SkillType 열거형 값 추가 | 기존 asset 직렬화 영향 없음 | 명시적 int 값 지정 (LightningArrow = 6) |
| AoE 순회 중 Enemy 제거 | ActiveEnemies 변경 가능 | ToArray()로 안전 순회 |
| ScriptableObject asset | AGENTS.md 규칙상 에디터에서 사용자가 직접 생성 | 코드만 작성 |
