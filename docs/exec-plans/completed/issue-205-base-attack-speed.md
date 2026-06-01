# Issue #205 — Tower.baseAttackCooldown → baseAttackSpeed 역할 분리 및 애니메이션 공식 수정

## 1. 시스템 구조

`Tower.cs`의 `RefreshStats()` 내에서 `Animator.speed` 계산 로직이 수정된다.

- **변경 전**: `baseAttackCooldown`이 공격 쿨타임 계산(스킬 없을 때)과 애니메이션 speed 분자로 이중 사용됨
- **변경 후**: `baseAttackSpeed`는 오직 애니메이션 speed 계산의 분자로만 사용, `SkillData.baseCooldown`은 오직 실제 쿨타임 결정에만 사용

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs` | 필드 이름 변경, `Animator.speed` 공식 단순화 |

## 3. 신규 클래스 / 파일

없음.

## 4. 테스트 계획

- [ ] Unity 에디터에서 타워에 스킬 장착 후 Inspector에서 `baseAttackSpeed` 값 확인
- [ ] `baseAttackSpeed=4`, `baseCooldown=2` → Animator.speed가 2.0인지 확인
- [ ] `baseAttackSpeed=4`, `baseCooldown=4` → Animator.speed가 1.0인지 확인
- [ ] `baseAttackSpeed=4`, `baseCooldown=8` → Animator.speed가 0.5인지 확인
- [ ] 스킬 미장착 시 쿨타임 계산이 `baseAttackSpeed` 기반으로 정상 동작하는지 확인

## 5. 위험 요소

- **SerializedField 이름 변경으로 인한 Inspector 값 초기화**: 기존 prefab/씬에 저장된 `baseAttackCooldown` 값이 사라지고 기본값(1f)으로 리셋됨. Unity 에디터에서 해당 Tower prefab의 `baseAttackSpeed` 값을 재설정해야 함.
