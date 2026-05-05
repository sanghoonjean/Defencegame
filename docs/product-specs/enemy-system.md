# 기능 스펙: EnemySystem

## 개요
웨이브 시작 시 등급별 적을 스폰하고,
웨이포인트를 따라 이동하여 기지에 도달하면 플레이어 HP를 감소시킨다.

---

## 기능 요구사항

### 1. 적 등급 및 기본 스탯 (웨이브 1단계 기준)

| 등급 | HP | 방어력 | 이동속도 | 플레이어 데미지 |
|------|----|--------|----------|-----------------|
| normal | 1000 | 50 | 1 | 2 |
| magic | 1050 | 52 | 1 | 4 |
| rare | 1102 | 55 | 1 | 6 |
| Unique | 1157 | 57 | 1 | 10 |
| Last Boss | 5000 | 200 | 2 | 100 |

> 소수점 이하 절삭 / Last Boss는 고정 스탯 (난이도 공식 미적용)

### 2. 난이도별 스탯 공식
```
HP       = 기준 HP       * (1 + stage * 0.05)
방어력   = 기준 방어력   * (1 + stage * 0.05)
이동속도 = 기준 이동속도 * (1 + stage * 0.02)
```

### 3. 스폰
- WaveSystem이 요청한 등급/수량대로 ObjectPool에서 꺼내 스폰 포인트에 생성
- 스폰 간격: 일정 시간마다 순차 생성 (간격 미확정)

### 4. 이동
- PathfindingSystem이 제공하는 웨이포인트 순서대로 이동
- 이동속도는 등급별 스탯 적용
- 기지 도달 시 PlayerSystem에 데미지 전달 후 오브젝트 풀로 반납

### 5. 피격 및 사망
- 타워 공격 적중 시 HP 감소
- 방어력만큼 데미지 감소: `실제 데미지 = 공격력 - 방어력` (최소 1)
- HP 0 이하 시 사망 → VFXSystem, SoundSystem 이벤트 발생 후 풀 반납

### 6. 특수 능력
- 현재 없음 (추후 등급별 추가 예정)

---

## 인터페이스

```csharp
public class Enemy
{
    public EnemyGrade Grade { get; }
    public float CurrentHp { get; }
    public void TakeDamage(float damage);

    public static event Action<Enemy> OnEnemyDied;
    public static event Action<Enemy> OnEnemyReachedBase;
}

public enum EnemyGrade
{
    Normal, Magic, Rare, Unique, LastBoss
}
```

---

## 검증 조건
- [ ] 등급별 기본 스탯이 표와 일치
- [ ] 난이도 공식 적용 시 스탯 정확히 계산 (소수점 절삭)
- [ ] 웨이포인트 순서대로 이동
- [ ] 기지 도달 시 플레이어 HP 감소
- [ ] 사망 시 ObjectPool로 반납
- [ ] Last Boss 스탯 고정값 적용
