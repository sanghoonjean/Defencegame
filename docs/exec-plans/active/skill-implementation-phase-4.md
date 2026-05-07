# 스킬 구현 계획: Phase 4 — 채널링 + 지속 장판 (Channeling + Persistent)

## 목표
채널링 이동형 스킬 2종, 지속 오브/장판/DoT 계열 8종을 구현한다.
고정형 오브젝트(장판, 오브) 배치 및 지속 효과 관리 시스템을 확립한다.

## 선행 조건
- Phase 3 ChannelingBeam 컴포넌트 완료
- PersistentAreaBase 컴포넌트 설계

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 회오리바람 (Cyclone) | Channeling/Movement/Melee | Channel while spinning and hitting nearby enemies | 회전형 지속 공격/이동형 스킬 후보 |
| 2 | 황폐 (Blight) | Channeling/Chaos/DoT | Channeling chaos damage over time | 지속 둔화/카오스 도트 후보 |
| 3 | 겨울의 낙인 (Winter Orb) | Channeling/Cold | Channel to maintain an orb that fires projectiles | 유지형 자동 발사 오브 후보 |
| 4 | 폭풍 보주 (Orb of Storms) | Persistent/Lightning | Persistent lightning orb | 고정 전기장/보조 트리거 후보 |
| 5 | 정의의 화염 (Righteous Fire) | Persistent/Aura/Fire | Self-centered burning aura | 주변 지속 피해 필드 후보 |
| 6 | 소용돌이 (Vortex) | Persistent/Cold | Cold ground area over time | 냉기 장판/감속 후보 |
| 7 | 맹독성 비 (Toxic Rain) | Persistent/Projectile/Chaos | Pods create chaos damage areas | 지속 독 장판 후보 |
| 8 | 부식성 화살 (Caustic Arrow) | Persistent/Projectile/Chaos | Projectile creates caustic ground | 독 장판 후보 |
| 9 | 모독 (Desecrate) | Persistent/AoE | Creates corpses and ground effect | 시체 생성/시체 기반 연계 후보 |
| 10 | 전염 (Contagion) | Persistent/Chaos/DoT | Chaos debuff that spreads on death | 전염형 도트 후보 |

---

## 공통 컴포넌트

### PersistentAreaBase
- [ ] 맵 상 특정 위치에 지속 효과 영역 오브젝트 배치
- [ ] 활성 시간(duration) 관리, 만료 시 자동 제거
- [ ] 범위 내 적 주기적 틱 데미지 처리

### PersistentOrb
- [ ] 공중에 고정된 오브 오브젝트 유지
- [ ] 주기적으로 범위 내 적 타격 또는 투사체 발사

---

## 스킬별 구현 작업

### 1. 회오리바람 (Cyclone)
- [ ] 채널링 중 타워 주변 회전 범위 내 적 지속 타격
- [ ] 이동형 구현 시: 타워가 채널링 중 천천히 이동하며 공격 (타워 디펜스 맥락 검토 필요)
- [ ] 고정형 구현: 타워 중심 회전 범위 고정 지속 공격

### 2. 황폐 (Blight)
- [ ] 전방 좁은 채널링 원뿔 범위에 카오스 DoT 지속 적용
- [ ] 범위 내 적 이동속도 감소(slow) 디버프 추가

### 3. 겨울의 낙인 (Winter Orb)
- [ ] 채널링 중 타워 위에 오브 생성 유지
- [ ] 오브에서 주기적으로 냉기 투사체 자동 발사
- [ ] 채널링 종료 시 잔류 투사체 추가 발사 후 오브 소멸

### 4. 폭풍 보주 (Orb of Storms)
- [ ] 타워 범위 내 임의 위치에 번개 오브 고정 생성
- [ ] 오브 주변 적에게 주기적 번개 타격 또는 인근 스킬 발동 트리거

### 5. 정의의 화염 (Righteous Fire)
- [ ] 타워 중심 원형 지속 화염 오라 유지
- [ ] 오라 범위 내 적에게 매 틱 화염 데미지
- [ ] 타워 자체 HP 감소(자해) 메커니즘 검토

### 6. 소용돌이 (Vortex)
- [ ] 타워 공격 범위 내 지정 지점에 냉기 장판 생성
- [ ] 장판 위 적 감속 + 주기적 냉기 DoT

### 7. 맹독성 비 (Toxic Rain)
- [ ] 범위 내 여러 지점에 독 포드(pod) 낙하
- [ ] 포드 착지 후 일정 시간 독 장판 생성
- [ ] 장판 위 적 독(poison) DoT 적용

### 8. 부식성 화살 (Caustic Arrow)
- [ ] 투사체 착탄 지점에 독 장판(caustic ground) 생성
- [ ] 장판 크기/지속 시간/DoT 수치 설정

### 9. 모독 (Desecrate)
- [ ] 범위 내 시체 오브젝트 생성 (이후 소환 스킬 연계용)
- [ ] 시체 근처 지면 효과(부패 장판) 추가

### 10. 전염 (Contagion)
- [ ] 적에게 카오스 DoT 부여
- [ ] 해당 적 사망 시 주변 적에게 DoT 전이(전염)

---

## 완료 기준
- [ ] 10종 스킬 ScriptableObject 데이터 정의
- [ ] PersistentArea 생성/유지/만료 정상 동작
- [ ] 채널링 오브 자동 발사 로직 정상 동작
- [ ] 전염 사망 트리거 로직 정상 동작
