# 스킬 구현 계획: Phase 2 — 투사체 공격 + 광역 마법 (Projectile Attacks + AoE Spells)

## 목표
투사체 공격(Attack) 계열 7종과 기본 AoE 마법 3종을 구현한다.
분산 투사체, 왕복 투사체, 원형/링 범위 패턴의 컴포넌트를 확립한다.

## 선행 조건
- Phase 1 스킬 컴포넌트 (ProjectileBase) 완료

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 탄막 (Barrage) | Projectile/Attack | Sequential concentrated projectile fire | 단일 집중 사격 타워 후보 |
| 2 | 회오리 사격 (Tornado Shot) | Projectile/AoE/Attack | Arrow attack that splits into secondary projectiles | 분산 화살/광역 클리어 후보 |
| 3 | 번개 화살 (Lightning Arrow) | Projectile/AoE/Attack | Lightning projectile with area lightning effect | 감전/범위 전이형 후보 |
| 4 | 얼음 화살 (Ice Shot) | Projectile/AoE/Attack | Cold projectile with cone/area cold effect | 냉기 범위/감속 타워 후보 |
| 5 | 환영 무기 투척 (Spectral Throw) | Projectile/Attack | Thrown weapon projectile that returns | 왕복 투사체 후보 |
| 6 | 코브라 채찍 (Cobra Lash) | Projectile/Chain/Attack | Poison/chaos projectile that chains | 독 체인 타워 후보 |
| 7 | 폭발 화살 (Explosive Arrow) | Projectile/AoE/Attack | Projectile stacks before delayed explosion | 중첩 후 폭발형 후보 |
| 8 | 얼음 폭발 (Ice Nova) | AoE/Cold | Circular cold nova | 원형 냉기 범위 타워 후보 |
| 9 | 충격 폭발 (Shock Nova) | AoE/Lightning | Lightning nova with ring-style area | 링 범위 전기 타워 후보 |
| 10 | 화염 폭풍 (Firestorm) | AoE/Persistent/Fire | Falling fire impacts over duration | 지속 낙하/포격 타워 후보 |

---

## 공통 컴포넌트

### SplitProjectile
- [ ] 투사체 명중 후 다수의 보조 투사체(secondary projectile) 생성
- [ ] 분열 각도, 수량 설정 가능

### NovaArea
- [ ] 타워 위치 또는 지정 위치 중심으로 원형/링 범위 즉시 발생
- [ ] 내경(inner radius) / 외경(outer radius) 설정 지원

---

## 스킬별 구현 작업

### 1. 탄막 (Barrage)
- [ ] 짧은 간격으로 다수의 투사체를 순차 발사
- [ ] 집중 단일 타겟 추적 로직

### 2. 회오리 사격 (Tornado Shot)
- [ ] 주 투사체 발사 → 명중 또는 일정 거리 후 보조 투사체 분열
- [ ] 보조 투사체 분산 각도 및 수량 설정

### 3. 번개 화살 (Lightning Arrow)
- [ ] 투사체 명중 시 주변 범위에 번개 효과 전이
- [ ] 감전(shock) 상태이상 적용 로직

### 4. 얼음 화살 (Ice Shot)
- [ ] 투사체 명중 지점 기준 전방 콘(cone) 또는 원형 냉기 범위 생성
- [ ] 범위 내 적 감속(slow) 적용

### 5. 환영 무기 투척 (Spectral Throw)
- [ ] 투사체가 목표 방향으로 발사 후 되돌아오는 왕복 경로 처리
- [ ] 왕복 경로 모두에서 적 타격 적용

### 6. 코브라 채찍 (Cobra Lash)
- [ ] 독/카오스 속성 투사체 발사, 체인 로직 적용
- [ ] 체인 시 독(poison) DoT 상태이상 부여

### 7. 폭발 화살 (Explosive Arrow)
- [ ] 동일 적에게 중첩 스택 누적 (최대 20중첩 기준)
- [ ] 중첩 소진 또는 타이머 후 대형 폭발 발생
- [ ] 스택 UI 표시 연계

### 8. 얼음 폭발 (Ice Nova)
- [ ] 타워 중심 원형 냉기 범위 즉발
- [ ] 쿨타임 기반 주기 발동
- [ ] 냉기 속성: 감속/동결 후보

### 9. 충격 폭발 (Shock Nova)
- [ ] 내경(도넛 모양) 링 범위 번개 공격
- [ ] 링 내/외경 설정으로 사각지대(중심부) 구현

### 10. 화염 폭풍 (Firestorm)
- [ ] 지정 범위에 일정 시간 동안 낙하 화염 다수 생성
- [ ] 낙하 지점 랜덤 분산, 주기적 타격

---

## 완료 기준
- [ ] 10종 스킬 ScriptableObject 데이터 정의
- [ ] 분열/왕복/링 범위 등 특수 투사체 패턴 정상 동작
- [ ] 중첩 스택(폭발 화살) 로직 정상 동작
- [ ] 지속 낙하(화염 폭풍) 로직 정상 동작
