# Issue #62 — [Feat] 데미지 표시 기능

## 1. 시스템 구조

```
Enemy.TakeDamage(damage, armorPen, isCrit=false)
  → actual 데미지 계산
  → GameUIManager.ShowDamage(transform.position, actual, isCrit)
      → _damageTexts 리스트에 추가

GameUIManager.OnGUI()
  → _damageTexts 순회
  → Camera.WorldToScreenPoint + 왼쪽 하단 오프셋
  → 시간에 따라 위로 부유 (floatSpeed)
  → 일반: 검은색, 크리티컬: 빨간색
  → 만료된 항목 제거
```

**크리티컬 판정 전달:**
- `Enemy.TakeDamage()`에 `bool isCrit = false` 선택적 파라미터 추가
- 각 프로젝타일 `OnHit()`에서 크리티컬 판정 시 `isCrit: true` 전달
- 스플래시 데미지 / DoT는 isCrit=false (기본값)

**영향 시스템:**
- `Enemy.cs` — `TakeDamage` 시그니처 변경 + `GameUIManager.ShowDamage` 호출
- `GameUIManager.cs` — `DamageText` 구조체, `_damageTexts` 리스트, `ShowDamage` 정적 메서드, `OnGUI` 확장
- `PreciseArrowProjectile.cs` — `TakeDamage(..., isCrit)` 전달
- `FreezingPulseProjectile.cs` — `TakeDamage(..., isCrit)` 전달
- `LightningArrowProjectile.cs` — `TakeDamage(..., isCrit)` 전달
- `FireballProjectile.cs` — 기존 크리티컬 없음 (LaunchFireball에서 crit 계산 → proj에 전달 필요)

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Enemy/Enemy.cs` | `TakeDamage(float damage, float armorPenRatio = 0f, bool isCrit = false)` + `ShowDamage` 호출 |
| `Assets/Scripts/Systems/GameUIManager.cs` | `DamageText` 구조체, `ShowDamage` 정적 메서드, `OnGUI` 부유 텍스트 렌더링 |
| `Assets/Scripts/Gameplay/Skills/Projectiles/PreciseArrowProjectile.cs` | `target.TakeDamage(dmg, _armorPen, isCrit)` |
| `Assets/Scripts/Gameplay/Skills/Projectiles/FreezingPulseProjectile.cs` | `target.TakeDamage(dmg, _armorPen, isCrit)` |
| `Assets/Scripts/Gameplay/Skills/Projectiles/LightningArrowProjectile.cs` | `e.TakeDamage(dmg, _armorPen, isCrit)` |
| `Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs` | `LaunchFireball` 크리티컬 결과를 `FireballProjectile`에 전달 |
| `Assets/Scripts/Gameplay/Skills/Projectiles/FireballProjectile.cs` | `IsCrit` 프로퍼티 추가, `TakeDamage(..., IsCrit)` 전달 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] 적 피격 시 왼쪽 하단에 데미지 숫자 표시 확인
- [ ] 일반 피격: 검은색 텍스트 확인
- [ ] 크리티컬 피격: 빨간색 텍스트 확인
- [ ] 텍스트가 위로 부유 후 사라짐 확인
- [ ] 스플래시 피격 적에도 데미지 표시 확인
- [ ] DoT(CausticGround) 피격 시 데미지 표시 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| TakeDamage 시그니처 변경 | 기존 호출부 모두 isCrit=false 기본값으로 동작 | 선택적 파라미터로 하위 호환 유지 |
| 동시 다발 데미지 텍스트 | 대량의 텍스트가 겹쳐 보일 수 있음 | 위치 오프셋 랜덤 ±x 추가 또는 그대로 허용 |
| OnGUI GC | 매 프레임 string 생성 | 만료된 항목만 제거, 활성 항목만 렌더링 |
| GameUIManager null | ShowDamage 호출 시 instance 없으면 무시 | null 체크 |
