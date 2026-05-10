# Issue #40 — Caustic Arrow 스킬 구현

## 1. 시스템 구조

```
[TestRunner OnGUI]
  → GUI.Button("Caustic Arrow") → _selectedSkill = causticArrowSkill

Tower.Attack()
  → SkillDispatcher.Execute(tower, target)
        [SkillType.CausticArrow]
          → ObjectPoolSystem.GetProjectile<CausticArrowProjectile>()
          → CausticArrowProjectile.Launch(origin, target, ...)
                → 단일 타겟 추적
                → 착탄 시 OnHit():
                    Instantiate(causticGroundPrefab, hitPos)
                    CausticGround.Init(radius, tickDamage, armorPen, tickInterval, duration)

CausticGround (MonoBehaviour, 독립 생존)
  → Update():
      _lifeTimer 증가 → duration 초과 시 Destroy
      _tickTimer 증가 → tickInterval 마다 ApplyDot()
  → ApplyDot():
      radius 내 모든 Enemy.ActiveEnemies 에 TakeDamage(tickDamage, armorPen)
```

**핵심 설계:**
- `CausticArrowProjectile`: 착탄 시 `causticGroundPrefab`을 Instantiate
- `CausticGround`: 자체 Update로 DoT 틱 관리, duration 후 Destroy(gameObject)
- 장판 피해는 `skill.baseDamage` 기반 틱 데미지, `skill.dotDuration` 지속

**영향 시스템:**
- `SkillData.cs` — SkillType에 CausticArrow 추가
- `SkillDispatcher.cs` — CausticArrow 분기 추가
- `TestRunner.cs` — causticArrowSkill 필드, Caustic Arrow 버튼 추가

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `CausticArrow = 7` 추가 |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `CausticArrow` 분기 추가 |
| `Assets/Scripts/TestRunner.cs` | `causticArrowSkill` 필드, Caustic Arrow 버튼 UI |

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Gameplay/Skills/Projectiles/CausticArrowProjectile.cs` | ProjectileBase 상속. 착탄 시 causticGroundPrefab Instantiate |
| `Assets/Scripts/Gameplay/Skills/CausticGround.cs` | 독 장판 MonoBehaviour. 틱 DoT, duration 후 자기소멸 |

### 클래스 인터페이스

```csharp
// CausticArrowProjectile.cs
public class CausticArrowProjectile : ProjectileBase
{
    [SerializeField] private GameObject causticGroundPrefab;
    public float AoeRadius    { get; set; }
    public float DotDuration  { get; set; }
    public float TickDamage   { get; set; }
    public float TickInterval { get; set; }
    protected override void OnHit(Enemy target);
}

// CausticGround.cs
public class CausticGround : MonoBehaviour
{
    public void Init(float radius, float tickDamage, float armorPen,
                     float tickInterval, float duration);
}
```

---

## 4. 테스트 계획

- [ ] Caustic Arrow 버튼 → T키 → 스킬 장착 확인
- [ ] 투사체 착탄 시 장판 생성 확인 (SpriteRenderer로 시각 확인)
- [ ] 장판 범위 내 적에게 틱 데미지 적용 확인
- [ ] 장판 범위 밖 적에게 피해 없음 확인
- [ ] dotDuration 경과 후 장판 소멸 확인
- [ ] 기존 스킬 동작 간섭 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| causticGroundPrefab 미연결 | Inspector에서 미연결 시 장판 미생성 | null 체크 + LogError |
| CausticGround 누적 | 장판 다수 생성 시 성능 저하 가능 | dotDuration으로 자동 소멸 |
| ActiveEnemies 변경 중 순회 | 장판 DoT 틱 중 적 제거 가능 | ToArray()로 안전 순회 |
| ScriptableObject asset | AGENTS.md 규칙상 에디터에서 사용자가 직접 생성 | 코드만 작성 |
