# Issue #394 — 차원석 등급 기능 추가

## 1. 시스템 구조

차원석에 등급(`StoneGrade`)을 도입한다. 등급은 획득 시점에 확률 테이블로 결정되고,
등급이 높을수록 옵션 수치가 높은 구간에서 roll 되며, 슬롯 아이콘 우하단에 등급
숫자를 표시한다.

### 등급 결정 (확률 테이블 랜덤)

```
Normal 60% / Magic 25% / Rare 12% / Unique 3%   (잠정값 — Inspector 튜닝 가능)
```

- `StoneGradeTable` (Serializable): 확률 필드 + `Resolve(float roll01)` — 누적 구간
  방식의 결정적 함수 (테스트 가능). `DroppedStoneSystem` 이 `[SerializeField]` 로 보유.
- 획득 진입점은 `DroppedStoneSystem.CollectAll()` / `GrantClearBonus()` 두 곳 —
  등급 roll 후 `DimensionStone.CreateRandom(grade)` 호출.
- 기존 `CreateRandom()` (무인자) 은 기본 확률로 등급을 자체 roll — 하위 호환 유지.

### 등급 효과 (옵션 수치 범위 상향)

옵션 roll 시 최솟값이 등급에 따라 상향된다 (max 는 고정, 시작 옵션 수는 기존대로 1개).

```
effectiveMin = Lerp(min, max, gradeFloor)
gradeFloor: Normal 0 / Magic 0.25 / Rare 0.5 / Unique 0.75   (잠정값)
예) MonsterHpBoost (5~30%): Normal 5~30 / Magic 11.25~30 / Rare 17.5~30 / Unique 23.75~30
```

- `RollOption` / `AddRandomOption` / `Reroll` 모두 등급 반영 (Reroll 은 등급 유지)
- `UpgradeRandomOption` (1.5배, max clamp) 은 변경 없음
- `Clone` 은 등급 복사
- `RiftWaveModifiers.FromOptions` 는 변경 없음 (옵션 값만 소비)

### UI 표시 (아이콘 우하단 숫자)

- 슬롯의 차원석 아이콘 오른쪽 아래에 등급 숫자 `1`~`4` 표시 (Normal=1 … Unique=4)
- 씬 수정 없이 **코드에서 런타임 생성**: ICON Image 의 자식으로 소형 `Text` 를
  생성/캐싱하는 헬퍼 컴포넌트 `StoneGradeBadge` — Kind == Stone 일 때만 표시
- 적용 지점: 인벤 슬롯 (`InvenUI` 바인딩) + 장착 슬롯 (Generate 슬롯 표시 갱신부)
- 숫자만 표시하므로 한글 TMP 폰트 이슈 없음 (uGUI 기본 폰트)

### 데이터 흐름

```text
Enemy 처치 / 웨이브 클리어 (DroppedStoneSystem)
 ↓
StoneGradeTable.Resolve(random01) → StoneGrade
 ↓
DimensionStone.CreateRandom(grade) — gradeFloor 반영된 범위에서 옵션 roll
 ↓
ShopSystem.AddStone → 인벤 슬롯 바인딩
 ↓
StoneGradeBadge — 아이콘 우하단에 등급 숫자 표시
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Rift/Core/DimensionStone.cs`
  - `StoneGrade Grade` 프로퍼티 추가 (생성 시 확정, Clone/Reroll 유지)
  - `CreateRandom(StoneGrade)` 오버로드, `RollOption` 에 gradeFloor 적용
- `MakeDefence/Assets/Scripts/Systems/DroppedStoneSystem.cs`
  - `[SerializeField] StoneGradeTable gradeTable` 추가, 획득 2개소에서 등급 roll
- `MakeDefence/Assets/Scripts/UI/InvenUI.cs`
  - 슬롯 바인딩 시 `StoneGradeBadge` 갱신 (Stone 일 때만 표시)
- 장착 슬롯 표시부 (`WaveGeneratorSystem` 의 Generate 슬롯 아이콘 갱신 위치 — 구현 시 확인)
  - 장착된 차원석에도 동일 배지 적용

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Gameplay/Rift/Core/StoneGrade.cs`
  - `enum StoneGrade { Normal, Magic, Rare, Unique }`
- `MakeDefence/Assets/Scripts/Gameplay/Rift/Core/StoneGradeTable.cs`
  - Serializable 확률 테이블 + `Resolve(float roll01)` (결정적, 테스트 대상)
  - 기본값: 0.60 / 0.25 / 0.12 / 0.03
- `MakeDefence/Assets/Scripts/UI/StoneGradeBadge.cs`
  - ICON Image 자식으로 등급 숫자 Text 를 런타임 생성/표시/숨김하는 헬퍼

## 4. 테스트 계획

### EditMode 자동 테스트 (MakeDefence.Rift.Core — 테스트 asmdef 에서 참조 가능)
- [ ] `StoneGradeTableTests`: 누적 구간 경계값 (0.0/0.6/0.85/0.97/1.0 부근) 별 등급,
      확률 합이 1 이 아니어도 마지막 등급으로 fallback
- [ ] `DimensionStoneTests` 확장: `CreateRandom(grade)` 의 Grade 저장,
      Unique 등급 옵션 값이 gradeFloor 반영 최솟값 이상, Clone/Reroll 등급 유지
- [ ] 기존 테스트 34개 회귀 없음

### 수동/에디터 검증 (MonoBehaviour·UI 의존)
- [ ] 컴파일 에러/경고 없음 (read_console)
- [ ] execute_code: 웨이브 클리어 시뮬레이션 없이 `CreateRandom(Unique)` 값 분포 확인
- [ ] 플레이 모드: 웨이브 클리어 → 인벤 슬롯 아이콘 우하단에 등급 숫자 표시 확인
- [ ] 차원석 드래그(인벤 ↔ 장착 슬롯) 시 배지가 잔상 없이 갱신되는지
- [ ] 스킬/서포트 슬롯에는 배지가 표시되지 않는지

## 5. 위험 요소

- **하위 호환**: 기존 `CreateRandom()` 호출처(DroppedStoneSystem 2개소)가 등급 roll 을
  거치도록 변경 — 저장/직렬화된 차원석은 없어 (런타임 전용) 마이그레이션 불필요
- **기존 테스트 영향**: `DimensionStoneTests` 가 `CreateRandom()` 을 사용 — 등급이
  랜덤이어도 옵션 수/CRUD 동작은 불변이므로 회귀 없음 예상, 실행으로 확인
- **UI 배지 런타임 생성**: 씬 수정을 피하는 대신 코드 생성 — 드래그 고스트/DropTarget
  하이라이트와 z 순서 충돌 여부는 플레이 모드에서 확인
- **수치 밸런스**: 확률/gradeFloor 는 잠정값 — Inspector 튜닝 가능하게 SerializeField 유지
- 등급별 아이콘 색상/이름 표기는 스코프 제외 (숫자 배지만)
