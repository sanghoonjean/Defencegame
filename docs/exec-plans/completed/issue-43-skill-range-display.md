# Issue #43 — [FEAT] 공격 스킬의 공격 범위 표시

## 1. 시스템 구조

```
ProjectileBase.OnHit()
  → (AoE 프로젝타일) GameUIManager.ShowAoeHit(pos, radius)
      → _aoeCircles 큐에 (pos, radius, expireTime) 추가

GameUIManager.OnRenderObject()
  → _aoeCircles 순회
  → 만료되지 않은 원을 GL.LINES로 렌더링
  → 만료된 항목 제거
```

**핵심 메커니즘:**
- `GameUIManager.ShowAoeHit(Vector2 pos, float radius, float duration=0.5f)` 정적 메서드 추가
- 내부 `_aoeCircles` 리스트에 `(pos, radius, expireTime)` 구조체 저장
- `OnRenderObject()`에서 만료 시간 비교 후 렌더링 & 정리
- AoE 프로젝타일(`Fireball`, `LightningArrow`, `CausticArrow`) `OnHit()`에서 호출

**표시 대상 스킬:**
- `FireballProjectile` — 착탄 위치 + AoeRadius
- `LightningArrowProjectile` — 착탄 위치 + AoeRadius
- `CausticArrowProjectile` — 착탄 위치 + AoeRadius (장판 범위 시각화)

**영향 시스템:**
- `GameUIManager.cs` — 원 큐 + ShowAoeHit 정적 메서드 + OnRenderObject 확장
- `FireballProjectile.cs` — OnHit에서 ShowAoeHit 호출
- `LightningArrowProjectile.cs` — OnHit에서 ShowAoeHit 호출
- `CausticArrowProjectile.cs` — OnHit에서 ShowAoeHit 호출

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Systems/GameUIManager.cs` | AoeCircle 구조체, _aoeCircles 리스트, ShowAoeHit 정적 메서드, OnRenderObject 확장 |
| `Assets/Scripts/Gameplay/Skills/Projectiles/FireballProjectile.cs` | OnHit에서 `GameUIManager.ShowAoeHit` 호출 |
| `Assets/Scripts/Gameplay/Skills/Projectiles/LightningArrowProjectile.cs` | OnHit에서 `GameUIManager.ShowAoeHit` 호출 |
| `Assets/Scripts/Gameplay/Skills/Projectiles/CausticArrowProjectile.cs` | OnHit에서 `GameUIManager.ShowAoeHit` 호출 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] Fireball 착탄 시 AoE 범위 원 표시 확인
- [ ] LightningArrow 착탄 시 AoE 범위 원 표시 확인
- [ ] CausticArrow 착탄 시 장판 범위 원 표시 확인
- [ ] 원이 duration(0.5초) 후 사라짐 확인
- [ ] 여러 착탄 동시 발생 시 각각 독립적으로 표시 확인
- [ ] PreciseArrow / FreezingPulse (단일 타겟) 착탄 시 원 미표시 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| GameUIManager 미부착 | 씬에 컴포넌트 없으면 정적 인스턴스 null | ShowAoeHit에서 instance null 체크 |
| OnRenderObject 중 리스트 수정 | 렌더링 중 제거 시 예외 | 역순 인덱스 순회로 안전하게 제거 |
| AoeRadius = 0 스킬 | 원 미표시가 맞음 | radius <= 0 조건 체크로 스킵 |
| .meta / 씬 파일 | AGENTS.md 규칙상 수동 생성 금지 | 코드만 수정 |
