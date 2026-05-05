# 기능 스펙 문서 인덱스 — MakeDefence

각 시스템의 인터페이스, 동작 규칙, 검증 조건을 정의한다.

---

## 문서 목록

| 문서 | 시스템 | Phase |
|------|--------|-------|
| [wave-system.md](./wave-system.md) | WaveSystem — 웨이브 생성, 스폰, 클리어 판정 | 1 |
| [enemy-system.md](./enemy-system.md) | EnemySystem — 등급별 스탯, 이동, 피격, 사망 | 1 |
| [map-system.md](./map-system.md) | MapTileSystem — 타일맵, 타워 배치 검증, 웨이포인트 | 1 |
| [tower-system.md](./tower-system.md) | TowerSystem — 타워 생성, 스킬/보조 옵션, 공격 | 1 · 2 |
| [cube-system.md](./cube-system.md) | CubeSystem — 5종 큐브 수량 관리, 드롭 보상 | 2 |
| [item-system.md](./item-system.md) | ItemSystem — 슬롯 해금, 옵션 크래프팅, 큐브 소모 | 2 |
| [shop-system.md](./shop-system.md) | ShopSystem — 스킬/보조 옵션 구매 | 2 |
| [boss-system.md](./boss-system.md) | BossSystem — 라스트 보스 도전, 승패 처리 | 3 |
| [save-load-system.md](./save-load-system.md) | SaveLoadSystem — JSON 자동 저장/불러오기 | 3 |
| [ui-system.md](./ui-system.md) | UISystem — HUD, 씬별 화면, 상태 연동 | 3 |

---

## 시스템 의존 관계 요약

```
WaveSystem → EnemySystem → PlayerSystem
WaveSystem → CubeSystem (드롭 보상)
TowerSystem → ItemSystem → CubeSystem
TowerSystem → ShopSystem
BossSystem → GameStateSystem
SaveLoadSystem ← WaveSystem / CubeSystem / TowerSystem
UISystem → 전체 시스템 (이벤트 구독)
```

---

## 관련 문서

- 설계 문서: [`docs/design-docs/index.md`](../design-docs/index.md)
- 실행 계획: [`docs/exec-plans/active/`](../exec-plans/active/)
- 시스템 아키텍처: [`ARCHITECTURE.md`](../../ARCHITECTURE.md)
