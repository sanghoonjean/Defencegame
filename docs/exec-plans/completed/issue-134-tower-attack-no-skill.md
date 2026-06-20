# Issue #134 — 스킬 미장착 타워가 사정거리 내 적에게 데미지를 입히는 버그

## 1. 시스템 구조

`Tower.Update()`가 매 프레임 쿨다운을 체크하고 `FindTarget()` → `Attack()` → `SkillDispatcher.Execute()`를 호출한다.
현재 `EquippedSkill == null` 여부를 체크하지 않아 스킬 미장착 상태에서도 공격이 실행된다.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`

## 3. 신규 클래스 / 파일

없음.

## 4. 테스트 계획

- [ ] 스킬 미장착 타워 배치 → 적이 사정거리 진입 → 적 HP 감소 없음 확인
- [ ] 스킬 장착 후 → 적이 사정거리 진입 → 정상 공격 확인
- [ ] 스킬 장착 → 해제 → 다시 공격 안 함 확인

## 5. 위험 요소

없음. `Update()` 최상단에 가드 추가이므로 사이드 이펙트 없음.
