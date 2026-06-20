# Issue #264 — Molten Strike 공격 스킬 구현

## 1. 시스템 구조

Path of Exile의 **Molten Strike** 메커니즘 — 근접 1차 타격 + 4개 마그마 투사체 분산 발사 + 낙하 지점 폭발의 하이브리드 스킬.

```
Tower.Attack()
  → SkillDispatcher.Execute(tower, target)
        [SkillType.MoltenStrike]
        ┌───────────────────────────────────────────────────┐
        │ ① 1차 근접 타격 (단일 타깃)                      │
        │   baseDmg = AttackDamage + skill.baseDamage      │
        │   phys = baseDmg * (1 - physToFireRatio)         │
        │   fire = baseDmg * physToFireRatio               │
        │   target.TakeDamage(phys, ..., Physical)         │
        │   target.TakeDamage(fire, 0, ..., Fire)          │
        │   + IncendiaryRound(AddedFireRatio) 추가 화염    │
        └───────────────────────────────────────────────────┘
        ┌───────────────────────────────────────────────────┐
        │ ② 마그마 투사체 4개 풀에서 인스턴스 가져옴       │
        │   forward = (target - tower).normalized          │
        │   for i in 0..projectileCount:                   │
        │     angle = spread * (i - (n-1)/2) / (n-1)       │
        │     landPos = hitPos + rotate(forward, angle)    │
        │             * Random.Range(minDist, maxDist)     │
        │     proj.Launch(hitPos, landPos, arcHeight)      │
        └───────────────────────────────────────────────────┘

MagmaProjectile (MonoBehaviour, ProjectileBase 미상속)
  → Update():
      포물선 보간 (시작점→낙하점, lifetime 기반 t)
      착지 시 OnLand():
        ExplosionRadius 범위 내 적에게 PHY+FIRE 분산 피해
        피해 = base * (1 - lessHitRatio)  // less 60% → ×0.4
        AoeFx 표시 → 풀로 반환
```

**핵심 설계:**
- **1차 근접 타격**: `SkillDispatcher.ExecuteMoltenStrike`에서 즉시 처리 (target 단일)
- **2차 마그마 투사체**: `MagmaProjectile`이 포물선 비행 후 낙하점에서 폭발
- **피해 전환**: `SkillData.physToFireRatio = 0.6` → phys 40% / fire 60% 분할 (Dispatcher와 Projectile 양쪽 적용)
- **less 감폭**: `projectileLessHitRatio = 0.6` → 투사체 피해 × 0.4
- **분산 발사**: `projectileCount = 4`, 부채꼴 spread + 낙하 거리 랜덤화로 위치 분산

**영향 시스템:**
- `SkillData.cs` — `MoltenStrike` enum 값 + 신규 필드(`projectileCount`, `explosionRadius`, `projectileRadius`, `physToFireRatio`, `projectileLessHitRatio`)
- `SkillDispatcher.cs` — `MoltenStrike` 분기 (`ExecuteMoltenStrike`)
- `TestRunner.cs` — `moltenStrikeSkill` 필드 + 버튼 추가

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `MakeDefence/Assets/Scripts/Gameplay/Tower/SkillData.cs` | `SkillType.MoltenStrike = 8` 추가, `projectileCount`/`explosionRadius`/`projectileRadius`/`physToFireRatio`/`projectileLessHitRatio` 필드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `MoltenStrike` 분기 (`ExecuteMoltenStrike`) — 1차 타격 + 4개 마그마 투사체 발사 |
| `MakeDefence/Assets/Scripts/TestRunner.cs` | `moltenStrikeSkill` 필드, "Molten Strike" 버튼 UI |

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/MagmaProjectile.cs` | 포물선 비행 + 낙하 폭발. `ProjectileBase` 미상속 (전용 비행/낙하 로직). 폭발 시 `ExplosionRadius` 범위 적에 PHY+FIRE 분산 피해 |

### 클래스 인터페이스

```csharp
// MagmaProjectile.cs
public class MagmaProjectile : MonoBehaviour
{
    [SerializeField] private GameObject _aoeFxPrefab;

    private const float FlightDuration = 0.5f;
    private const float ArcHeight      = 1.5f;

