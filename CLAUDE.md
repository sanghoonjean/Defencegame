# MakeDefence — Claude Code 가이드라인

## 이슈 처리 워크플로우

이슈를 받으면 아래 순서로 진행한다.

### 1. 플랜 파일 생성 (필수)

파일 경로: `docs/exec-plans/active/issue-{issue_number}-{short-title}.md`

- `issue_number`: GitHub 이슈 번호
- `short-title`: 이슈 제목 핵심 단어 2~3개, 소문자 하이픈 구분 (예: `issue-14-world-setting`)
- 구현 완료 후 `docs/exec-plans/completed/`로 이동

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

### 3. Plan PR 생성

플랜 파일 커밋/푸시 후 **Plan PR을 생성**한다.

- 템플릿: `.github/PULL_REQUEST_TEMPLATE/plan.md` 사용
- PR 제목: `[Plan] Issue #{number} — {이슈 제목}`
- PR body는 템플릿의 모든 섹션을 실제 내용으로 채운다
  - **관련 Issue**: `Refs #{number}` (이슈를 자동 닫지 않도록 `Closes` 대신 `Refs` 사용)
  - **목적**: 이슈의 목적
  - **포함되는 작업 / 제외되는 작업**: 플랜 파일의 시스템 구조/수정 파일/신규 파일 기준
  - **핵심 구조**: 시스템 구조 요약
  - **데이터 흐름**: Input → System → Output 다이어그램

이 PR은 **플랜 검토용**이며, 실제 코드 작성 전에 머지하지 않는다.
플랜 승인 후 별도 구현 브랜치에서 코드 작성 → 별도 구현 PR을 생성한다.

**Plan PR은 절대 Close하지 않는다** — 구현 PR이 머지된 후에도 플랜 검토 기록으로 남겨둔다.
Claude Code는 Plan PR을 임의로 close/merge하지 않는다 (사용자가 직접 관리).

---

## 브랜치 규칙

- 작업 브랜치: `claude/fix-issue-{number}-{slug}`
- 항상 해당 브랜치에서 작업 후 push

## 저장소 정보

- Repository: `sanghoonjean/Defencegame`
- 엔진: Unity (C#)
- 장르: 타워 디펜스
- 세계관: 판타지
