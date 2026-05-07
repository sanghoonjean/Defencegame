# 스킬 구현 계획: Phase 6 — 소환(Minion) 확장 + 토템 + 덫 (Minion Ext. + Totem + Trap)

## 목표
소환 공격형 스킬 1종, 토템 계열 4종, 덫 계열 5종을 구현한다.
설치형 오브젝트(토템/덫)의 배치 및 트리거 시스템을 확립한다.

## 선행 조건
- Phase 5 MinionManager, MinionBase 완료
- 타워와 별개로 맵에 배치되는 설치물 시스템 설계

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 지배의 일격 (Dominating Blow) | Minion/Melee | Creates sentinel minions from affected enemies | 처치/전환형 소환 후보 |
| 2 | 신성한 화염 토템 (Holy Flame Totem) | Totem/Fire | Totem fires holy flame projectiles | 임시 화염 타워 후보 |
| 3 | 충격파 토템 (Shockwave Totem) | Totem/Physical | Totem releases shockwaves | 임시 충격파 타워 후보 |
| 4 | 회복 토템 (Rejuvenation Totem) | Totem/Aura | Totem provides regeneration aura | 회복/지원 설치물 후보 |
| 5 | 공성 쇠뇌 (Siege Ballista) | Totem/Projectile | Ballista totem with projectile attack | 원거리 설치 포탑 후보 |
| 6 | 번개 덫 (Lightning Trap) | Trap/Lightning | Trap releases lightning projectiles | 전기 함정 후보 |
| 7 | 폭발 덫 (Explosive Trap) | Trap/Fire | Trap creates explosions | 폭발 함정 후보 |
| 8 | 얼음 덫 (Ice Trap) | Trap/Cold | Trap creates cold explosion | 냉기 함정 후보 |
| 9 | 지진 덫 (Seismic Trap) | Trap/Physical | Trap releases repeated seismic bursts | 지속 충격 함정 후보 |
| 10 | 화염 덫 (Fire Trap) | Trap/Fire/Persistent | Trap creates fire and burning ground | 화염 장판 함정 후보 |

---

## 공통 컴포넌트

### PlaceableObject (토템/덫 공통 기반)
- [ ] 맵 타일 위에 배치 가능한 오브젝트
- [ ] 배치 수량 제한 및 기존 설치물 교체 로직
- [ ] 수명(duration) 또는 내구도(charges) 관리

### TotemBase (PlaceableObject 상속)
- [ ] 배치 즉시 자동 공격/지원 시작
- [ ] 자체 HP 보유, 피격 가능 여부 설정

### TrapBase (PlaceableObject 상속)
- [ ] 비활성 상태로 배치, 적 접근 시 트리거
- [ ] 트리거 후 즉발 효과 발동 및 재장전 또는 소멸

---

## 스킬별 구현 작업

### 1. 지배의 일격 (Dominating Blow)
- [ ] 타격한 적을 소환 유닛(Sentinel)으로 전환
- [ ] 전환된 유닛은 타워 편으로 이동 전환 후 근접 공격

### 2. 신성한 화염 토템 (Holy Flame Totem)
- [ ] 배치 후 자동으로 신성 화염 투사체 발사
- [ ] 화염 속성 투사체, 기본 포탑과 유사한 공격 패턴

### 3. 충격파 토템 (Shockwave Totem)
- [ ] 주기적으로 주변 원형 충격파 방출
- [ ] 물리 속성, 범위 내 적 넉백 또는 기절 가능

### 4. 회복 토템 (Rejuvenation Totem)
- [ ] 배치 후 주변 타워 HP 지속 회복 오라 제공
- [ ] 범위 내 타워 회복량 설정

### 5. 공성 쇠뇌 (Siege Ballista)
- [ ] 배치 후 원거리 투사체 발사 (장거리, 관통)
- [ ] 타워보다 사거리 길고 단일 고데미지 특화

### 6. 번개 덫 (Lightning Trap)
- [ ] 적 접근 시 주변으로 번개 투사체 다수 발사
- [ ] 감전 상태이상 부여 가능

### 7. 폭발 덫 (Explosive Trap)
- [ ] 적 접근 시 즉발 폭발 AoE
- [ ] 주변 다수 적 범위 피해

### 8. 얼음 덫 (Ice Trap)
- [ ] 적 접근 시 냉기 폭발 발동
- [ ] 범위 내 적 감속/동결 상태이상

### 9. 지진 덫 (Seismic Trap)
- [ ] 트리거 후 일정 시간 동안 반복 충격파 발생 (지속형 덫)
- [ ] 물리 속성, 범위 내 적 주기 타격

### 10. 화염 덫 (Fire Trap)
- [ ] 트리거 후 즉발 화염 폭발 + 장판 생성
- [ ] 장판 위 적 지속 화염 DoT

---

## 완료 기준
- [ ] 10종 스킬 ScriptableObject 데이터 정의
- [ ] TotemBase / TrapBase 설치 및 트리거 로직 정상 동작
- [ ] 배치 수량 제한 정상 동작
- [ ] 토템 자동 공격, 덫 트리거 후 효과 정상 발동
- [ ] 지배의 일격 적→아군 전환 로직 정상 동작
