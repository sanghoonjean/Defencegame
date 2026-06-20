# Issue #166 — 타워 공격 애니메이션 적용

## 1. 시스템 구조

`Tower.Attack()` → `SkillDispatcher.Execute()` 호출 흐름에서,
공격 직전 `Animator.SetTrigger("Attack")`를 발동하면 된다.

- `_animator` 필드를 `GetComponent<Animator>()`로 캐싱
- Animator가 없는 타워는 null 체크로 무시 (기존 동작 유지)
- 애니메이션 속도는 `baseAttackCooldown / AttackCooldown` 비율로 연동
  - 공격 속도 2배 → animator.speed = 2.0

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

### Tower.cs — Animator 필드 추가 및 캐싱

```csharp
private Animator _animator;

private void Awake()
{
    _animator = GetComponent<Animator>();
    RefreshStats();
}
```

### RefreshStats — animator speed 동기화

```csharp
if (_animator != null)
    _animator.speed = baseAttackCooldown / Mathf.Max(0.01f, AttackCooldown);
```

### Attack() — 트리거 발동

```csharp
private void Attack(Enemy target)
{
    _animator?.SetTrigger("Attack");
    SkillDispatcher.Execute(this, target);
    TryDropCube();
}
```

### Animator 파라미터 규약

| 파라미터명 | 타입    | 용도           |
|-----------|---------|----------------|
| `Attack`  | Trigger | 공격 애니메이션 재생 |

## 5. 테스트 계획

- [ ] Animator 연결된 타워 공격 시 애니메이션 재생 확인
- [ ] AttackSpeed 버프 적용 후 애니메이션 속도 증가 확인
- [ ] Animator 미연결 타워 오류 없이 동작 확인

## 6. 위험 요소

- Animator Controller / 클립은 Unity에서 별도 설정 필요 (코드만으로 완결 불가)
- `RefreshStats` 호출 빈도가 높으면 `animator.speed` 재설정이 잦을 수 있으나, 성능 영향 미미
