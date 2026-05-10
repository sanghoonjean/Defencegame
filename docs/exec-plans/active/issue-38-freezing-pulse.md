# Issue #38 — Freezing Pulse 공격 스킬 구현

## 1. 시스템 구조

```
[TestRunner OnGUI]
  → GUI.Button("Freezing Pulse") → _selectedSkill = freezingPulseSkill

[T키]
  → InventorySystem.EquipSkill(_selectedSkill)

Tower.Attack()
  → SkillDispatcher.Execute(tower, target)
        [SkillType.FreezingPulse]
          → ObjectPoolSystem.GetProjectile<FreezingPulseProjectile>()
          → FreezingPulseProjectile.Launch(origin, target, damage, armorPen)
                → 단일 타겟 추적
                → 착탄 시 OnHit() — 근거리 보너스 데미지 + 짧은 빙결(스턴)
```

**근거리 보너스 메커니즘:**
- 발사 시점 origin 저장
- OnHit() 시 이동 거리 계산
- 거리가 짧을수록 데미지 배율 상승 (최대 2배, 최소 1배)
- 착탄 시 짧은 빙결(ApplyStun) 적용

**영향 시스템:**
- `SkillData.cs` — SkillType 열거형에 FreezingPulse 추가
- `SkillDispatcher` — FreezingPulse 분기 추가
- `TestRunner` — Freezing Pulse 버튼 UI 추가

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Tower/SkillData.cs` | `FreezingPulse = 5` 추가 |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `FreezingPulse` 분기 추가 |
| `Assets/Scripts/TestRunner.cs` | `freezingPulseSkill` 필드, Freezing Pulse 버튼 UI |

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Gameplay/Skills/Projectiles/FreezingPulseProjectile.cs` | `ProjectileBase` 상속. 근거리 보너스 데미지, 착탄 시 빙결 |

### 클래스 인터페이스

```csharp
// FreezingPulseProjectile.cs
public class FreezingPulseProjectile : ProjectileBase
{
    public float MaxRangeBonus { get; set; }  // 근거리 최대 배율 (예: 2.0)
    public float FreezeDuration { get; set; } // 빙결 지속시간
    protected override void OnHit(Enemy target);
    // 근거리일수록 데미지 증가: multiplier = lerp(1, MaxRangeBonus, 1 - dist/maxRange)
}
```

---

## 4. 테스트 계획

- [ ] TestRunner OnGUI에 Freezing Pulse 버튼 표시 확인
- [ ] Freezing Pulse 버튼 클릭 → T키 → 스킬 장착 확인
- [ ] 근거리 적: 높은 데미지 적용 확인
- [ ] 원거리 적: 낮은 데미지 적용 확인
- [ ] 착탄 시 빙결(이동 정지) 적용 확인
- [ ] 기존 Fireball / 정밀 화살 동작 간섭 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| SkillType 열거형 값 추가 | 기존 asset 직렬화 영향 없음 (새 값 추가) | 명시적 int 값 지정 (FreezingPulse = 5) |
| 거리 계산 기준 | Tower 위치 vs 투사체 발사 위치 차이 | Launch() 시 origin을 저장해 OnHit에서 비교 |
| .meta 파일 | AGENTS.md 규칙상 수동 생성 금지 | Unity 에디터에서 자동 생성되도록 방치 |
| ScriptableObject asset | AGENTS.md 규칙상 에디터에서 사용자가 직접 생성 | 코드만 작성, asset은 사용자 생성 |
