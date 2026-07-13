# Issue #388 — 몬스터 레벨 기능 추가

## 1. 시스템 구조

몬스터에 레벨 개념을 도입하고, 레벨을 스탯 스케일링의 단일 기준으로 삼는다.

### 레벨 산정 규칙

```
레벨 = 스테이지 + 등급 보너스
등급 보너스: Normal +0 / Magic +1 / Rare +2 / Unique +3
LastBoss(fixedStats): 난이도 공식 미적용 → 레벨은 표시용으로만 스테이지 값 사용
```

### 스탯 스케일링 치환

현재 `Enemy.Initialize()` 는 stage 를 직접 사용한다.

```
현재:  hpMult = 1 + stage * 0.05, defMult = 1 + stage * 0.05, speedMult = 1 + stage * 0.02
변경:  hpMult = 1 + level * 0.05, defMult = 1 + level * 0.05, speedMult = 1 + level * 0.02
```

- Normal 몬스터는 `level == stage` 이므로 밸런스 변화 없음
- Magic/Rare/Unique 는 등급 보너스만큼 배율이 소폭 상승 (예: stage 5 Rare → 레벨 7 → HP 배율 1.25 → 1.35)
- Rift 웨이브 배율(`RiftWaveModifiers`)은 기존과 동일하게 레벨 배율 위에 곱 적용 — 변경 없음

### 레벨 표시 (HP 바 연동)

`GameUIManager.OnGUI()` 의 IMGUI HP 바 루프에서 각 몬스터의 HP 바 왼쪽에 `Lv.N`
라벨을 `GUI.Label` 로 그린다.

- 표기는 영어(`Lv.N`) — 한글 TMP 폰트 부재 이슈와 무관한 IMGUI 이지만, 표기 일관성 유지
- HP 바와 동일한 blocker(열린 UI 패널) 가림 규칙 적용
- 라벨 스타일은 줌(pixelsPerUnit)에 비례한 폰트 크기로 계산해 확대/축소와 자연스럽게 연동

### 데이터 흐름

```text
WaveSystem.SpawnEnemies (stage, grade)
 ↓
EnemyLevel.Calculate(stage, grade) → level
 ↓
Enemy.Initialize — level 저장 + 레벨 기반 스탯 배율 적용
 ↓
GameUIManager.OnGUI — enemy.Level 을 HP 바 옆 Lv.N 라벨로 표시
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
  - `public int Level { get; private set; }` 추가
  - `Initialize()` 에서 `EnemyLevel.Calculate()` 로 레벨 산정 후 stage 대신 level 로 배율 계산
  - fixedStats 경로에서도 표시용 레벨(= stage) 저장
- `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs`
  - `OnGUI()` HP 바 루프에 `Lv.N` 라벨 렌더링 추가 (GUIStyle 캐싱, blocker 가림 적용)
  - 라벨 표시 여부 토글용 `[SerializeField] bool showEnemyLevel = true`

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Gameplay/Enemy/EnemyLevel.cs`
  - 정적 헬퍼: `Calculate(int stage, EnemyGrade grade)` — 레벨 산정 공식 단일 소스
  - 등급 보너스 상수 보관 (Normal 0 / Magic 1 / Rare 2 / Unique 3 / LastBoss 0)

## 4. 테스트 계획

- [ ] 컴파일 에러 없음 (UnityMCP `read_console` 확인)
- [ ] 스테이지 1 일반 웨이브: Normal 몬스터 Lv.1, Magic Lv.2 표시 확인
- [ ] 스테이지 5 이상: Rare Lv.(stage+2) 표시 및 HP 상승 확인 (스폰 로그/인스펙터)
- [ ] 스테이지 10 이상: Unique Lv.(stage+3) 확인
- [ ] Rift 웨이브: 레벨 배율 위에 차원석 배율이 곱으로 적용되는지 확인
- [ ] UI 패널(ItemHubPanel 등) 열었을 때 라벨도 HP 바와 함께 가려지는지 확인
- [ ] 카메라 줌 인/아웃 시 라벨 크기가 스프라이트와 같은 비율로 변하는지 확인

## 5. 위험 요소

- **밸런스 변화**: Magic/Rare/Unique 몬스터가 등급 보너스만큼 강해짐 (승인된 설계).
  등급 몬스터의 baseHp 가 이미 높다면 고스테이지에서 체감 난이도 상승 폭 확인 필요
- **IMGUI 라벨 부하**: 몬스터 90마리 × GUI.Label — GUIStyle 을 매 프레임 생성하지 않고
  캐싱하면 기존 HP 바와 비슷한 수준의 비용
- **표시 겹침**: HP 바 왼쪽 공간이 부족한 화면 가장자리에서 라벨이 잘릴 수 있음 —
  구현 시 라벨 위치(왼쪽 vs 바 위쪽) 는 실제 화면 확인 후 조정 가능
- LastBoss(fixedStats) 는 스탯 공식 미적용 유지 — 레벨은 표시 전용
