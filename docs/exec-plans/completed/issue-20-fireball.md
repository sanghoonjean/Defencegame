# Issue #20 — 화염구 (Fireball) 공격 스킬 구현

## 1. 시스템 구조

```
Tower.Update()
  → FindTarget() : 범위 내 최근접 적 탐색
  → Attack(target)
      → SkillDispatcher.Execute(tower, target)   ← 신규
            [SkillType.Fireball]
              → ObjectPoolSystem.Get<FireballProjectile>()
              → FireballProjectile.Launch(origin, target, towerStats)
                    → 이동 중 target 추적
                    → 착탄 시 AoE 폭발
                          → Enemy.ActiveEnemies 순회
                          → aoeRadius 내 모든 적 TakeDamage()
```

**영향 시스템:**
- `Tower` — Attack 분기 추가
- `SkillData` — SkillType enum 판타지 이름으로 교체
- `ObjectPoolSystem` — 제네릭 풀 또는 투사체 전용 풀 추가

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `SkillType` enum을 판타지 이름으로 교체 (SingleLaser→PreciseArrow, Missile→Fireball, EMP→ParalysisMagic, Railgun→LightningSpear, Nanobot→PoisonCloud) |
| `Assets/Scripts/Gameplay/Tower/Tower.cs` | `Attack()` 내부에 `SkillDispatcher.Execute()` 호출로 교체 |
| `Assets/Scripts/Systems/ObjectPoolSystem.cs` | 투사체(ProjectileBase) 풀 지원 추가 |

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | SkillType에 따라 공격 방식 분기 (Strategy 패턴). 단일 공격 폴백 포함 |
| `Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs` | 투사체 공통 기반. 이동·명중·ObjectPool 반납 처리. `MonoBehaviour` |
| `Assets/Scripts/Gameplay/Skills/Projectiles/FireballProjectile.cs` | `ProjectileBase` 상속. 착탄 시 `aoeRadius` 범위 내 모든 적에 AoE 데미지 |

### 클래스 인터페이스 요약

```csharp
// SkillDispatcher.cs
public static class SkillDispatcher
{
    public static void Execute(Tower tower, Enemy target);
}

// ProjectileBase.cs
public class ProjectileBase : MonoBehaviour
{
    public void Launch(Vector2 origin, Enemy target, float damage, float armorPen);
    protected virtual void OnHit(Enemy target);   // 하위 클래스 오버라이드
}

// FireballProjectile.cs
public class FireballProjectile : ProjectileBase
{
    public float AoeRadius { get; set; }
    protected override void OnHit(Enemy target);  // AoE 폭발
}
```

---

## 4. 테스트 계획

- [ ] `FireballSkill` ScriptableObject 생성 (baseDamage=30, aoeRadius=2, baseCooldown=1.5)
- [ ] 타워에 화염구 스킬 장착 후 에디터 Play
- [ ] 단일 적 명중 시 데미지 적용 확인
- [ ] AoE 반경 내 다수 적 동시 피해 확인
- [ ] AoE 반경 밖 적은 피해 없음 확인
- [ ] 투사체 명중 후 ObjectPool로 정상 반납 확인 (메모리 누수 없음)
- [ ] 기존 스킬 없는 타워가 여전히 단일 직접 공격 동작 확인 (폴백)

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| SkillType enum 이름 변경 | 기존 `.asset` ScriptableObject 직렬화 키가 깨질 수 있음 | enum 값에 명시적 정수 할당 후 기존 에셋 재확인 |
| AoE 순회 중 적 사망 | `Enemy.ActiveEnemies` 순회 중 `Die()` 호출 → 리스트 변경 예외 | 순회 전 `ToArray()` 복사본으로 처리 |
| ObjectPool 투사체 미지원 | 현재 Enemy 전용 — 투사체 반납 불가 | 풀을 제네릭화하거나 투사체 전용 풀 별도 추가 |
| Tower.Attack() 리팩터링 | 기존 단일 공격 동작 깨질 위험 | SkillData == null 시 기존 직접 공격 폴백 유지 |
