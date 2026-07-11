# Issue #359 — 직업 군 구현 (전사, 마법사, 궁수)

## 1. 시스템 구조

```
JobClass (enum)
    ↓ [SerializeField]
Tower.jobClass
    ↓ RefreshStats()
직업별 스탯 보너스 (damage / speed / range / crit / skillCD / manaRegen)
    ↓
UnitPanelController → JobClassDisplayUI (직업 이름 표시)
```

### 직업별 특성 (스킬 제한 없음 — 모든 직업 모든 스킬 장착 가능)

| 직업 | 스탯 보너스 |
|------|------------|
| 전사 (Warrior) | 데미지 +20%, 치명타 데미지 +30% |
| 마법사 (Mage) | 스킬 CD 감소 +20%, 마나 재생 +10% |
| 궁수 (Archer) | 공격 속도 +20%, 사거리 +20% |

## 2. 수정 파일

- `Assets/Scripts/Gameplay/Tower/Tower.cs` — `[SerializeField] JobClass jobClass` 추가, `RefreshStats()`에 클래스 보너스 적용
- `Assets/Scripts/Systems/InventorySystem.cs` — 스킬 장착 경로 유지 (제한 없음)

## 3. 신규 클래스 / 파일

- `Assets/Scripts/Gameplay/Player/JobClass.cs` — `JobClass` enum (None, Warrior, Mage, Archer)
- `Assets/Scripts/UI/JobClassDisplayUI.cs` — UnitPanel 내 직업 이름 TextMeshPro 컴포넌트 제어

## 4. 테스트 계획

- [ ] 전사 Tower 프리팹에 jobClass = Warrior 설정 후 데미지·크리티컬 스탯 확인
- [ ] 마법사 Tower 스킬 CD 감소·마나 재생 보너스 확인
- [ ] 궁수 Tower 공격 속도·사거리 보너스 확인 (스킬 장착 후에도 보너스 유지 확인)
- [ ] 어떤 직업이든 모든 스킬 자유롭게 장착 가능 확인
- [ ] 유닛 패널 선택 시 직업 이름 올바르게 표시

## 5. 위험 요소

- 기존 Tower 프리팹들은 `jobClass = None`으로 초기화 → Inspector에서 직업 지정 필요
- JobClassDisplayUI는 씬 Canvas에 컴포넌트 추가 및 TMP 레이블 연결 필요
