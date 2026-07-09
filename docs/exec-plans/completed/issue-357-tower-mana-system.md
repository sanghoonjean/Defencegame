# Issue #357 — 타워 마나 시스템 추가

## 1. 시스템 구조

타워가 스킬을 사용할 때 마나를 소비하고, 마나가 부족하면 공격하지 못하는 마나 시스템을 추가한다.

```
Tower
 ├── MaxMana (float) — 타워별 최대 마나 (Inspector 설정)
 ├── CurrentMana (float) — 현재 마나
 ├── ManaRegenRate (float) — 초당 마나 회복량
 └── Update() — 매 프레임 마나 회복, 공격 시 ManaCost 차감

SkillData
 └── manaCost (float) — 스킬 발동 시 소모 마나

UI: TowerManaBarUI (새 컴포넌트)
 └── 선택된 타워의 마나바를 UnitPanel에 표시
```

마나 소비 흐름:
1. Tower.Update() → AttackCooldown 충족 → 타겟 발견
2. CurrentMana >= SkillData.manaCost 확인
3. 충족 시: 마나 차감 → Attack() 실행
4. 부족 시: 공격 스킵 (타이머는 리셋하지 않음 — 마나 차면 즉시 공격)

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs` — MaxMana, CurrentMana, ManaRegenRate 필드 추가, Update()에 마나 회복/소비 로직
- `MakeDefence/Assets/Scripts/Gameplay/Tower/SkillData.cs` — manaCost 필드 추가
- `MakeDefence/Assets/Scripts/Systems/GameStateSystem.cs` — 웨이브 시작/종료 시 마나 관련 리셋 여부 검토

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/UI/TowerManaBarUI.cs` — 선택된 타워의 CurrentMana/MaxMana를 UnityUI Slider로 표시. Tower.OnManaChanged 이벤트 구독.

## 4. 테스트 계획

- [ ] 마나 초기값 = MaxMana (타워 배치 시)
- [ ] 시간이 지남에 따라 CurrentMana가 ManaRegenRate로 회복되는지 확인
- [ ] CurrentMana < manaCost 상태에서 공격하지 않는지 확인
- [ ] 마나 회복 후 공격이 즉시 재개되는지 확인
- [ ] UnitPanel에 마나바가 표시되고 실시간으로 업데이트되는지 확인
- [ ] 타워 선택 해제/재선택 시 마나바가 올바른 타워 정보를 표시하는지 확인

## 5. 위험 요소

- **기존 스킬 manaCost 기본값**: 0으로 설정하면 기존 동작 유지 (하위 호환)
- **마나 없는 타워 처리**: maxMana = 0이면 마나 시스템 비활성화로 처리 (기존 타워 프리팹 수정 최소화)
- **UI 씬 연결**: TowerManaBarUI를 UnitPanel에 연결하려면 SampleScene 수정 필요
