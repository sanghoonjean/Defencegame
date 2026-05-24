# Issue #153 — 물리 + 불꽃 데미지 팝업 겹침 수정

## 1. 시스템 구조

### 문제 흐름
```
Enemy.TakeDamage(100, Physical)
  → GameUIManager.ShowDamage(transform.position, 100)  // worldPos 고정

Enemy.TakeDamage(30, Fire)
  → GameUIManager.ShowDamage(transform.position, 30)   // 동일 worldPos → 겹침
```

`ShowDamage`가 항상 `enemy.transform.position`을 worldPos로 받기 때문에,
같은 프레임(또는 짧은 시간 내)에 두 번 호출되면 동일 좌표에 팝업이 중첩 표시.

### 수정 흐름
```
ShowDamage(pos, 100) → extraYOffset = 0   → 기존 위치
ShowDamage(pos, 30)  → extraYOffset = 15  → 15px 위로 분리
```

동일 위치(sqrMagnitude < 0.01)에서 0.1초 이내 기존 팝업이 있으면
새 팝업의 Y 오프셋을 누적해 분리.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

### DamageText 구조체에 필드 추가
```csharp
private struct DamageText
{
    public Vector2 worldPos;
    public string  text;
    public bool    isCrit;
    public float   startTime;
    public float   expireTime;
    public float   extraYOffset;  // 추가
}
```

### ShowDamage 수정
```csharp
public static void ShowDamage(Vector2 worldPos, float damage, bool isCrit)
{
    if (_instance == null) return;

    float yExtra = 0f;
    float now    = Time.time;
    foreach (var d in _instance._damageTexts)
    {
        if ((d.worldPos - worldPos).sqrMagnitude < 0.01f &&
            now - d.startTime < 0.1f)
            yExtra += 15f;
    }

    _instance._damageTexts.Add(new DamageText
    {
        worldPos     = worldPos,
        text         = Mathf.RoundToInt(damage).ToString(),
        isCrit       = isCrit,
        startTime    = now,
        expireTime   = now + _instance.dmgDuration,
        extraYOffset = yExtra
    });
}
```

### DrawDamageTexts 수정
```csharp
float sy = Screen.height - sp.y + dmgYOffset + d.extraYOffset
           - progress * dmgFloatSpeed;
```

## 5. 테스트 계획

- [ ] IncendiaryRound 장착 타워 → 적 공격 → 100 / 30 팝업이 분리되어 표시
- [ ] IncendiaryRound 미장착 → 팝업 1개만 표시 (기존과 동일)
- [ ] 크리티컬 + 불꽃 동시 → 각각 올바른 색상(빨강/검정)으로 분리 표시
- [ ] Fireball 스플래시 → 여러 적에게 각각 정상 표시

## 6. 위험 요소

- `_damageTexts` 순회 중 추가 → foreach 사용이므로 안전 (Add는 순회 후)
- 오프셋 누적으로 팝업이 화면 밖으로 나갈 수 있음 — 최대 2~3개 동시 발생 구조상 문제 없음
