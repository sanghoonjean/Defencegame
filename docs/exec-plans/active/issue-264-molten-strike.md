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

MagmaProjectile : ProjectileBase  // 풀 호환을 위해 상속
  → base.Launch 호출하지 않음 (적 추적 미사용) → _launched = false 유지
     → base.Update 는 early-return 가드 → 자체 Update 오버라이드로 비행 제어
  → override Update():
      포물선 보간 (시작점→낙하점, lifetime 기반 t)
      착지 시 OnLand():
        ExplosionRadius 범위 내 적에게 PHY+FIRE 분산 피해
        피해 = base * (1 - lessHitRatio)  // less 60% → ×0.4
        AoeFx 표시 → ReturnToPool()
```

**핵심 설계:**
- **1차 근접 타격**: `SkillDispatcher.ExecuteMoltenStrike`에서 즉시 처리 (target 단일)
- **2차 마그마 투사체**: `MagmaProjectile`이 `ProjectileBase` 상속 — `ObjectPoolSystem.GetProjectile<T>() where T : ProjectileBase` 호환. 적 추적 비행 대신 자체 포물선 비행을 사용하므로 `base.Launch` 미호출 + `Update` 오버라이드
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
| `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs` | `Update()` `private` → `protected virtual`, `ReturnToPool()` `private` → `protected` (서브클래스의 자체 비행/풀 반환을 허용). 기존 파생 클래스 동작은 미변경 |
| `MakeDefence/Assets/Scripts/TestRunner.cs` | `moltenStrikeSkill` 필드, "Molten Strike" 버튼 UI |

### 신규 에셋 (UnityMCP)

| 에셋 | 용도 |
|------|------|
| `MakeDefence/Assets/Prefabs/Projectiles/MagmaProjectile.prefab` | `MagmaProjectile` 컴포넌트 + Sprite 가진 풀링용 프리팹. **`ObjectPoolSystem.projectilePrefabs` 배열에 반드시 추가 등록** — `Awake` 가 이 배열만으로 `_projectilePrefabMap` 을 빌드하므로 미등록 시 `GetProjectile<MagmaProjectile>()` 가 `[ObjectPoolSystem] 프리팹 미등록` 로그와 함께 null 반환 |
| `MakeDefence/Assets/Data/Skills/MoltenStrike.asset` | `SkillData` ScriptableObject. `skillType = MoltenStrike`, baseDamage=120, aoeRadius(근접 거리), explosionRadius=9, projectileRadius=2, projectileCount=4, physToFireRatio=0.6, projectileLessHitRatio=0.6 |

> 두 에셋 모두 [[feedback_unity_asset_edits]] (AGENTS.md §7) 에 따라 UnityMCP 로 생성/편집한다.
> 코드 측은 `proj == null` 가드로 프리팹 미등록 시 1차 타격만 발생하고 투사체 단계는 안전하게 스킵 (다른 스킬과 동일 패턴).

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/MagmaProjectile.cs` | `ProjectileBase` 상속 (풀 호환). 포물선 비행 + 낙하 폭발 — `base.Launch` 미호출, `Update`/`OnHit` 오버라이드. 폭발 시 `ExplosionRadius` 범위 적에 PHY+FIRE 분산 피해 |

### 클래스 인터페이스

```csharp
// MagmaProjectile.cs
public class MagmaProjectile : ProjectileBase
{
    [SerializeField] private GameObject _aoeFxPrefab;

    private const float FlightDuration = 0.5f;
    private const float ArcHeight      = 1.5f;

    // 자체 비행 상태 — base._launched 는 false 유지
    private bool    _arcLaunched;
    private float   _arcElapsed;
    private Vector2 _arcOrigin;
    private Vector2 _arcLand;

    public float ExplosionRadius     { get; set; }   // 9
    public float ProjectileRadius    { get; set; }   // 2 (시각/충돌 반경)
    public float BasePhysDamage      { get; set; }   // 전환 후 잔여 물리
    public float BaseFireDamage      { get; set; }   // 전환된 화염
    public float NewArmorPen         { get; set; }
    public float ProjectileLessRatio { get; set; }   // 0.6 → ×0.4

    // 풀에서 가져온 직후 호출 (base.Launch 대신)
    public void LaunchArc(Vector2 origin, Vector2 landPos);

    protected override void Update();   // 포물선 보간, t>=1 시 OnLand
    private void OnLand();               // ExplosionRadius 범위 처리 → ReturnToPool()
}
```

