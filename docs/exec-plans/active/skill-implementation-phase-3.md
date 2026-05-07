# 스킬 구현 계획: Phase 3 — 광역 마법 + 근접 AoE (AoE Spells + Melee AoE)

## 목표
낙하/물리 광역 마법 2종과 근접 범위 공격 5종, 채널링 빔 계열 3종을 구현한다.
전방 충격, 지면 파동, 빔 계열 패턴의 컴포넌트를 확립한다.

## 선행 조건
- Phase 1 ProjectileBase 완료
- MeleeAttackBase 컴포넌트 설계

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 칼날비 (Bladefall) | AoE/Physical | Falling blades in an area | 물리 낙하 범위 후보 |
| 2 | 수확 (Reap) | AoE/Physical | Physical blood-themed area spell | 광역 물리/누적 리스크 후보 |
| 3 | 지면 강타 (Ground Slam) | AoE/Melee/Slam | Frontal ground impact | 전방 부채꼴 충격 후보 |
| 4 | 대지 강타 (Earthquake) | AoE/Melee/Slam | Initial hit followed by delayed aftershock | 지연 폭발형 후보 |
| 5 | 파쇄 (Sunder) | AoE/Melee/Slam | Ground wave attack | 라인 관통 파동 후보 |
| 6 | 회전베기 (Cleave) | AoE/Melee | Melee area slash | 근접 범위 후보 |
| 7 | 휩쓸기 (Sweep) | AoE/Melee | Circular melee sweep | 원형 근접 범위 후보 |
| 8 | 신성한 진노 (Divine Ire) | Channeling/Beam | Channel then release beam-like burst | 충전 레이저/차지 포탑 후보 |
| 9 | 작열 광선 (Scorching Ray) | Channeling/Beam | Sustained fire beam applying debuff | 지속 화염 레이저 후보 |
| 10 | 소각 (Incinerate) | Channeling/Fire | Channeling fire skill that grows in stages | 단계 증가 화염 방사 후보 |

---

## 공통 컴포넌트

### MeleeAreaBase
- [ ] 타워 기준 전방 부채꼴 또는 원형 범위 콜라이더 생성
- [ ] 범위 내 다수 적 일괄 데미지 처리

### GroundWave
- [ ] 타워에서 전방으로 지면 파동 이동 처리
- [ ] 파동 경로 상 적 관통 타격

### ChannelingBeam
- [ ] 채널링 시작/유지/해제 상태 관리
- [ ] 빔 레이캐스트 또는 얇은 직선 콜라이더로 경로 상 적 지속 타격

---

## 스킬별 구현 작업

### 1. 칼날비 (Bladefall)
- [ ] 타워 공격 범위 내 랜덤 지점에 낙하 칼날 생성
- [ ] 일정 시간 동안 다수 칼날 연속 낙하
- [ ] 물리 속성 데미지 적용

### 2. 수확 (Reap)
- [ ] 전방 부채꼴 광역 물리 공격
- [ ] 공격 직후 타워에 일시적 불안정(HP 소모 or 쿨타임 증가) 리스크 부여 검토

### 3. 지면 강타 (Ground Slam)
- [ ] 타워 전방 부채꼴 콘 범위 즉발 충격
- [ ] 범위 내 적 넉백 또는 기절 부여 가능

### 4. 대지 강타 (Earthquake)
- [ ] 1차 즉발 타격 처리
- [ ] 일정 시간 후 2차 여진(aftershock) 폭발 처리
- [ ] 1차 타격 위치에 지연 AoE 생성

### 5. 파쇄 (Sunder)
- [ ] 타워 전방 직선 방향으로 지면 파동 발사
- [ ] 파동 경로 상 모든 적 관통 타격
- [ ] 파동 끝 지점에 소규모 AoE 폭발

### 6. 회전베기 (Cleave)
- [ ] 타워 전방 90~120도 부채꼴 근접 베기
- [ ] 범위 내 다수 적 동시 타격

### 7. 휩쓸기 (Sweep)
- [ ] 타워 중심 360도 원형 회전 베기
- [ ] 사거리는 짧지만 전방위 타격

### 8. 신성한 진노 (Divine Ire)
- [ ] 채널링 중 에너지 충전 스택 누적 (최대 20스택)
- [ ] 채널링 종료 시 충전 스택에 비례한 강력한 빔/폭발 발사
- [ ] 충전 단계 시각 효과 연계

### 9. 작열 광선 (Scorching Ray)
- [ ] 지속 화염 빔 유지 중 적에게 지속 데미지 + 화염 저항 감소 디버프 적용
- [ ] 채널링 유지 시간에 따라 디버프 스택 증가

### 10. 소각 (Incinerate)
- [ ] 채널링 지속 시간에 따라 3~4단계로 데미지/범위 증가
- [ ] 채널링 해제 시 단계 초기화

---

## 완료 기준
- [ ] 10종 스킬 ScriptableObject 데이터 정의
- [ ] 근접 범위(부채꼴/원형) 히트박스 정상 동작
- [ ] 지연 발동(대지 강타 여진) 로직 정상 동작
- [ ] 채널링 단계 증가 로직 정상 동작
