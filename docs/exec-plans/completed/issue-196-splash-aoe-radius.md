# Issue #196 — 스플래시 피해 범위가 표시 범위보다 작게 적용됨

## 1. 시스템 구조

`ProjectileBase.Update()` → `OnHit()` → `ApplySplash()` + `GameUIManager.ShowAoeHit()`

- 피해 계산: `ApplySplash`에서 `SplashRadius`(world units)로 sqrMagnitude 비교
- 시각 표시: `ShowAoeHit` → `SpawnAoeFx` → `localScale = (diameter, diameter, 1f)` 설정

## 2. 원인

`SpawnAoeFx`에서 스프라이트 네이티브 크기를 고려하지 않고 `localScale = diameter`로 설정.
스프라이트가 1 world unit이 아닐 경우 시각적 원이 실제 피해 범위보다 크게 표시됨.

예: 64px 스프라이트 (PPU=32 → 2 world units 네이티브), radius=1.5
- 기존: `localScale = 3` → 시각 지름 = 6 world units (실제의 2배)
- 수정: `scale = 3 / 2 = 1.5` → 시각 지름 = 3 world units ✓

GL 원(프리팹 없을 때)은 world 좌표 직접 사용이라 정확함.

## 3. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `MakeDefence/Assets/Scripts/Systems/GameUIManager.cs` | `SpawnAoeFx`에서 sprite.bounds.size.x로 스케일 보정 |

## 4. 테스트 계획

- [ ] Fireball 장착 후 적 다수 밀집 상태에서 발사 — AoE 원과 실제 피해 범위 일치 확인
- [ ] FreezingPulse, LightningArrow 동일 확인
- [ ] AoeFxPrefab 없는 경우(GL 원) 기존대로 정상 표시되는지 확인

## 5. 위험 요소

- `SpriteRenderer`가 자식 오브젝트에 있는 경우 `GetComponent`가 null을 반환할 수 있음
  → 이 경우 폴백으로 기존 `localScale = diameter` 유지
- 비정방형 스프라이트는 X 기준으로만 스케일 계산 (원형 FX이므로 무방)
