# Issue #42 — [FEAT] Tower Base Range 표시

## 1. 시스템 구조

```
마우스 클릭 (TestRunner.Update)
  → InventorySystem.SelectTower(tower)
      SelectedTower 갱신

TowerRangeUI (씬 내 오브젝트에 부착)
  OnRenderObject()
    → InventorySystem.Instance.SelectedTower 읽기
    → tower.AttackRange를 반지름으로 GL.LINES 원 그리기
    → tower.transform.position 중심, 월드 좌표계 렌더링
```

**핵심 메커니즘:**
- `TowerRangeUI.cs` 단일 스크립트로 구현 — 씬 임의 GameObject에 부착
- `OnRenderObject()` + `GL.LINES` 로 월드 좌표 원 렌더링 (좌표 변환 불필요)
- `Shader.Find("Hidden/Internal-Colored")` 기반 단색 Material 런타임 생성
- 선택된 타워가 없으면 렌더링 스킵
- 원 분할 수(`segments`)는 Inspector에서 조정 가능

**영향 시스템:**
- `TowerRangeUI.cs` (신규) — GL 기반 사거리 원 렌더러. 기존 코드 무수정

---

## 2. 수정 파일

없음

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/Systems/TowerRangeUI.cs` | 선택된 타워의 AttackRange를 GL.LINES 원으로 렌더링 |

### 클래스 인터페이스

```csharp
public class TowerRangeUI : MonoBehaviour
{
    [SerializeField] private Color  lineColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private int    segments  = 64;

    private Material _mat;

    private void Awake();          // Shader.Find로 Material 생성
    private void OnDestroy();      // Destroy(_mat)
    private void OnRenderObject(); // SelectedTower != null 일 때 GL 원 그리기
}
```

---

## 4. 테스트 계획

- [ ] 씬에 빈 GameObject 생성 → TowerRangeUI 컴포넌트 부착
- [ ] 타워 설치 후 클릭 → AttackRange 원 표시 확인
- [ ] 다른 타워 클릭 시 원이 해당 타워로 이동 확인
- [ ] 타워 선택 해제(빈 곳 클릭) 시 원 사라짐 확인
- [ ] 기존 EnemyHPBarUI, TestRunner 동작 간섭 없음 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| Shader null | 빌드 환경에 따라 Internal-Colored 없을 수 있음 | null 체크 후 경고 |
| GL ZTest | 3D 오브젝트에 가려질 수 있음 | ZTest Always 설정 |
| 씬 부착 방법 | AGENTS.md: 씬/프리팹 편집 금지 | 코드만 작성, 사용자가 씬에서 직접 부착 |
| .meta 파일 | AGENTS.md 규칙상 수동 생성 금지 | Unity 에디터에서 자동 생성 |