    public float ExplosionRadius     { get; set; }   // 9
    public float ProjectileRadius    { get; set; }   // 2 (시각/충돌 반경)
    public float BasePhysDamage      { get; set; }   // 전환 후 잔여 물리
    public float BaseFireDamage      { get; set; }   // 전환된 화염
    public float ArmorPen            { get; set; }
    public float AddedFireRatio      { get; set; }   // IncendiaryRound 추가 화염
    public float IgniteChance        { get; set; }
    public float ProjectileLessRatio { get; set; }   // 0.6 → ×0.4

    public void Launch(Vector2 origin, Vector2 landPos);
    private void OnLand();   // 폭발 범위 처리
}
```

### SkillData 추가 필드

```csharp
[Header("Molten Strike 전용")]
public int   projectileCount        = 4;
public float explosionRadius        = 9f;
public float projectileRadius       = 2f;
public float physToFireRatio        = 0.6f;
public float projectileLessHitRatio = 0.6f;
```

> `aoeRadius` 는 1차 근접 타격 거리(=AttackRange 한도)로 사용,
> `explosionRadius` 는 마그마 폭발 전용으로 분리.

---

## 4. 테스트 계획

- [ ] `MoltenStrike` 버튼 → T키 → 스킬 장착 확인
- [ ] 근접 1차 타격 시 단일 타깃 PHY+FIRE 합산 피해 확인 (baseDmg=120 → phys 48 + fire 72)
- [ ] 1차 타격 명중 시 마그마 구체 4개 발사 + 낙하 + 폭발 시각 확인
- [ ] 4개 투사체가 서로 다른 지점에 분산 낙하 확인 (부채꼴 + 거리 랜덤)
- [ ] 폭발 범위 9 내 적에 PHY+FIRE 분산 피해 적용 확인
- [ ] 폭발 피해가 1차 타격 대비 약 40% (×(1 − 0.6) less) 수준인지 확인
- [ ] 화염 저항 / 물리 저항 적에 대해 비율 정상 동작 확인
- [ ] `IncendiaryRound`(`AddedFireRatio`) 장착 시 추가 화염 합산 확인
- [ ] `BrutalitySupport`([[issue-111-brutality-support]]) + Molten Strike → 발사 차단 (damageNature = Fire)
- [ ] 4발/타격 × 다타워 환경에서 풀 고갈 / 프레임 드랍 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| 낙하 타깃 결정 로직 | 4발 동일 지점 모이면 효과 약화 | 부채꼴 spread(±30°) + 거리 `Random.Range(minDist, maxDist)` |
| `MagmaProjectile` 풀 압박 | 1 타격당 4발 → 다타워 환경에서 풀 고갈 | `ObjectPoolSystem` 초기 풀 사이즈 점검 / 부족 시 증설 |
| 피해 표시 가시성 | PHY/FIRE 분리 표시 시 데미지 팝업 중복 | 1차 타격은 합산 단일 팝업, 폭발도 합산 단일 팝업 |
| `_aoeFxPrefab` 미연결 | 인스펙터 미연결 시 폭발 시각 없음 | null 가드 + `GameUIManager.ShowAoeHit` fallback |
| `BrutalitySupport` 호환 | `damageNature = Fire` 미설정 시 차단 누락 | `SkillData` 에셋에 `damageNature = Fire` 명시 (UnityMCP) |
| 1차 phys/fire 전환 적용 누락 | Dispatcher는 합산, Projectile은 분할 시 비대칭 발생 | 양쪽 모두 `physToFireRatio` 동일 분할 |
| ScriptableObject 에셋 (`MoltenStrike.asset`) | AGENTS.md §7 따라 [[feedback_unity_asset_edits]] | UnityMCP 로만 생성/편집, 코드는 enum/필드만 |

---

## 6. 참고

- 베이스: Path of Exile — Molten Strike (Gem)
- 분류: Attack / Projectile / AoE / Melee / Strike / Fire
- 관련 플랜: [[issue-114-splash-fire-damage]] (스플래시 화염 적용), [[issue-109-added-fire-damage]] (IncendiaryRound 합산), [[issue-111-brutality-support]] (damageNature 분류)
