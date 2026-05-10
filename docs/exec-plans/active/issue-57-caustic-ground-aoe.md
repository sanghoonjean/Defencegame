# Issue #57 — [FEAT] Caustic Arrow의 Caustic Ground AoE 개선

## 1. 시스템 구조

```
CausticArrowProjectile.OnHit()
  → cg.Init(AoeRadius, ...)     ← radius 전달됨
      CausticGround._radius = radius  ← 데미지 판정에만 사용
      transform.localScale 미적용     ← 프리팹 고정 크기로 표시 (문제)
```

**근본 원인:**
`Init()`에서 `_radius`를 데미지 판정 변수로만 저장하고,  
프리팹 GameObject의 `transform.localScale`을 반영하지 않아 시각적 크기가 항상 프리팹 기본값으로 고정됨.

**수정:**
`Init()` 내에서 `transform.localScale = Vector3.one * radius * 2f` 추가  
(프리팹 스프라이트가 직경 1 기준이면 radius * 2 = 직경)

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Skills/CausticGround.cs` | `Init()`에 `transform.localScale = Vector3.one * radius * 2f` 추가 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

> 스케일 공식: `localScale = radius * 2f` (프리팹 스프라이트 직경 1 기준)  
> AoeRadius = 반지름, localScale = 직경(반지름 × 2)

- [ ] AoeRadius = 1 → localScale = 2 (시각적 반지름 1) 확인
- [ ] AoeRadius = 5 → localScale = 10 (시각적 반지름 5) 확인
- [ ] 크기 변경 후 데미지 판정 범위도 일치 확인
- [ ] 기존 스킬 동작 간섭 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| 프리팹 스프라이트 기준 | 스프라이트 직경이 1이 아닐 경우 스케일 불일치 | radius * 2f 적용 (직경 1 가정), 에디터에서 사용자 확인 필요 |
| z 스케일 | 2D 게임이므로 z=1 유지 | `Vector3.one * radius * 2f`는 z도 변경되나 2D에서 무관 |
