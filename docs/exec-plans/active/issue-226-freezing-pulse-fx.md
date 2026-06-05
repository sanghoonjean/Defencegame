# Issue #226 — [FIX] FreezingPulse AoE FX 프리팹/애니메이션 미재생

## 1. 시스템 구조

PR #216에서 FreezingPulse가 프로젝타일 기반 → 즉발 AoE 펄스로 전환되면서 FX 프리팹 전달 경로가 끊겼고, 동시에 Rectangle/Cone 도형은 처음부터 prefab 인스턴스화를 지원하지 않았다.

### 변경 후 데이터 흐름

```
Tower.Attack
   ↓
SkillDispatcher.ExecuteFreezingPulse(tower, target)
   ├─ skill.aoeFxPrefab  ← [신규] SkillData 필드
   └─ AoeUtils.ShowAoeHit(origin, forward, shape, radius, width, angle, fxPrefab)
         ├─ Circle    → GameUIManager.ShowAoeHit(pos, radius, fxPrefab)            [기존]
         ├─ Rectangle → GameUIManager.ShowRectAoeHit(pos, dir, w, len, fxPrefab)  [신규 인자]
         └─ Cone      → GameUIManager.ShowConeAoeHit(pos, dir, ang, r, fxPrefab)  [신규 인자]
                          ↓
                       SpawnAoeFx 계열 (prefab != null 일 때만)
                          ├─ 회전: forward 기준 Z축 (atan2(dir.y, dir.x) * Rad2Deg)
                          ├─ 스케일: 도형별 (Rect=width·length, Cone/Circle=radius·2)
                          └─ Destroy(go, aoeHitDuration)
   prefab == null 인 경우 → 기존 내부 도형 렌더(_aoeCircles/_rectAoes/_coneAoes) 유지
```

### 핵심 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| FX 프리팹 위치 | `SkillData.aoeFxPrefab` 필드 (Inspector 노출) | FreezingPulse 전용 자산이 아니라 모든 스킬 공용 슬롯. LightningArrow 등 향후 확장 용이 |
| 회전 기준 | forward 방향을 prefab의 local +X 축에 매핑 (Z축 회전) | 2D 톱다운 표준. Rectangle/Cone의 forward와 시각적 정렬 |
| Rectangle 위치 | `origin + forward * (length / 2)` (중심을 사각형 중앙에 둠) | 기존 `_rectAoes` 렌더링은 origin 기준 forward로 length만큼 뻗는 형태. FX는 일반적으로 중심점 기준이라 절반만큼 forward 이동 |
| Cone 위치 | `origin` 그대로 | Cone은 원점이 꼭짓점 |
| 스케일 정책 | sprite native size 기준 비례 (기존 `SpawnAoeFx` 패턴 재사용) | 회귀 위험 최소화. PR #215에서 도입된 보정 로직과 일관 |
| 풀링 | 사용하지 않음 (Instantiate/Destroy) | 기존 `SpawnAoeFx`와 동일. 풀링은 별도 이슈로 검토 |
| FX 수명 | 기존 `aoeHitDuration` 사용 | Animator 클립 길이와 별개. 클립이 더 길어도 prefab 파괴됨 — 디자인 의도(짧은 점멸)와 부합 |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/SkillData.cs`
  - `[SerializeField] public GameObject aoeFxPrefab` 추가 (Inspector 노출)
- `MakeDefence/Assets/Scripts/Gameplay/Skills/AoeUtils.cs`
  - `ShowAoeHit` 의 Rectangle/Cone 케이스에서 `fxPrefab` 인자 전달
- `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs`
  - `ShowRectAoeHit` 시그니처에 `GameObject fxPrefab = null` 추가
  - `ShowConeAoeHit` 시그니처에 `GameObject fxPrefab = null` 추가
  - `SpawnRectAoeFx` / `SpawnConeAoeFx` 신규 private 메서드 — 기존 `SpawnAoeFx` 패턴 재사용
- `MakeDefence/Assets/Scripts/Gameplay/Skills/SkillDispatcher.cs`
  - `ExecuteFreezingPulse` 의 `AoeUtils.ShowAoeHit(...)` 호출에 `skill.aoeFxPrefab` 추가

## 3. 신규 클래스 / 파일

없음. 기존 메서드 시그니처 확장 + private 헬퍼 2개 추가에 그침.

## 4. 테스트 계획

### 수동 검증
- [ ] 컴파일 OK (`read_console`)
- [ ] `FreezingPulseSkill.asset.aoeFxPrefab` = `FX_Freezing.prefab` 설정 후:
  - Circle: 기존 동작 회귀 없이 FX 표시
  - Rectangle: FX가 origin → forward 방향으로 정렬, 가로 폭이 `aoeWidth`에 비례
  - Cone: FX가 origin에서 forward 방향으로 회전 적용
- [ ] `aoeFxPrefab` 미설정 시: 내부 도형 렌더링으로 fallback (회귀 없음)
- [ ] LightningArrow 등 다른 스킬: 변경 없이 정상 동작 (SkillData에 필드 추가만, 기본값 null)

### 회귀 검증
- 기존 Circle FreezingPulse 흐름 (SpawnAoeFx) 유지
- PR #215의 스프라이트 네이티브 보정 동작 유지

## 5. 위험 요소

- **FX_Freezing.prefab은 원형 디자인** — Rectangle/Cone에 그대로 쓰면 시각적 부조화 가능. 본 PR 범위 밖이지만, Rectangle/Cone 전용 프리팹 디자인은 사용자 측 후속 작업 필요. 코드는 이미 모양별 회전·스케일을 적용하므로 새 프리팹 적용 시 즉시 반영.
- **회전 기준 불일치** — prefab의 local +X 축이 정면이 아닌 다른 축(예: +Y)을 정면으로 디자인됐다면 90도 어긋남. 본 fix는 +X 정면 가정. 어긋날 경우 프리팹의 루트 회전을 조정하거나 추후 옵션 필드 도입.
- **SkillData 직렬화 변경** — 기존 `.asset` 파일에 `aoeFxPrefab` 필드가 없지만 Unity는 기본값(null)으로 안전하게 로드. 회귀 없음.
- **풀링 미적용** — 빈번한 Instantiate/Destroy는 GC 부담. 본 PR은 기존 `SpawnAoeFx`와 동일 정책 유지. 성능 이슈 시 별도 이슈에서 `ObjectPoolSystem` 적용 검토.
- **Animator 미부착 prefab** — fxPrefab이 SpriteRenderer만 있어도 동작 (스케일/회전만 적용). 정적 이미지여도 깨지지 않음.
