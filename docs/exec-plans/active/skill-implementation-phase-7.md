# 스킬 구현 계획: Phase 7 — 지뢰 + 오라 (Mine + Aura)

## 목표
지뢰 계열 2종과 오라 버프 계열 8종을 구현한다.
연쇄 기폭 시스템과 타워 버프 오라 관리 시스템을 확립한다.

## 선행 조건
- Phase 6 PlaceableObject, TrapBase 완료
- TowerSystem 버프/디버프 스택 처리 가능

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 화염파편 지뢰 (Pyroclast Mine) | Mine/Fire | Mine fires fiery projectiles/explosions | 연속 폭발 지뢰 후보 |
| 2 | 폭풍 점사 지뢰 (Stormblast Mine) | Mine/Lightning | Mine applies lightning damage/bonus theme | 번개 증폭 지뢰 후보 |
| 3 | 분노 (Anger) | Aura/Fire | Adds fire damage aura | 화염 버프 타워/오라 후보 |
| 4 | 진노 (Wrath) | Aura/Lightning | Adds lightning damage aura | 번개 버프 타워/오라 후보 |
| 5 | 증오 (Hatred) | Aura/Cold | Adds cold damage based on physical theme | 냉기/물리 변환 버프 후보 |
| 6 | 은총 (Grace) | Aura | Evasion aura | 회피/생존 버프 후보 |
| 7 | 결의 (Determination) | Aura | Armour aura | 방어 버프 후보 |
| 8 | 단련 (Discipline) | Aura | Energy shield aura | 보호막 버프 후보 |
| 9 | 가속 (Haste) | Aura | Speed aura | 공속/이속 버프 후보 |
| 10 | 원소의 순수함 (Purity of Elements) | Aura | Elemental resistance/ailment protection aura | 상태 저항/보호 버프 후보 |

---

## 공통 컴포넌트

### MineBase (TrapBase 상속)
- [ ] 적 접근 또는 플레이어 수동 기폭으로 폭발
- [ ] 연쇄 기폭: 인접 지뢰 순차 폭발 처리
- [ ] 기폭 후 효과 발동 + 오브젝트 풀 반납

### AuraBase
- [ ] 타워에 장착 시 주변 일정 범위 내 모든 타워에 버프 적용
- [ ] 버프 종류(공격력, 공격속도, 방어력, 속성 저항 등) ScriptableObject 설정
- [ ] 오라 보유 타워가 제거되면 버프 즉시 해제

---

## 스킬별 구현 작업

### 1. 화염파편 지뢰 (Pyroclast Mine)
- [ ] 기폭 시 화염 투사체 다수 발사 (방사형)
- [ ] 연쇄 기폭 시 투사체 수/데미지 증가 가능

### 2. 폭풍 점사 지뢰 (Stormblast Mine)
- [ ] 기폭 시 번개 AoE 폭발
- [ ] 연쇄 기폭 시 추가 번개 데미지 증폭 스택

### 3. 분노 (Anger)
- [ ] 범위 내 타워 공격에 화염 추가 데미지 부여
- [ ] 오라 범위 및 화염 데미지 증가량 설정

### 4. 진노 (Wrath)
- [ ] 범위 내 타워 공격에 번개 추가 데미지 부여
- [ ] 오라 범위 및 번개 데미지 증가량 설정

### 5. 증오 (Hatred)
- [ ] 범위 내 타워 물리 데미지 일부를 냉기로 변환
- [ ] 냉기 변환 비율 및 추가 냉기 데미지 설정

### 6. 은총 (Grace)
- [ ] 범위 내 아군 설치물(타워/토템 등) 회피율 증가 버프
- [ ] 타워 디펜스 맥락: 특정 공격 빗나감 확률 부여

### 7. 결의 (Determination)
- [ ] 범위 내 타워 방어력(데미지 감소) 증가 버프
- [ ] 받는 데미지 % 감소 적용

### 8. 단련 (Discipline)
- [ ] 범위 내 타워/설치물에 에너지 실드(보조 HP) 부여
- [ ] 에너지 실드 수치 및 회복 속도 설정

### 9. 가속 (Haste)
- [ ] 범위 내 타워 공격 속도 및 투사체 속도 증가 버프
- [ ] 공격 속도 증가 % 설정

### 10. 원소의 순수함 (Purity of Elements)
- [ ] 범위 내 타워가 상태이상(점화/감속/감전) 면역 또는 저항 부여
- [ ] 상태이상 지속 시간 감소 % 설정

---

## 완료 기준
- [ ] 10종 스킬 ScriptableObject 데이터 정의
- [ ] MineBase 기폭 및 연쇄 기폭 정상 동작
- [ ] AuraBase 범위 내 타워 버프 적용/해제 정상 동작
- [ ] 오라 중첩 또는 충돌 규칙 정의 (동종 오라 중복 허용 여부)
