# 스킬 구현 계획: Phase 8 — 저주/징표 (Curse + Mark)

## 목표
적에게 취약점을 부여하는 저주/징표 계열 디버프 스킬 8종과
이동 패턴 참고용 이동 스킬 2종을 구현한다.
디버프 관리 시스템과 취약 속성 데미지 연계를 확립한다.

## 선행 조건
- EnemySystem 디버프 스택 처리 가능
- TowerSystem 속성 데미지 적용 가능

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 인화성 (Flammability) | Curse/Hex/Fire | Fire vulnerability curse | 화염 취약 디버프 후보 |
| 2 | 동상 (Frostbite) | Curse/Hex/Cold | Cold vulnerability curse | 냉기 취약 디버프 후보 |
| 3 | 전도성 (Conductivity) | Curse/Hex/Lightning | Lightning vulnerability curse | 번개 취약 디버프 후보 |
| 4 | 절망 (Despair) | Curse/Hex/Chaos | Chaos/DoT vulnerability curse | 카오스/도트 취약 후보 |
| 5 | 시간의 사슬 (Temporal Chains) | Curse/Hex | Slows actions/effects | 둔화 디버프 후보 |
| 6 | 쇠약화 (Enfeeble) | Curse/Hex | Reduces enemy offensive power | 공격 약화 디버프 후보 |
| 7 | 암살자의 징표 (Assassin's Mark) | Mark/Critical | Mark supporting critical strikes | 치명타 취약/보상 후보 |
| 8 | 저격수의 징표 (Sniper's Mark) | Mark/Projectile | Mark that benefits projectile hits | 투사체 증폭 디버프 후보 |
| 9 | 화염 질주 (Flame Dash) | Movement/Fire | Short teleport/dash | 적 이동 패턴/스킬 변환 참고 |
| 10 | 서리 점멸 (Frostblink) | Movement/Cold | Instant cold blink | 점멸/냉기 이동 패턴 참고 |

---

## 공통 컴포넌트

### DebuffData (ScriptableObject)
- [ ] 디버프 종류(취약/둔화/약화/징표) 설정
- [ ] 지속 시간, 중첩 허용 여부, 효과 수치 설정

### CurseApplier
- [ ] 범위 또는 투사체로 적에게 디버프 적용
- [ ] 동시에 부여 가능한 저주 수 제한 처리 (타워당 1개 기준)

### Enemy 디버프 처리 확장
- [ ] 속성 취약(Flammability 등): 해당 속성 받는 데미지 % 증가
- [ ] 둔화(Temporal Chains): 모든 행동/이동속도 % 감소
- [ ] 약화(Enfeeble): 기지 도달 시 플레이어 데미지 감소

---

## 스킬별 구현 작업

### 1. 인화성 (Flammability)
- [ ] 적용 대상 화염 피해 저항 감소 (화염 받는 데미지 증가)
- [ ] 점화(ignite) 발생 확률 추가 증가

### 2. 동상 (Frostbite)
- [ ] 적용 대상 냉기 피해 저항 감소
- [ ] 동결/감속 상태이상 발생 확률 추가 증가

### 3. 전도성 (Conductivity)
- [ ] 적용 대상 번개 피해 저항 감소
- [ ] 감전(shock) 발생 확률 추가 증가

### 4. 절망 (Despair)
- [ ] 적용 대상 카오스 피해 저항 감소
- [ ] DoT(지속 데미지) 받는 피해 % 증가

### 5. 시간의 사슬 (Temporal Chains)
- [ ] 적 이동속도 % 감소
- [ ] 적이 받는 상태이상(DoT, 둔화 등) 지속 시간 연장

### 6. 쇠약화 (Enfeeble)
- [ ] 적 기지 도달 시 플레이어 HP 감소량 % 감소
- [ ] 적 공격력 약화(소환 유닛/스킬 사용 시 적용)

### 7. 암살자의 징표 (Assassin's Mark)
- [ ] 단일 대상 마크, 해당 적에 대한 치명타 확률 증가
- [ ] 마크된 적 처치 시 하위 큐브 드롭 보너스

### 8. 저격수의 징표 (Sniper's Mark)
- [ ] 단일 대상 마크, 투사체 스킬의 데미지 증가
- [ ] 마크된 적에 명중 시 분열 투사체 생성

### 9. 화염 질주 (Flame Dash)
- [ ] 타워 디펜스 맥락: 특정 적이 짧은 거리 순간 이동하는 패턴에 참고
- [ ] 또는 플레이어 카메라 이동 스킬로 활용 검토

### 10. 서리 점멸 (Frostblink)
- [ ] 타워 디펜스 맥락: 냉기 속성 적의 이동 패턴 참고
- [ ] 점멸 시 착지 지점 냉기 장판 생성 이펙트 참고

---

## 완료 기준
- [ ] 8종 저주/징표 ScriptableObject 데이터 정의
- [ ] 속성 취약(화염/냉기/번개/카오스) 데미지 계산 적용 확인
- [ ] 시간의 사슬 이동속도 감소 정상 동작
- [ ] 징표 처치 시 보상 연계 정상 동작
- [ ] 이동 스킬 2종 적 패턴 적용 가이드 문서화
