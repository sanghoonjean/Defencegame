# Issue #41 — [FEAT] enemy HP Bar 구현

## 1. 시스템 구조

```
Enemy.Initialize()
  → MaxHp 저장 (CurrentHp와 동일한 초기값)

EnemyHPBarUI (씬 내 오브젝트에 부착)
  OnGUI()
    → Enemy.ActiveEnemies 순회
    → Camera.main.WorldToScreenPoint(enemy.transform.position)
    → 화면 좌표 위에 HP 바 렌더링
    → fillRatio = enemy.CurrentHp / enemy.MaxHp
```

**핵심 메커니즘:**
- `Enemy.MaxHp` 프로퍼티 추가 — `Initialize()` 에서 `CurrentHp`와 함께 설정
- `EnemyHPBarUI.cs` — 씬의 어느 GameObject에든 부착, `OnGUI()`로 모든 활성 적의 HP 바를 화면 좌표에 그림
- `Camera.main.WorldToScreenPoint`로 월드 위치 → 스크린 좌표 변환 (y축 반전 처리)
- OnGUI 좌표계는 y=0이 화면 상단이므로 `Screen.height - screenPos.y` 보정 필요

**영향 시스템:**
- `Enemy.cs` — `MaxHp` 프로퍼티 추가
- `EnemyHPBarUI.cs` (신규) — OnGUI 기반 HP 바 렌더링 매니저

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Gameplay/Enemy/Enemy.cs` | `MaxHp` public 프로퍼티 추가, `Initialize()`에서 설정 |

---

## 3. 신규 클래스 / 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/EnemyHPBarUI.cs` | `OnGUI()`로 모든 활성 적 위에 HP 바 렌더링. 씬 오브젝트에 컴포넌트로 부착 |

### 클래스 인터페이스

```csharp
// EnemyHPBarUI.cs
public class EnemyHPBarUI : MonoBehaviour
{
    [SerializeField] private float barWidth  = 40f;
    [SerializeField] private float barHeight = 5f;
    [SerializeField] private float yOffset   = 20f;  // 적 위쪽 픽셀 오프셋

    private void OnGUI();
    // 모든 Enemy.ActiveEnemies 순회
    // Camera.main.WorldToScreenPoint → y 반전
    // GUI.DrawTexture: 배경(회색) + 채움(초록→빨강) 직사각형
}
```

---

## 4. 테스트 계획

- [ ] 씬에 빈 GameObject 생성 → EnemyHPBarUI 컴포넌트 부착 확인
- [ ] 웨이브 시작 후 적 위에 HP 바 표시 확인
- [ ] 이동하는 적에 HP 바가 따라가는 것 확인
- [ ] 타워 공격 후 HP 바 비율(%) 감소 확인
- [ ] 적 사망 시 HP 바 사라짐 확인 (ActiveEnemies에서 제거됨)
- [ ] 여러 적이 동시에 존재할 때 각각 독립적으로 표시 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| OnGUI y축 반전 | OnGUI 좌표 y=0이 상단 | `Screen.height - screenPos.y` 보정 |
| Camera.main null | 씬에 MainCamera 없으면 NPE | null 체크 후 스킵 |
| MaxHp = 0 나누기 | 초기화 전 접근 | `MaxHp > 0` 조건 체크 |
| 씬 부착 방법 | AGENTS.md: 씬/프리팹 편집 금지 | 코드만 작성, 사용자가 씬에서 직접 부착 |
| .meta 파일 | AGENTS.md 규칙상 수동 생성 금지 | Unity 에디터에서 자동 생성 |