### ProjectileBase 변경

```csharp
// 기존 private → protected 로 노출 (기능 변경 없음)
protected bool _launched;
protected virtual void Update() { /* 기존 본문 그대로 */ }
protected void ReturnToPool()   { /* 기존 본문 그대로 */ }
```

> 기존 파생 클래스(`FireballProjectile`, `CausticArrowProjectile`, `LightningArrowProjectile`,
> `FreezingPulseProjectile`)는 `Update`/`ReturnToPool` 을 재정의하지 않으므로 base 동작 그대로 유지 — 회귀 없음.

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
- [ ] `MagmaProjectile.prefab` 미등록 상태 → `GetProjectile` null 반환 시 1차 타격만 발생하고 콘솔 에러 1회 로깅, 게임 진행에 차단 없음 확인
- [ ] `MagmaProjectile.prefab` 등록 후 → 4발 정상 발사 + 폭발 확인 (등록 전후 회귀 차이)

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| 낙하 타깃 결정 로직 | 4발 동일 지점 모이면 효과 약화 | 부채꼴 spread(±30°) + 거리 `Random.Range(minDist, maxDist)` |
| `MagmaProjectile` 풀 호환 | `ObjectPoolSystem.GetProjectile<T>() where T : ProjectileBase` 제약 | `ProjectileBase` 상속 + `_launched`/`Update`/`ReturnToPool` 접근성 완화로 해결 (Codex P2 피드백 반영) |
| `MagmaProjectile` 풀 압박 | 1 타격당 4발 → 다타워 환경에서 풀 고갈 | `ObjectPoolSystem` 초기 풀 사이즈 점검 / 부족 시 증설 |
| 피해 표시 가시성 | `Enemy.TakeDamage` 가 호출마다 `GameUIManager.ShowDamage` 를 호출하므로 PHY/FIRE 분리 호출 시 팝업이 분리 표시됨 | **본 이슈 범위에서는 분리 표시 허용** — 기존 `Fireball` + `IncendiaryRound` 조합도 동일 패턴 (본체 Fire 팝업 + Added Fire 팝업). 단일 합산 팝업이 필요하면 `Enemy.TakeDamageMulti(...)` 같은 별도 API 가 필요하므로 별도 이슈로 분리 |
| `_aoeFxPrefab` 미연결 | 인스펙터 미연결 시 폭발 시각 없음 | null 가드 + `GameUIManager.ShowAoeHit` fallback |
| `MagmaProjectile.prefab` 미등록 | `ObjectPoolSystem.projectilePrefabs` 누락 시 `GetProjectile<MagmaProjectile>()` null 반환 | 1) UnityMCP 로 프리팹 생성 후 인스펙터 배열 등록, 2) Dispatcher 의 `proj == null` 가드로 안전 폴백 (1차 타격만 발생) |
| `BrutalitySupport` 호환 | `damageNature = Fire` 미설정 시 차단 누락 | `SkillData` 에셋에 `damageNature = Fire` 명시 (UnityMCP) |
| 1차 phys/fire 전환 적용 누락 | Dispatcher는 합산, Projectile은 분할 시 비대칭 발생 | 양쪽 모두 `physToFireRatio` 동일 분할 |
| ScriptableObject 에셋 (`MoltenStrike.asset`) | AGENTS.md §7 따라 [[feedback_unity_asset_edits]] | UnityMCP 로만 생성/편집, 코드는 enum/필드만 |

---

## 6. 참고

- 베이스: Path of Exile — Molten Strike (Gem)
- 분류: Attack / Projectile / AoE / Melee / Strike / Fire
- 관련 플랜: [[issue-114-splash-fire-damage]] (스플래시 화염 적용), [[issue-109-added-fire-damage]] (IncendiaryRound 합산), [[issue-111-brutality-support]] (damageNature 분류)
