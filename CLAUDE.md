# MakeDefence — Claude Code 가이드라인

## 이슈 처리 워크플로우

이슈를 받으면 아래 순서로 진행한다.

### 1. 플랜 파일 생성 (필수)

파일 경로: `docs/exec-plans/issue-{issue_number}-{short-title}.md`

- `issue_number`: GitHub 이슈 번호
- `short-title`: 이슈 제목 핵심 단어 2~3개, 소문자 하이픈 구분 (예: `issue-14-world-setting`)

### 2. 플랜 파일 필수 항목

```
# Issue #{issue_number} — {이슈 제목}

## 1. 시스템 구조
변경이 영향을 주는 시스템/컴포넌트 구조 설명

## 2. 수정 파일
수정할 기존 파일 목록 (경로 포함)

## 3. 신규 클래스 / 파일
새로 만들 클래스 또는 파일 (역할 포함)

## 4. 테스트 계획
구현 후 검증 방법 및 체크리스트

## 5. 위험 요소
사이드 이펙트, 미확정 항목, 주의사항
```

### 3. PR 생성 금지

이슈 작업 시 PR을 자동 생성하지 않는다.
코드 작성 완료 후 커밋/푸시까지만 진행한다.

---

## 브랜치 규칙

- 작업 브랜치: `claude/fix-issue-{number}-{slug}`
- 항상 해당 브랜치에서 작업 후 push

## 저장소 정보

- Repository: `sanghoonjean/Defencegame`
- 엔진: Unity (C#)
- 장르: 타워 디펜스
- 세계관: 판타지
