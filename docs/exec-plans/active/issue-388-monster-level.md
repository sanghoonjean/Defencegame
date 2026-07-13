# Issue #388 — 몬스터 레벨 기능 추가

## 1. 시스템 구조

몬스터에 레벨 개념을 도입하고, 레벨을 스탯 스케일링의 단일 기준으로 삼는다.
레벨의 UI 표시는 하지 않는다 (내부 스탯 로직 전용).

### 레벨 산정 규칙

```
레벨 = 스테이지 + 등급 보너스
등급 보너스: Normal +0 / Magic +1 / Rare +2 / Unique +3
LastBoss(fixedStats): 난이도 공식 미적용 → 레벨은 참조용으로만 스테이지 값 저장
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

### 데이터 흐름

```text
WaveSystem.SpawnEnemies (stage, grade)
 ↓
EnemyLevel.Calculate(stage, grade) → level
 ↓
Enemy.Initialize — level 저장 + 레벨 기반 스탯 배율 적용
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
  - `public int Level { get; private set; }` 추가 (외부 조회/향후 확장용)
  - `Initialize()` 에서 `EnemyLevel.Calculate()` 로 레벨 산정 후 stage 대신 level 로 배율 계산
  - fixedStats 경로에서도 참조용 레벨(= stage) 저장

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Gameplay/Enemy/EnemyLevel.cs`
  - 정적 헬퍼: `Calculate(int stage, EnemyGrade grade)` — 레벨 산정 공식 단일 소스
  - 등급 보너스 상수 보관 (Normal 0 / Magic 1 / Rare 2 / Unique 3 / LastBoss 0)

## 4. 테스트 계획

- [ ] 컴파일 에러 없음 (UnityMCP `read_console` 확인)
- [ ] 스테이지 1 일반 웨이브: Normal 레벨 1, Magic 레벨 2 (스폰 로그/인스펙터 확인)
- [ ] 스테이지 5 이상: Rare 레벨 (stage+2) 및 HP 상승 확인
- [ ] 스테이지 10 이상: Unique 레벨 (stage+3) 확인
- [ ] Normal 몬스터 스탯이 기존과 동일한지 확인 (밸런스 불변 검증)
- [ ] Rift 웨이브: 레벨 배율 위에 차원석 배율이 곱으로 적용되는지 확인

## 5. 위험 요소

- **밸런스 변화**: Magic/Rare/Unique 몬스터가 등급 보너스만큼 강해짐 (승인된 설계).
  등급 몬스터의 baseHp 가 이미 높다면 고스테이지에서 체감 난이도 상승 폭 확인 필요
- **UI 미표시**: 레벨이 화면에 노출되지 않으므로 플레이어는 등급 몬스터의 강화를
  수치로 인지할 수 없음 — 추후 표시가 필요해지면 `Enemy.Level` 을 그대로 활용 가능
- LastBoss(fixedStats) 는 스탯 공식 미적용 유지 — 레벨은 참조 전용
