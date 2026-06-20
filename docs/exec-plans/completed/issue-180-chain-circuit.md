# Issue #180 — ChainCircuit 보조 옵션 연쇄 타격 구현

## 1. 시스템 구조

- `ChainCircuit` 장착 시 투사체가 첫 타겟 명중 후 가장 가까운 미타격 적으로 재발사
- `value * 10` → 연쇄 횟수 (예: value=0.3 → 3회)
- 이미 타격한 적 목록(`_hitEnemies`)을 투사체가 보관해 중복 타격 방지

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs`

## 3. 구현 상세

### Tower
- `ChainCount` 프로퍼티 추가
- `AccumulateSupportOption`에서 `ChainCircuit: ChainCount += Mathf.RoundToInt(opt.value * 10)`

### ProjectileBase
- `ChainCount` 프로퍼티 추가
- `_hitEnemies` HashSet으로 타격 이력 관리
- `OnHit` 이후 `ChainCount > 0`이면 가장 가까운 미타격 적 탐색 → `ChainCount - 1`로 재발사
- `ReturnToPool`에서 `_hitEnemies` Clear, `ChainCount = 0`

## 4. 테스트 계획

- [ ] ChainCircuit 장착 → 첫 타겟 명중 후 인접 적으로 연쇄 확인
- [ ] 연쇄 횟수 소진 후 소멸 확인
- [ ] 같은 적 중복 타격 없음 확인
- [ ] 미장착 시 기존 동작 유지 확인

## 5. 위험 요소

- 연쇄 중 적이 풀에 반환되면 null 체크 필수
- 범위 제한 없이 맵 전체 탐색 → 적이 많을 경우 성능 주의 (HashSet으로 최적화)
