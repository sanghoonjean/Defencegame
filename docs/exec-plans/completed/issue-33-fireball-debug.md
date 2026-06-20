# Issue #33 — [BUG] Fireball 스킬 발사 안됨

## 1. 시스템 구조

```
Tower.Update()
  → FindTarget()
      → AttackRange = 0  ← 문제 원인
      → 모든 적이 범위 밖 → null 반환
  → Attack() 호출 안 됨 → 화염구 미발사
```

**근본 원인:**
`FireballSkill.asset`의 `baseRange = 0`.
`Tower.RefreshStats()`에서 스킬 장착 시 `AttackRange = EquippedSkill.baseRange`로 덮어쓰므로
사거리가 0이 되어 타겟을 찾지 못함.

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Perfab/FireballSkill.asset` | `baseRange` 0 → 5 로 수정 |
| `Assets/Scripts/Gameplay/Tower/Tower.cs` | `RefreshStats()`에서 `baseRange <= 0`이면 기존 `AttackRange` 유지 (방어 코드) |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] 스킬 장착 후 `AttackRange`가 0이 아닌 값으로 설정되는지 확인
- [ ] 타워가 적을 감지하고 화염구를 발사하는지 확인
- [ ] AoE 범위 내 다수 적 피해 확인
- [ ] `baseRange = 0` 설정 시 기존 타워 사거리 유지되는지 확인 (방어 코드)

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| asset 직접 수정 | Unity 에디터 열린 상태에서 파일 수정 시 충돌 가능 | Unity 에디터 닫고 수정 또는 Inspector에서 직접 수정 후 저장 |
| 다른 스킬 asset도 baseRange=0 가능성 | 향후 추가되는 스킬도 동일 문제 발생 가능 | Tower.RefreshStats()에 방어 코드 추가로 근본 방지 |
