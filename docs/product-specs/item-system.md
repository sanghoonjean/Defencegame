# 기능 스펙: ItemSystem

## 개요
타워별 아이템 슬롯을 관리하고,
큐브를 소모해 아이템 옵션을 추가/제거/리롤/교체/복제한다.

---

## 기능 요구사항

### 1. 아이템 슬롯
- 타워당 최대 3개 슬롯 (하위 큐브로 순서대로 해금)
- 슬롯 해금 비용:

| 슬롯 | 비용 |
|------|------|
| 1번째 | 하위 큐브 10개 |
| 2번째 | 하위 큐브 20개 |
| 3번째 | 하위 큐브 30개 |

- 슬롯 해금 시 기본 아이템 자동 생성 (랜덤 옵션 1개 부여)

### 2. 아이템 옵션

- 아이템 등급 없음
- 옵션 수: 최소 1개 ~ 최대 6개
- 동일 옵션 중복 불가

**옵션 테이블 (9종):**

| 옵션 | 효과 |
|------|------|
| 공격력 | 타워 기본 공격력 증가 |
| 공격속도 | 공격 주기 감소 |
| 공격범위 | 공격 사거리 증가 |
| 스턴확률 | 적 기절 확률 증가 |
| 치명타 확률 | 치명타 발생 확률 증가 |
| 치명타 데미지 | 치명타 시 추가 데미지 증가 |
| 관통력 | 적 방어력 무시 % |
| 스킬 쿨타임 감소 | 공격 스킬 쿨타임 감소 |
| 하위 큐브 드롭률 | 적 처치 시 하위 큐브 드롭 확률 증가 |

### 3. 큐브 크래프팅 효과

| 큐브 | 효과 | 제약 |
|------|------|------|
| 하위 큐브 | 옵션 전체 리롤 | 옵션 수 유지, 종류만 재랜덤 |
| 상위 큐브 | 옵션 1개 추가 | 최대 6개 초과 불가 |
| 최상위 큐브 | 옵션 1개 랜덤 제거 → 상위 옵션 교체 | 최소 1개 유지 |
| 삭제 큐브 | 옵션 1개 랜덤 제거 | 최소 1개 유지 |
| 복제형 큐브 | 아이템 복제 → 다른 슬롯에 생성 | 빈 슬롯 필요 |

### 4. 타워 스탯 반영
- 아이템 옵션 변경 시 즉시 TowerSystem.RefreshStats() 호출
- 모든 슬롯 옵션 합산하여 타워 최종 스탯 계산

---

## 인터페이스

```csharp
public class ItemSystem
{
    public bool UnlockSlot(Tower tower, int slot);      // 하위 큐브 소모
    public void ApplyCube(CubeType cube, Tower tower, int slot);
    public ItemData GetItem(Tower tower, int slot);

    public static event Action<Tower> OnItemChanged;
}

public class ItemData
{
    public List<ItemOption> Options { get; }
    public int MaxOptions => 6;
}

public class ItemOption
{
    public ItemOptionType Type { get; }
    public float Value { get; }
}

public enum ItemOptionType
{
    AttackPower, AttackSpeed, AttackRange, StunChance,
    CritChance, CritDamage, ArmorPenetration,
    SkillCooldownReduce, CubeDropRate
}
```

---

## 검증 조건
- [ ] 슬롯 해금 시 정확한 큐브 소모
- [ ] 슬롯 해금 시 옵션 1개 랜덤 부여
- [ ] 동일 옵션 중복 부여 불가
- [ ] 옵션 6개 초과 시 상위 큐브 사용 불가
- [ ] 옵션 1개일 때 삭제 큐브/최상위 큐브 적용 후 최소 1개 유지
- [ ] 복제 시 빈 슬롯 없으면 실패 처리
- [ ] 옵션 변경 시 타워 스탯 즉시 반영
