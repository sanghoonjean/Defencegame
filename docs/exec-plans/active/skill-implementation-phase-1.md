# 스킬 구현 계획: Phase 1 — 투사체 마법 (Projectile Spells)

## 목표
기본 투사체 마법 10종을 타워 공격 스킬로 구현한다.
단일/광역/체인/지속 투사체 패턴의 기반 컴포넌트를 확립한다.

## 선행 조건
- Phase 2 빌드 시스템 완료 (TowerSystem, SkillData ScriptableObject)
- ProjectileBase 컴포넌트 구현

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 화염구 (Fireball) | Projectile/AoE | Exploding fire projectile | 기본 폭발 투사체 타워 후보 |
| 2 | 동결 파동 (Freezing Pulse) | Projectile/Cold | Cold projectile with stronger close-range hit | 근거리 고효율 냉기 포탑 후보 |
| 3 | 얼음 창 (Ice Spear) | Projectile/Cold | Cold projectile with long-range critical behavior | 장거리 치명/저격형 냉기 포탑 후보 |
| 4 | 번개 차원 이동 (Spark) | Projectile/Lightning | Erratic lightning projectiles | 랜덤 반사/다중 경로 타워 후보 |
| 5 | 실체 없는 칼날 (Ethereal Knives) | Projectile/Physical | Fan of physical blades | 부채꼴 물리 투사체 후보 |
| 6 | 전기불꽃 (Arc) | Projectile/Chain | Lightning skill that chains between enemies | 체인 라이트닝 타워 핵심 후보 |
| 7 | 굴러가는 마그마 (Rolling Magma) | Projectile/Chain/AoE | Bouncing fire projectile | 바운스 폭발형 타워 후보 |
| 8 | 구형 번개 (Ball Lightning) | Projectile/AoE | Slow moving lightning orb with repeated hits | 지속 접촉형 전기 구체 후보 |
| 9 | 동력 폭발 (Kinetic Blast) | Projectile/AoE | Projectile attack that creates secondary explosions | 충격파/분산 폭발 타워 후보 |
| 10 | 마력 착취 (Power Siphon) | Projectile | Wand projectile attack with culling/power charge identity | 저체력 마무리/자원 연계 후보 |

---

## 공통 컴포넌트

### ProjectileBase
- [ ] 발사 방향, 속도, 최대 사거리 설정
- [ ] OnHitEnemy(Enemy target) 이벤트 처리
- [ ] ObjectPool 반납 처리

### ProjectileSkillData (ScriptableObject)
- [ ] 투사체 속도, 데미지, 범위, 속성(fire/cold/lightning/physical) 필드
- [ ] 체인 횟수, 관통 여부, 바운스 여부 필드

---

## 스킬별 구현 작업

### 1. 화염구 (Fireball)
- [ ] 단일 투사체 발사 → 착탄 시 AoE 폭발 처리
- [ ] 폭발 반경 내 다수 Enemy에 데미지 적용
- [ ] 화염 속성: 점화(ignite) 상태이상 후보

### 2. 동결 파동 (Freezing Pulse)
- [ ] 거리에 따른 데미지 감쇠 로직 (근거리 고데미지)
- [ ] 냉기 속성: 감속(slow) 상태이상 적용

### 3. 얼음 창 (Ice Spear)
- [ ] 장거리에서 치명타 확률 증가 로직 구현
- [ ] 냉기 속성: 동결(freeze) 상태이상 후보

### 4. 번개 차원 이동 (Spark)
- [ ] 다수의 랜덤 방향 투사체 발사
- [ ] 투사체가 경로를 무작위로 이동하는 물리 시뮬레이션
- [ ] 번개 속성: 감전(shock) 후보

### 5. 실체 없는 칼날 (Ethereal Knives)
- [ ] 부채꼴 방향으로 다중 투사체 동시 발사
- [ ] 물리 속성 데미지 적용

### 6. 전기불꽃 (Arc)
- [ ] 첫 번째 적 명중 후 인접 적으로 자동 체인
- [ ] 체인 횟수 제한 및 체인당 데미지 감소 로직
- [ ] 번개 속성: 감전 후보

### 7. 굴러가는 마그마 (Rolling Magma)
- [ ] 투사체가 지면을 바운스하며 이동
- [ ] 바운스 지점마다 소규모 AoE 폭발
- [ ] 화염 속성 적용

### 8. 구형 번개 (Ball Lightning)
- [ ] 느린 속도로 직선 이동하는 구체 투사체
- [ ] 이동 중 범위 내 적에게 주기적으로 반복 타격
- [ ] 번개 속성 적용

### 9. 동력 폭발 (Kinetic Blast)
- [ ] 투사체 명중 시 추가 분산 폭발(secondary explosion) 생성
- [ ] 분산 폭발 수 및 배치 랜덤성 구현

### 10. 마력 착취 (Power Siphon)
- [ ] 적 체력 임계값 이하(예: 20%)에서 즉시 처치(culling) 로직
- [ ] 처치 시 자원(하위 큐브) 드롭 확률 증가 보너스 연계

---

## 완료 기준
- [ ] 10종 스킬 ScriptableObject 데이터 정의
- [ ] 각 스킬별 투사체 프리팹 생성
- [ ] 타워에 스킬 장착 후 적에게 정상 데미지 적용 확인
- [ ] 체인/바운스/AoE 등 특수 메커니즘 정상 동작 확인
