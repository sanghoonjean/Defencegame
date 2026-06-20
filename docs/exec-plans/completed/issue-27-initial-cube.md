# Issue #27 — 큐브 초기 지급 추가 및 TestRunner 디버그 큐브 키 추가

## 1. 시스템 구조

```
CubeSystem.Awake()
  → initialLowerCubes > 0 이면 Add(CubeType.Lower, initialLowerCubes)  ← 신규

TestRunner.Update()
  → Input.GetKeyDown(KeyCode.C)
      → CubeSystem.Instance.Add(CubeType.Lower, 10)                     ← 신규
```

**영향 시스템:**
- `CubeSystem` — Inspector 설정 가능한 초기 Lower 큐브 지급
- `TestRunner` — C키 디버그 큐브 지급

---

## 2. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `Assets/Scripts/Systems/CubeSystem.cs` | `[SerializeField] private int initialLowerCubes` 필드 추가, `Awake()`에서 초기 지급 |
| `Assets/Scripts/TestRunner.cs` | C키 입력 시 `CubeSystem.Instance.Add(CubeType.Lower, 10)` 호출 |

---

## 3. 신규 클래스 / 파일

없음 (기존 파일 수정만)

---

## 4. 테스트 계획

- [ ] Unity 실행 시 CubeSystem Inspector의 `Initial Lower Cubes` 값만큼 큐브가 지급되는지 확인
- [ ] `Initial Lower Cubes = 0` 설정 시 초기 지급 없음 확인
- [ ] C키 누르면 Lower 큐브 10개 증가 확인
- [ ] C키 null 가드 — CubeSystem.Instance 없을 때 에러 없음 확인
- [ ] 초기 큐브로 타워 즉시 설치 가능한지 확인

---

## 5. 위험 요소

| 항목 | 내용 | 대응 |
|------|------|------|
| 초기값 과다 지급 | initialLowerCubes를 너무 크게 설정하면 게임 밸런스 붕괴 | 기본값 5 정도로 보수적 설정, Inspector 조정 가능하도록 유지 |
| TestRunner C키 중복 | 빠르게 연타 시 무한 큐브 획득 가능 | 개발 전용 도구이므로 허용 |
