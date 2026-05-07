# 스킬 구현 계획: Phase 5 — DoT + 소환 (DoT + Minion)

## 목표
단일 DoT/흡수 스킬 1종과 소환 계열 스킬 9종을 구현한다.
소환 유닛 관리 시스템(MinionManager)과 시체 연계 메커니즘을 확립한다.

## 선행 조건
- Phase 4 PersistentAreaBase 완료
- EnemySystem의 OnEnemyDied 이벤트 활용 가능

---

## 스킬 목록

| # | 스킬명 (한국어) | 카테고리 | Mechanism Summary | Defense Analysis Note |
|---|---------------|---------|-------------------|-----------------------|
| 1 | 정수 흡수 (Essence Drain) | Projectile/Chaos/DoT | Projectile applying chaos DoT and recovery | 단일 도트/흡수 후보 |
| 2 | 좀비 소환 (Raise Zombie) | Minion | Raises zombie minions from corpses | 탱커 소환 유닛 후보 |
| 3 | 해골 소환 (Summon Skeletons) | Minion | Summons temporary skeletons | 물량형 소환 후보 |
| 4 | 망령 소환 (Raise Spectre) | Minion | Raises slain monsters as spectres | 몬스터 복제형 후보 |
| 5 | 수호자 기동 (Animate Guardian) | Minion | Animates equipment into guardian minion | 장비 기반 버프/탱커 후보 |
| 6 | 무기 기동 (Animate Weapon) | Minion | Animates weapons as minions | 다수 소환/무기 변환 후보 |
| 7 | 격노의 유령 소환 (Summon Raging Spirit) | Minion/Fire | Summons temporary flying spirits | 돌진형 소환 후보 |
| 8 | 성스러운 유물 소환 (Summon Holy Relic) | Minion | Summons a relic with support behavior | 보조 공격/회복 소환 후보 |
| 9 | 고통의 전령 (Herald of Agony) | Minion/Herald/Chaos | Creates agony crawler through poison/virulence | 독 기반 소환/스택 후보 |
| 10 | 골렘 소환 (Summon Golem) | Minion | Summons golem minions that often grant buffs | 버프 제공 소환 후보 |

---

## 공통 컴포넌트

### MinionBase
- [ ] 소환 유닛 기본 AI: 타겟 선정, 이동, 공격
- [ ] 수명(duration) 관리, 만료 시 자동 제거
- [ ] MinionManager에 등록/해제

### MinionManager
- [ ] 타워별 활성 소환 유닛 목록 관리
- [ ] 최대 소환 수 제한 처리
- [ ] 소환 유닛 사망/만료 이벤트 처리

### CorpseManager (Phase 4 모독과 연계)
- [ ] 사망한 적의 시체 위치 저장
- [ ] 시체 소환 스킬에서 참조 가능

---

## 스킬별 구현 작업

### 1. 정수 흡수 (Essence Drain)
- [ ] 단일 타겟 투사체 발사 → 카오스 DoT 부여
- [ ] 대상 DoT 틱마다 타워(또는 플레이어) HP 소량 회복

### 2. 좀비 소환 (Raise Zombie)
- [ ] 시체 위치에서 좀비 소환 (CorpseManager 연계)
- [ ] 좀비는 적을 향해 이동하며 근접 공격
- [ ] 좀비는 자체 HP 보유, 피격 가능한 탱커 역할

### 3. 해골 소환 (Summon Skeletons)
- [ ] 타워 주변에 다수 해골 소환 (시체 불필요)
- [ ] 해골은 낮은 HP/공격력, 짧은 수명의 물량형 유닛
- [ ] 최대 동시 소환 수 제한

### 4. 망령 소환 (Raise Spectre)
- [ ] 처치한 적의 스탯/능력을 복제한 망령 소환
- [ ] 망령은 원본 적 스킬 패턴을 타워 측에서 사용

### 5. 수호자 기동 (Animate Guardian)
- [ ] 고정 위치에 수호자 소환 (시체/아이템 기반)
- [ ] 수호자는 주변 타워에 버프(공격력/방어력) 제공 오라 유지

### 6. 무기 기동 (Animate Weapon)
- [ ] 여러 무기 소환 유닛 생성 (수명 있음)
- [ ] 각 유닛이 독립적으로 적을 추적하며 공격

### 7. 격노의 유령 소환 (Summon Raging Spirit)
- [ ] 적을 향해 돌진하는 화염 유령 소환
- [ ] 접촉 시 폭발 데미지 후 소멸, 수명 짧음

### 8. 성스러운 유물 소환 (Summon Holy Relic)
- [ ] 타워 주변에 유물 소환, 공중 부유
- [ ] 인근 타워 공격 시 연쇄 신성 공격 트리거 또는 주변 소환 유닛 회복

### 9. 고통의 전령 (Herald of Agony)
- [ ] 타워가 독 스택 누적 시 독거미(Agony Crawler) 소환
- [ ] 독거미는 강력한 단일 유닛, 지속적으로 적 추적 공격

### 10. 골렘 소환 (Summon Golem)
- [ ] 강력한 골렘 1기 소환 (높은 HP, 느린 공격)
- [ ] 골렘 종류(화염/얼음/번개)에 따라 타워에 속성 버프 제공

---

## 완료 기준
- [ ] 10종 스킬 ScriptableObject 데이터 정의
- [ ] MinionManager 등록/해제 정상 동작
- [ ] 시체 기반 소환(좀비, 망령) 정상 동작
- [ ] 소환 유닛 AI (이동/공격/사망) 정상 동작
- [ ] 버프 오라(수호자, 골렘) 타워에 정상 반영
