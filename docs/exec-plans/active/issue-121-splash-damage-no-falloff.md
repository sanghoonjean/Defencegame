# Issue #121 — 스플래시 피해 감쇄 제거

## 1. 시스템 구조

`ProjectileBase.ApplySplash()`에서 주변 적에게 주 타겟 데미지의 50%만 적용.

```csharp
float splashDmg = actualDamage * 0.5f;  // 현재 — 50% 감쇄
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/ProjectileBase.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

```csharp
float splashDmg = actualDamage;  // 감쇄 제거 — 주 타겟과 동일 데미지
```

## 5. 테스트 계획

- [ ] Fireball 장착 타워 → 주 타겟 데미지 == 주변 적 데미지 확인
- [ ] SplashRadius = 0인 스킬은 ApplySplash가 호출되지 않아 영향 없음 확인

## 6. 위험 요소

- 스플래시 범위 내 다수 적에게 동일 데미지 적용으로 DPS가 크게 증가할 수 있음
  → 밸런스 조정이 필요하면 Inspector의 SplashRadius로 제어
