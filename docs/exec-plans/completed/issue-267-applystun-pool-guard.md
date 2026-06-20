# Issue #267 — 치명타 후 ApplyStun → 풀 반환된 적에 stun set 되어 재사용 시 stun 상태로 시작

## 1. 시스템 구조

```
Tower.Attack(enemy)
  → SkillDispatcher / Projectile / Splash
       e.TakeDamage(...)
         → Enemy.CurrentHp <= 0 → Die()
              → ObjectPoolSystem.Instance.Return(this)   // 풀로 반환
                  → SetActive(false)
       e.ApplyStun(0.5f)   // ← 이미 풀의 비활성 적!
         → _stunTimer = Mathf.Max(_stunTimer, 0.5f)   // 0.5 set
                                                       ↓
                                              풀에 보존 (Initialize 미초기화)
                                                       ↓
                                          다음 웨이브 재사용 시
                                                       ↓
                                         Enemy.Update: _stunTimer > 0 → 정지
```

**근본 원인:**
- `Enemy._stunTimer` 는 `Initialize` 에서 초기화되지 않음
- `ApplyStun` 은 `Mathf.Max(_stunTimer, duration)` — 풀 반환 시 0 으로 복구되지 않음
- 6 개 호출 위치 모두 `e.TakeDamage` 후 `CurrentHp > 0` 가드 없이 `ApplyStun` 호출

## 2. 수정 파일

| 파일 | 위치 | 수정 내용 |
|------|------|----------|
| `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs` | `Initialize` | `_stunTimer = 0f;` 추가 (근본 원인 차단) |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | line 47 (`DirectAttack`) | `target.CurrentHp > 0f` 가드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | line 71 (`ExecuteFreezingPulse`) | `e.CurrentHp > 0f` 가드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs` | line 193 (splash) | `e.CurrentHp > 0f` 가드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/LightningArrowProjectile.cs` | line 34 | `e.CurrentHp > 0f` 가드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/FreezingPulseProjectile.cs` | line 38 | `target.CurrentHp > 0f` 가드 추가 |
| `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/FireballProjectile.cs` | line 17 | `target.CurrentHp > 0f` 가드 추가 |

## 3. 신규 클래스 / 파일

없음.

## 4. 구현 세부

### Enemy.Initialize 수정

```csharp
public void Initialize(EnemyData data, int stage, Vector2[] waypoints)
{
    Grade = data.grade;
    _waypoints = waypoints;
    _waypointIndex = 0;
    _playerDamage = data.playerDamage;
    _stunTimer = 0f;   // ← 추가

    // ... 기존 로직
}
```

### ApplyStun 호출 가드 패턴 (6곳 공통)

기존:
```csharp
if (StunChance > 0f && Random.value < Mathf.Clamp01(StunChance / 100f))
    target.ApplyStun(0.5f);
```

수정:
```csharp
if (target.CurrentHp > 0f && StunChance > 0f &&
    Random.value < Mathf.Clamp01(StunChance / 100f))
    target.ApplyStun(0.5f);
```

> [[issue-264-molten-strike]] PR #266 의 `SkillDispatcher.cs:177` 가드와 동일 패턴.

특수 케이스:
- `LightningArrowProjectile.cs:34` 는 `if (isCrit)` 단일 조건이므로 `if (isCrit && e.CurrentHp > 0f)` 로 변경
- `FreezingPulseProjectile.cs:38` 도 동일 패턴 변환

## 5. 테스트 계획

- [ ] StunChance 100% 타워 + 약한 적 (한 방 사망) → 다음 웨이브 재사용 시 적이 stun 없이 정상 이동 확인
- [ ] StunChance 100% 타워 + 강한 적 (생존) → 정상 stun 적용 + 만료 후 이동 재개 확인
- [ ] `FreezingPulse` 단일 타깃 사망 케이스 → 풀 재사용 적 정상 동작
- [ ] `Fireball` 단일 타깃 사망 케이스 → 동일
- [ ] `LightningArrow` AoE 루프 — 즉사한 적 풀 반환 후 재사용 시 정상 동작
- [ ] `ExecuteFreezingPulse` AoE 루프 동일
- [ ] `ProjectileBase.ApplySplash` 의 splash 즉사 — 풀 재사용 회귀 없음
- [ ] `DirectAttack` (스킬 미장착) 사망 케이스
- [ ] Molten Strike PR #266 의 1차 타격 가드는 기존 유지 (회귀 X)

## 6. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| stun 빈도 감소 | "lethal hit 시 stun 미적용" 으로 stun 빈도가 약간 줄어들 수 있음 | 의도된 동작 — 죽은 적에 stun 은 의미 없음. 풀 재사용 버그 차단이 우선 |
| `_stunTimer = 0f` 초기화 누락 | 다른 stun 관련 상태 (ex. burn / dot 등) 도 동일 위험 가능 | 본 이슈는 stun 한정. 다른 상태도 발견 시 별도 이슈 (DoT/Burn 등 별도 검토 필요) |
| 가드 누락 | 향후 새 스킬 추가 시 같은 실수 반복 가능 | `Enemy.ApplyStun` 내부에서 `if (CurrentHp <= 0f) return;` 가드를 두는 방어적 옵션도 있으나, 호출자 의도(은닉)가 흐려지므로 호출자 가드 + Initialize 초기화 조합 채택 |

## 7. 참고

- 발견: PR #266 (Molten Strike) Codex 리뷰 — `SkillDispatcher.cs:177` 가드 추가 답변에서 후속 이슈로 분리
- 관련: [[issue-264-molten-strike]] — 신규 분기는 #266 에서 가드 완료, 본 이슈는 기존 6곳 일괄 정정
