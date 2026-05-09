# Issue #35 — 정밀 화살 스킬 구현 & 스킬 선택 버튼 추가

## 1. 시스템 구조

```
[TestRunner OnGUI]
  → GUI.Button("Fireball")    → _selectedSkill = fireballSkill
  → GUI.Button("정밀 화살")   → _selectedSkill = preciseArrowSkill

[T키]
  → InventorySystem.EquipSkill(_selectedSkill)

Tower.Attack()
  → SkillDispatcher.Execute(tower, target)
        [SkillType.PreciseArrow]
          → ObjectPoolSystem.GetProjectile<PreciseArrowProjectile>()
          → PreciseArrowProjectile.Launch(origin, target, damage, armorPen)
                → 단일 타겟 추적
                → 착탄 시 OnHit() — 단일 데미지 + 크리티컬 보너스
```

**영향 시스템:**
- `SkillDispatcher` — PreciseArrow 분기 추가
- `ObjectPoolSystem` — PreciseArrowProjectile 프리팹 등록 (에디터 작업)
- `TestRunner` — 스킬 선택 버튼 UI + `_selectedSkill` 필드

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `PreciseArrow` 분기 추가 |
| `Assets/Scripts/TestRunner.cs` | `_selectedSkill` 필드, 스킬 선택 버튼 UI, T키 로직 변경 |

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Gameplay/Skills/Projectiles/PreciseArrowProjectile.cs` | `ProjectileBase` 상속. 단일 타겟 고데미지. 크리티컬 확률 추가 보너스 적용 |
| `Assets/Perfab/PreciseArrowSkill.asset` | ScriptableObject (baseDamage=50, baseCooldown=2, baseRange=8) |

### 클래스 인터페이스

```csharp
// PreciseArrowProjectile.cs
public class PreciseArrowProjectile : ProjectileBase
{
    public float BonusCritChance { get; set; }
    protected override void OnHit(Enemy target);  // 크리티컬 보너스 포함 단일 데미지
}
```

---

## 4. 테스트 계획

- [ ] TestRunner OnGUI에 Fireball / 정밀 화살 버튼 표시 확인
- [ ] Fireball 버튼 클릭 → T키 → Fireball 스킬 장착 확인
- [ ] 정밀 화살 버튼 클릭 → T키 → 정밀 화살 스킬 장착 확인
- [ ] 정밀 화살 장착 타워가 단일 적에게 투사체 발사 확인
- [ ] Fireball과 정밀 화살 동작 상호 간섭 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| TestRunner `fireballSkill` 필드 제거 | 기존 Inspector 연결 초기화 | `_selectedSkill` 기본값을 fireballSkill로 설정 |
| PreciseArrow 프리팹 미등록 | ObjectPoolSystem에 프리팹 미등록 시 DirectAttack 폴백 | 에디터에서 프리팹 생성 후 등록 필요 |
| OnGUI 버튼과 타워 클릭 중복 | 버튼 클릭 시 마우스 이벤트가 타워 선택으로도 전달될 수 있음 | `GUIUtility.hotControl` 또는 버튼 위치 조정으로 방지 |
