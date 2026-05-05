# 기능 스펙: SaveLoadSystem

## 개요
게임 진행 상황을 로컬에 저장하고,
게임 시작 시 자동으로 불러온다.

---

## 기능 요구사항

### 1. 저장 시점
- 웨이브 클리어 시 자동 저장
- 웨이브 결과 화면 종료 시 자동 저장
- 게임 종료 시 자동 저장

### 2. 저장 데이터

| 데이터 | 설명 |
|--------|------|
| 해금된 웨이브 난이도 | 1~16단계 현재 해금 현황 |
| 보유 큐브 수량 | 5종 큐브 각각 수량 |
| 타워 배치 정보 | 타일 좌표, 장착 스킬, 보조 옵션 |
| 아이템 슬롯 상태 | 슬롯 해금 여부, 옵션 목록 |
| 보유 스킬/보조 옵션 목록 | 상점에서 구매한 목록 |

### 3. 불러오기
- 게임 시작 시 저장 데이터 자동 로드
- 저장 데이터 없으면 기본 상태로 시작
- 불러오기 실패 시 기본 상태로 시작 (오류 무시)

### 4. 저장 방식
- Unity PlayerPrefs 또는 JSON 파일 저장
- 저장 경로: `Application.persistentDataPath`

---

## 인터페이스

```csharp
public class SaveLoadSystem
{
    public void Save();
    public void Load();
    public bool HasSaveData { get; }
    public void DeleteSave();
}

[System.Serializable]
public class SaveData
{
    public int unlockedStage;
    public Dictionary<CubeType, int> cubeInventory;
    public List<TowerSaveData> towers;
    public List<SkillType> ownedSkills;
    public List<SupportOptionType> ownedSupportOptions;
}
```

---

## 검증 조건
- [ ] 웨이브 클리어 시 자동 저장
- [ ] 게임 재시작 시 저장 데이터 정확히 복원
- [ ] 타워 배치/스킬/아이템 상태 복원
- [ ] 저장 데이터 없을 시 기본 상태로 시작
- [ ] 저장 데이터 삭제 기능 동작
