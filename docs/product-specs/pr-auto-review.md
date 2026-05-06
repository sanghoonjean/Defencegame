# PR 자동 리뷰 워크플로우 스펙

## 목표
- Pull Request 생성/업데이트 시 자동으로 리뷰어를 지정한다.
- PR 변경 규모(파일 수/추가/삭제 라인)를 자동 리뷰 요약으로 남긴다.

## 트리거
- `pull_request_target` 이벤트의 `opened`, `reopened`, `ready_for_review`, `synchronize`

## 동작 규칙
1. Draft PR은 제외한다.
2. 저장소 변수 `PR_AUTO_REVIEWERS`(쉼표 구분 GitHub 로그인 목록)를 읽는다.
3. 이미 요청된 리뷰어를 제외하고 남은 사용자에게 리뷰를 자동 요청한다.
4. 변경 파일 목록을 조회해 파일 수/추가/삭제 라인을 집계한다.
5. 대상 커밋 SHA를 포함한 리뷰 요약 코멘트를 PR 리뷰로 등록한다.

## 실패 처리
- API 호출 실패 시 워크플로우를 실패 처리해 로그에서 원인을 확인한다.

## 보안/권한
- `pull-requests: write` 권한을 사용한다.
- 외부 시크릿 없이 `GITHUB_TOKEN`만으로 동작한다.
