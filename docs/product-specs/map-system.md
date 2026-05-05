# 기능 스펙: MapSystem

## 개요
60x33 타일 기반의 단일 고정 맵을 관리한다.
타일 종류(경로/배치가능/장식)를 구분하고,
웨이포인트 경로 데이터를 PathfindingSystem에 제공한다.

---

## 기능 요구사항

### 1. 맵 기본 정보
- 크기: 60x33 타일 (1920x1080, PPU 32)
- 카메라 고정 (스크롤 없음)
- 단일 고정 맵 (런타임 생성 없음)

### 2. 맵 구조
3x3 구역 그리드. 구역 사이 통로가 적의 이동 경로.

```
[스폰]   [본진]
  │    ←←←←←←←←←←←←←←│
  ↓  ┌──────┬──────┬──────┤
     │  구역 │  구역 │  구역 │
  ↓  ├──────┼──────┼──────┤  ↑
     │  구역 │  구역 │  구역 │
  ↓  ├──────┼──────┼──────┤  ↑
     │  구역 │  구역 │  구역 │
  └→→→→→→→→→→→→→→→→→→→→┘
```

### 3. 타일 종류

| 타일 타입 | 설명 | 타워 배치 |
|-----------|------|-----------|
| `Path` | 구역 사이 통로, 적 이동 경로 | 불가 |
| `Buildable` | 9개 구역 내부 | 가능 |
| `Decoration` | 배경/구조물 시각 요소 | 불가 |

### 4. 주요 지점

| 지점 | 위치 |
|------|------|
| 스폰 포인트 | 좌상단 |
| 본진 (기지) | 중상단 |

### 5. 웨이포인트 경로
적 이동 순서:
```
스폰(좌상단)
  → 좌측 통로 하강
  → 하단 통로 우측 이동
  → 우측 통로 상승
  → 상단 통로 좌측 이동
  → 본진(중상단) 도달
```

### 6. 타워 배치 검증
- 타워 배치 시 해당 타일이 `Buildable` 인지 확인
- 이미 타워가 있는 타일에는 중복 배치 불가
- 배치 성공/실패 결과 반환

---

## 인터페이스

```csharp
public class MapTileSystem
{
    public TileType GetTileType(Vector2Int coord);
    public bool CanPlaceTower(Vector2Int coord);
    public bool PlaceTower(Vector2Int coord, Tower tower);
    public void RemoveTower(Vector2Int coord);
    public Vector2[] GetWaypoints();      // PathfindingSystem 제공용
    public Vector2 GetSpawnPoint();
    public Vector2 GetBasePoint();
}

public enum TileType
{
    Path, Buildable, Decoration
}
```

---

## 검증 조건
- [ ] 60x33 타일 맵 정상 로드
- [ ] 구역 내부 타일 Buildable 확인
- [ ] 경로 타일 배치 불가 확인
- [ ] 웨이포인트 순서 정확히 반환
- [ ] 스폰/기지 위치 정확히 반환
- [ ] 중복 배치 방지
