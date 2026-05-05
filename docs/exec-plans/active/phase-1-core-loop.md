# 실행 계획: Phase 1 — 핵심 루프

## 목표
맵에 적이 등장하고 경로를 따라 이동하며,
타워가 자동으로 공격하는 기본 게임 루프를 완성한다.

---

## 작업 목록

### 1. 프로젝트 초기 설정
- [ ] Unity 2022.3.62f3 프로젝트 생성
- [ ] URP 2D 렌더 파이프라인 설정
- [ ] New Input System 패키지 설치
- [ ] 폴더 구조 생성 (`Assets/Scripts/Gameplay`, `Systems`, `UI`, `Data`)

### 2. MapTileSystem
- [ ] TileType enum 정의 (Path, Buildable, Decoration)
- [ ] 60x33 타일맵 데이터 정의
- [ ] 웨이포인트 배열 정의 (스폰 → 본진 경로)
- [ ] CanPlaceTower() 구현
- [ ] GetWaypoints(), GetSpawnPoint(), GetBasePoint() 구현

### 3. PathfindingSystem
- [ ] 웨이포인트 배열 수신
- [ ] Enemy에 다음 목적지 제공
- [ ] 목적지 도달 판정 구현

### 4. EnemySystem
- [ ] Enemy 클래스 구현 (등급, HP, 방어력, 이동속도)
- [ ] 등급별 기본 스탯 ScriptableObject 작성
- [ ] 난이도별 스탯 공식 적용
- [ ] 웨이포인트 이동 구현
- [ ] 기지 도달 → PlayerSystem.TakeDamage() 호출
- [ ] 피격/사망 처리

### 5. ObjectPoolSystem
- [ ] Enemy 오브젝트 풀 구현
- [ ] Get() / Return() 인터페이스

### 6. WaveSystem
- [ ] 등급 비율 공식 구현 (Magic/Rare/Unique)
- [ ] 단계별 적 수 계산
- [ ] 적 순차 스폰 구현
- [ ] 웨이브 클리어/실패 판정
- [ ] 자동 웨이브 토글 구현

### 7. PlayerSystem
- [ ] HP 100 기본값
- [ ] 웨이브 시작 시 HP 초기화
- [ ] TakeDamage() 구현

### 8. TowerSystem (기본)
- [ ] 타워 배치 (Buildable 타일 검증)
- [ ] 공격 범위 내 적 감지
- [ ] 기본 단일 공격 구현
- [ ] 데미지 계산 공식 적용

### 9. GameStateSystem
- [ ] 상태 정의 (Playing, WaveResult, Victory, Defeat)
- [ ] 상태 전환 이벤트 발행

---

## 완료 기준
- 적이 스폰되어 경로를 따라 이동한다
- 타워가 범위 내 적을 자동 공격한다
- 모든 적 처치 시 웨이브 클리어 판정
- 적 기지 도달 시 플레이어 HP 감소
- HP 0 시 패배 처리
