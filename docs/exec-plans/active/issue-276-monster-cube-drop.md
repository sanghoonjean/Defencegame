# Issue #276 — 몬스터 처치 시 큐브 드랍 기능

## 1. 시스템 구조

```
Enemy.Die()
   │
   │ (이벤트) OnEnemyDied(Enemy)
   ▼
CubeSystem.HandleEnemyDied(Enemy)
   │
   │ 1) Grade → Drop chance + Drop count 결정
   │ 2) 확률 롤
   │ 3) 성공 시 RollDrop() × count 만큼 Add()
   ▼
CubeSystem._counts[type] 증가 → OnCubeChanged 이벤트
   │
   ▼
CubeUIDisplay 갱신 (기존 경로 그대로)
```

기존 `CubeSystem.HandleWaveEnded` 와 동일한 구조의 이벤트 핸들러를 하나 추가하는 형태.
신규 클래스 / 신규 ScriptableObject 없이 **CubeSystem 한 곳만 확장**한다.

### 결합도
- `Enemy` 는 `OnEnemyDied` 이벤트만 발행 (기존과 동일, 변경 없음).
- `CubeSystem` 이 이벤트 구독 추가 + Grade 분기 로직 추가.
- `EnemyData` / `Enemy` 클래스에는 드랍 관련 필드 추가하지 않음 (Grade 정보만으로 분기 가능).

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/CubeSystem.cs`
  - `OnEnable` / `OnDisable` 에 `Enemy.OnEnemyDied` 구독 / 해제 추가
  - Grade별 드랍 확률 / 드랍 수 SerializeField 추가
  - `HandleEnemyDied(Enemy enemy)` 메서드 추가
  - `RollDrop()` 은 기존 그대로 재사용

## 3. 신규 클래스 / 파일

없음. CubeSystem 내부 필드 / 메서드 추가만으로 완결.

### 추가 필드 (CubeSystem)
```csharp
[Header("처치 시 드랍 확률 (0~1)")]
[SerializeField, Range(0f, 1f)] private float normalKillDropChance   = 0.08f;
[SerializeField, Range(0f, 1f)] private float magicKillDropChance    = 0.20f;
[SerializeField, Range(0f, 1f)] private float rareKillDropChance     = 0.40f;
[SerializeField, Range(0f, 1f)] private float uniqueKillDropChance   = 1.0f;
[SerializeField, Range(0f, 1f)] private float lastBossKillDropChance = 1.0f;

[Header("처치 시 드랍 개수")]
[SerializeField] private int normalKillDropCount   = 1;
[SerializeField] private int magicKillDropCount    = 1;
[SerializeField] private int rareKillDropCount     = 1;
[SerializeField] private int uniqueKillDropCount   = 1;
[SerializeField] private int lastBossKillDropCount = 3;
```

### 추가 메서드 (의사 코드)
```csharp
private void HandleEnemyDied(Enemy enemy)
{
    (float chance, int count) = enemy.Grade switch
    {
        EnemyGrade.Normal   => (normalKillDropChance,   normalKillDropCount),
        EnemyGrade.Magic    => (magicKillDropChance,    magicKillDropCount),
        EnemyGrade.Rare     => (rareKillDropChance,     rareKillDropCount),
        EnemyGrade.Unique   => (uniqueKillDropChance,   uniqueKillDropCount),
        EnemyGrade.LastBoss => (lastBossKillDropChance, lastBossKillDropCount),
        _                   => (0f, 0),
    };
    if (count <= 0 || chance <= 0f) return;
    if (UnityEngine.Random.value > chance) return;
    for (int i = 0; i < count; i++) Add(RollDrop(), 1);
}
```

## 4. 테스트 계획

### 수동 검증 (Unity Play)
1. **Normal 적 다수 처치** → 드랍 카운트가 8% 근사로 누적되는지 확인
2. **Rare 적 처치** → 40% 근사로 드랍, 큐브 UI 즉시 갱신
3. **Unique 적 처치** → 매번 1개 드랍
4. **LastBoss 처치** → 항상 3개 드랍
5. **베이스 도달 적** → 드랍 없음 (PlayerSystem 데미지만 적용)
6. **웨이브 종료** → 기존 `HandleWaveEnded` 드랍이 여전히 작동 (몬스터 드랍과 중복 가능)
7. **씬 전환 / 재시작** → `OnDisable` 에서 구독 해제로 이중 호출 없음

### 회귀 체크
- 기존 웨이브 종료 일괄 드랍 동작 (`DropReward`) 변경 없음
- CubeSystem UI 표시 경로 (`OnCubeChanged`) 변경 없음

## 5. 위험 요소

- **후반 인플레이션**: 다수 적이 동시 사망하는 후반 스테이지에서 큐브가 빠르게 쌓일 수 있음. 초기값은 보수적으로(Normal 8%) 시작하고, 플레이테스트 후 조정.
- **확률 결정성**: 보스 등 핵심 적의 드랍이 매번 가변이면 기획 의도 어긋날 수 있음 → Unique/LastBoss 는 100% 보장.
- **시각 피드백 부재**: 이번 범위에는 드랍 팝업/이펙트 미포함. 카운터 증가만으로는 플레이어 인지가 약할 수 있음 → **별도 이슈로 분리**.
- **`RollDrop()` 가중치 공유**: per-kill 도 wave-end 와 동일 가중치를 쓰므로, 향후 분리가 필요해질 가능성 있음. 현 단계에서는 단순화 우선.
- **테스트 자동화 부재**: 현재 프로젝트에 자동 테스트 인프라가 작아, 수동 Play 모드 검증에 의존.
