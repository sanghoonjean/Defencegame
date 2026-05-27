# Issue #181 — EnergyDrain 보조 옵션 DoT 구현

## 1. 시스템 구조

- `EnergyDrain` 보조 옵션 장착 시 투사체 명중 → 적에게 DoT(지속 피해) 적용
- `value` = 틱 피해량 비율 (타워 AttackDamage 기준), 고정 지속 3초 / 틱 간격 0.5초
- 같은 적에 중복 명중 시 타이머 갱신(refresh) — 중첩 없음

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/DamageType.cs` — Energy 타입 추가
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs` — ApplyDot 메서드 추가
- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs` — DotDamageRatio 프로퍼티 추가
- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs` — DotTickDamage/DotDuration 추가, OnHit 호출
- `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` — 각 Launch 함수에 주입

## 3. 구현 상세

### DamageType
```csharp
public enum DamageType { Physical, Fire, Energy }
```

### Enemy.ApplyDot
- Coroutine 기반, OnDisable/Initialize에서 정리
- 기존 DoT 있으면 StopCoroutine 후 재시작 (refresh)

### Tower
- `DotDamageRatio`: EnergyDrain value 누산
- `DotDuration`: 고정 3f (EnergyDrain 장착 시 활성)

### ProjectileBase
- `DotTickDamage`, `DotDuration` 프로퍼티
- `OnHit`에서 `target.ApplyDot(DotTickDamage, DotDuration)` 호출
- `ReturnToPool`에서 초기화

## 4. 테스트 계획

- [ ] EnergyDrain 장착 → 명중 후 0.5초 간격 Energy 피해 3초간 확인
- [ ] 중복 명중 시 타이머 갱신, 중첩 없음 확인
- [ ] EnergyDrain 미장착 시 DoT 없음 확인
- [ ] 적 사망/풀 반환 후 DoT 코루틴 정리 확인

## 5. 위험 요소

- 오브젝트 풀 반환 시 코루틴 반드시 정리 필요 (OnDisable에서 처리)
- GameUIManager 색상은 Fire와 구분되도록 Energy 타입 처리 필요
