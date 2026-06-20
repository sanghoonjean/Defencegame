# Issue #186 / #187 — 번개·독 저항 시스템 구현

## 1. 시스템 구조

```
DamageType (enum)
  └─ Physical / Fire / Energy / Lightning(#186) / Poison(#187)

EnemyData (ScriptableObject)
  └─ fireResistance / lightningResistance / poisonResistance

Enemy (MonoBehaviour)
  ├─ Initialize: 각 resistance 필드 Clamp 초기화
  └─ TakeDamage: switch expression으로 타입별 저항 적용

LightningArrowProjectile → TakeDamage(..., DamageType.Lightning)
CausticGround.ApplyDot   → TakeDamage(..., DamageType.Poison)

GameUIManager
  ├─ _lightningDmgStyle: 노란색 Color(1f, 0.95f, 0.2f)
  └─ _poisonDmgStyle:    초록색 Color(0.3f, 0.9f, 0.3f)
```

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/DamageType.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/EnemyData.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Enemy/Enemy.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/Projectiles/LightningArrowProjectile.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Skills/CausticGround.cs`
- `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs`

## 3. 신규 클래스 / 파일

없음 (기존 파일 수정만)

## 4. 테스트 계획

- [ ] DamageType enum 컴파일 확인
- [ ] LightningResistance=0.5 → Lightning 100 → 50 피해
- [ ] LightningResistance=-0.25 → Lightning 100 → 125 피해
- [ ] PoisonResistance=0.5 → Poison 100 → 50 피해
- [ ] PoisonResistance=-0.25 → Poison 100 → 125 피해
- [ ] Physical 피해 → 저항 미적용 확인
- [ ] GameUIManager: Lightning=노란색, Poison=초록색 데미지 텍스트

## 5. 위험 요소

- **PR #191 충돌**: PR #191이 Enemy.cs TakeDamage를 switch expression으로 변경. 동일 패턴 사용으로 충돌 최소화.
- **Unity ScriptableObject 직렬화**: 기존 EnemyData 에셋에 새 필드 기본값(0) 자동 적용 → 기존 동작 유지.
