# Issue #30 — TestRunner T키로 선택 타워에 스킬 장착

## 1. 시스템 구조

```
TestRunner.Update()
  → Input.GetMouseButtonDown(0)
      → Physics2D.OverlapPoint(mouseWorldPos)
          → Tower 컴포넌트 확인
          → InventorySystem.Instance.SelectTower(tower)   ← 타워 선택

  → Input.GetKeyDown(KeyCode.T)
      → InventorySystem.Instance.SelectedTower null 체크
      → InventorySystem.Instance.EquipSkill(fireballSkill)  ← 스킬 장착
```

**영향 시스템:**
- `TestRunner` — 마우스 클릭 타워 선택 + T키 스킬 장착 추가
- `InventorySystem` — 기존 `SelectTower()` / `EquipSkill()` 그대로 활용

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/TestRunner.cs` | `[SerializeField] SkillData fireballSkill` 추가, 마우스 클릭 타워 선택, T키 스킬 장착 |

---

## 3. 신규 클래스 / 파일

없음

---

## 4. 테스트 계획

- [ ] 타워 위 마우스 클릭 → Console에 타워 선택 로그 확인
- [ ] T키 입력 → 선택된 타워에 Fireball 스킬 장착 로그 확인
- [ ] 스킬 장착 후 타워가 화염구를 발사하는지 확인
- [ ] 타워 미선택 상태에서 T키 → 에러 없이 무시 확인
- [ ] Inspector에 `Fireball Skill` 미연결 시 에러 로그 출력 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| Collider 미설정 | Tower 프리팹에 Collider2D 없으면 OverlapPoint 감지 불가 | Tower 프리팹에 Collider2D 추가 필요 (Unity 에디터 작업) |
| 카메라 Z축 | ScreenToWorldPoint 시 z=0 처리 필요 | `Camera.main.ScreenToWorldPoint` + z 보정 |
| fireballSkill 미연결 | Inspector에서 SkillData를 연결 안 하면 null | null 체크 + LogError 처리 |
