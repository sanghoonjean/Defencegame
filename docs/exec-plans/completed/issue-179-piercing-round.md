# Issue #179 — PiercingRound 보조 옵션 관통 구현

## 1. 시스템 구조

- `PiercingRound` 장착 시 투사체가 적 명중 후 소멸하지 않고 이동 방향 전방의 미타격 적으로 계속 진행
- Chain과 차이: 방향 제약 있음 (전방 반구, dot product > 0)
- `value * 5` → 관통 횟수 (예: value=0.4 → 2회)

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs`

## 3. 구현 상세

### Tower
- `PierceCount` 프로퍼티 추가
- `AccumulateSupportOption`: `PiercingRound → PierceCount += Max(1, RoundToInt(value * 5))`

### ProjectileBase
- `PierceCount` 프로퍼티 추가
- 명중 시 `PierceCount > 0`이면 `TryPierce()` 호출
- `TryPierce()`: 마지막 이동 방향 벡터 기준 전방(dot > 0) 미타격 적 중 최근접 → re-target
- `ReturnToPool`에서 `PierceCount = 0` 초기화

## 4. 테스트 계획

- [ ] PiercingRound 장착 → 전방 적 관통 확인 (value=0.4 → 2회)
- [ ] 이동 방향 후방 적은 관통 대상 아님 확인
- [ ] 관통 횟수 초과 후 소멸 확인
- [ ] 미장착 시 기존 동작 유지 확인

## 5. 위험 요소

- 전방 적이 없으면 즉시 소멸 (ChainCount와 동일)
- Fireball 등 스플래시 스킬과 함께 쓰면 각 타겟마다 스플래시 발생 → 강력할 수 있음
