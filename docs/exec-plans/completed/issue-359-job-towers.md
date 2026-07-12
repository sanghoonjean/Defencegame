# Issue #359 — 직업별 타워 분리 (전사 / 법사 / 궁수)

> 후속 플랜. 선행 플랜([issue-359-job-class.md](../completed/issue-359-job-class.md))에서
> JobClass enum + 스탯 보너스 + 배치 시 직업 선택 팝업까지 구현 완료(PR #360).
> 본 플랜은 "직업 선택 시 같은 프리팹에 스탯 보너스만 적용" → **"직업별로 실제 다른 타워
> 프리팹이 배치"** 되도록 분리하는 것을 목표로 한다. (방법 A)

## 0. 목표 / 결정 사항

- 스폰 버튼 UI(5개)와 직업 선택 팝업 UI는 **그대로 유지**한다. (사용자 결정)
- 팝업에서 전사/법사/궁수를 고르면, 지금처럼 `SetJob()`으로 스탯만 바꾸는 게 아니라
  **직업 전용 프리팹**(외형·기본 스킬·기본 스탯이 다른)이 배치되도록 한다.
- 직업 → 프리팹 매핑은 **팝업 싱글턴(`JobSelectPopup`)에 집중**시켜, 버튼별 인스펙터
  wiring 없이 한 곳에서만 3개 프리팹을 연결한다.
- 매핑이 비어있으면(프리팹 미연결) 기존 동작(버튼 `unitPrefab` + `SetJob`)으로
  **안전 폴백** → 회귀 방지.

## 1. 시스템 구조

```
UnitSpawnButton (버튼 5개, unitPrefab 유지)
    │  첫 배치 클릭
    ▼
JobSelectPopup.Show(onSelected)      ← UI 그대로
    │  전사/법사/궁수 선택
    ▼
JobSelectPopup.ResolvePrefab(job)    ← [신규] JobClass → Tower 프리팹 매핑
    │
    ├─ 매핑 있음 → 직업 전용 프리팹(Tower_Warrior/Mage/Archer) 배치
    └─ 매핑 없음 → 폴백: 버튼 unitPrefab + SetJob(job)  (기존 동작)
    ▼
TowerPlacer.EnterPlacementMode(prefab, onPlaced)
```

직업별 프리팹은 각자 `jobClass`가 프리팹에 고정 저장되므로, 배치 후 별도 `SetJob`
호출 없이도 `RefreshStats()`의 직업 보너스가 그대로 적용된다.

## 2. 수정 파일

- `Assets/Scripts/UI/JobSelectPopup.cs`
  - `[SerializeField] Tower warriorPrefab / magePrefab / archerPrefab` 추가
  - `public Tower ResolvePrefab(JobClass job)` 추가 (미연결 시 null 반환)
- `Assets/Scripts/UI/UnitSpawnButton.cs`
  - `OnJobSelected(job)` → 팝업에서 직업 프리팹을 조회.
    프리팹이 있으면 그 프리팹으로 `EnterPlacement`(SetJob 불필요),
    없으면 기존 폴백(`unitPrefab` + `SetJob(job)`).
- `Assets/Scenes/SampleScene.unity` (UnityMCP로만 편집)
  - `JobSelectPopup` 컴포넌트에 전사/법사/궁수 프리팹 3개 연결

## 3. 신규 클래스 / 파일

- `Assets/Perfab/Tower_Warrior.prefab` — 전사 전용 타워
  (jobClass=Warrior, 근접 외형, defaultSkill=MoltenStrike 등)
- `Assets/Perfab/Tower_Mage.prefab` — 법사 전용 타워
  (jobClass=Mage, 마법 외형, defaultSkill=Fireball 등, 마나 시스템 활성)
- `Assets/Perfab/Tower_Archer.prefab` — 궁수 전용 타워
  (jobClass=Archer, 원거리 외형, defaultSkill=LightningArrow 등)

> 프리팹은 기존 `Tower.prefab` 기반 Variant로 만들어 컴포넌트/스탯 구조를 재사용한다.
> 외형(스프라이트/애니메이터)·기본 스킬·기본 스탯 수치는 tech-debt로 우선 placeholder,
> 인스펙터에서 조정.

## 4. 테스트 계획

- [ ] 스폰 버튼 클릭 → 팝업 → 전사 선택 시 `Tower_Warrior` 프리팹이 배치되는지
- [ ] 법사/궁수도 각각 전용 프리팹이 배치되는지
- [ ] 배치된 타워의 `Job` 값과 스탯 보너스(전사 데미지/크리, 법사 CD/마나, 궁수 속도/사거리)가 올바른지
- [ ] 유닛 패널 선택 시 직업 이름(전사/법사/궁수) 표시 유지
- [ ] 팝업에 프리팹 미연결 상태에서 기존 폴백(unitPrefab + SetJob)이 동작하는지 (회귀)
- [ ] 재배치(이동 모드)가 직업 프리팹에서도 정상 동작하는지
- [ ] Unity 콘솔 컴파일 에러 0

## 5. 위험 요소

- **버튼 5개 ↔ 직업 3개 매핑 중복**: 현재 5개 버튼이 모두 팝업을 열고 동일한 3개 직업
  프리팹으로 귀결 → 버튼 간 기능 차이가 사라진다. 본 플랜 범위에서는 버튼 UI를 건드리지
  않기로 했으므로(사용자 결정) 그대로 두고, 버튼 정리는 후속 이슈로 분리.
- **프리팹 외형/스킬 미확정**: 직업별 스프라이트·애니메이터·기본 스킬 수치는 미정 →
  placeholder로 두고 인스펙터 조정. tech-debt-tracker에 기록.
- **에셋 편집 경로**: `.prefab`/`.unity`/`.meta`는 UnityMCP 도구로만 생성·편집 (직접 YAML 편집 금지).
- **폴백 경로 유지 필수**: 팝업 프리팹 미연결 시 크래시/널 프리팹 배치가 없도록 폴백 검증.
