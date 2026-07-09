# Issue #359 — 직업 군 구현 (전사, 마법사, 궁수)

## 1. 시스템 구조

```
JobClass (enum)
    ↓ [SerializeField]
Tower.jobClass
    ↓ RefreshStats()
직업별 스탯 보너스 (damage / speed / range / crit / skillCD)
    ↓
SkillData.requiredClass
    ↓ InventorySystem.EquipSkill()
직업 불일치 시 장착 거부
    ↓
UnitPanelController → JobClassDisplayUI (직업 이름/아이콘 표시)
```

### 직업별 특성

| 직업 | 스탯 보너스 | 허용 스킬 |
|------|------------|----------|
| 전사 (Warrior) | 데미지 +20%, 치명타 데미지 +30% | MoltenStrike |
| 마법사 (Mage) | 스킬 CD 감소 +20%, 마나 재생 +10% | Fireball, ParalysisMagic, LightningSpear, FreezingPulse |
| 궁수 (Archer) | 공격 속도 +20%, 사거리 +20% | LightningArrow, CausticArrow, PoisonCloud |

## 2. 수정 파일

- `Assets/Scripts/Gameplay/Tower/Tower.cs` — `[SerializeField] JobClass jobClass` 추가, `RefreshStats()`에 클래스 보너스 적용
- `Assets/Scripts/Gameplay/Tower/SkillData.cs` — `public JobClass requiredClass = JobClass.None` 추가
- `Assets/Scripts/Systems/InventorySystem.cs` — `EquipSkill()`에 직업 검증 추가

## 3. 신규 클래스 / 파일

- `Assets/Scripts/Gameplay/Player/JobClass.cs` — `JobClass` enum (None, Warrior, Mage, Archer)
- `Assets/Scripts/UI/JobClassDisplayUI.cs` — UnitPanel 내 직업 이름 TextMeshPro 컴포넌트 제어

## 4. 테스트 계획

- [ ] 전사 Tower 프리팹에 jobClass = Warrior 설정 후 RefreshStats 스탯 확인
- [ ] 마법사 Tower에 Fireball 스킬 장착 성공, MoltenStrike 장착 실패 확인
- [ ] 궁수 Tower에 LightningArrow 장착 성공, Fireball 장착 실패 확인
- [ ] JobClass.None Tower는 모든 스킬 장착 가능
- [ ] 유닛 패널 선택 시 직업 이름 올바르게 표시

## 5. 위험 요소

- 기존 Tower 프리팹들은 `jobClass = None`으로 초기화 → 하위 호환 유지 (스킬 제한 없음)
- SkillData ScriptableObject에 `requiredClass` 필드 추가 시 기존 에셋은 None(0)으로 자동 초기화 → 하위 호환 유지
- JobClassDisplayUI는 TextMeshPro 컴포넌트를 참조하므로 씬에서 TMP 오브젝트 연결 필요
