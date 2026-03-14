# Git Convention Guide

> Commit · Branch · Pull Request 컨벤션 가이드

---

## 1. Commit Message Convention

### 1-1. 형식

```
<type>: <subject>
<type>(<scope>): <subject>   // scope는 선택
```

### 1-2. 타입 목록

| 타입 | 설명 | 예시 |
|------|------|------|
| `feat` | 새로운 기능 추가 | `feat: 사용자 프로필 이미지 업로드 기능 추가` |
| `fix` | 버그 수정 | `fix: 로그인 시 토큰 만료 미처리 오류 수정` |
| `refactor` | 리팩토링, 성능 개선 (기능 변경 없음) | `refactor: 결제 모듈 의존성 구조 개선` |
| `asset` | 새로운 에셋 파일 추가 (기능 변경 없음) | `asset: 플레이어 캐릭터 에셋 추가` |
| `chore` | 그 외 모든 작업 (설정, CI, 문서, 테스트, 빌드, 되돌리기 등) | `chore: GitHub Actions 배포 워크플로우 추가` |

### 1-3. 작성 규칙

1. 타입은 소문자로 작성한다.
2. 제목(subject)은 50자 이내로 간결하게 작성한다.
3. 제목 끝에 마침표(.)를 붙이지 않는다.
4. 한글로 작성하며, 명령형 어미를 사용한다. (예: "추가", "수정", "제거")
5. 본문(body)이 필요한 경우 제목과 한 줄 띄어 작성한다.

---

## 2. Branch Naming Convention

### 2-1. 형식

```
<type>/<name>/<description>
<type>/<name>/<issue-number>-<description>   // 이슈 번호 포함 시
```

### 2-2. 브랜치 목록

| 접두사 | 용도 | 예시 |
|--------|------|------|
| `main` | 운영 배포 브랜치 | `main` |
| `develop` | 개발 통합 브랜치 | `develop` |
| `feat/` | 새 기능 개발 | `feat/jk/user-profile-image` |
| `fix/` | 버그 수정 | `fix/jk/login-token-expiry` |
| `hotfix/` | 긴급 운영 수정 | `hotfix/jk/payment-crash` |
| `release/` | 릴리스 준비 | `release/1.2.0` |
| `refactor/` | 리팩토링, 성능 개선 | `refactor/jk/payment-module` |
| `chore/` | 그 외 모든 작업 | `chore/jk/eslint-config-update` |

### 2-3. 작성 규칙

1. 영문 소문자와 하이픈(-)만 사용한다.
2. 타입 뒤에 작업자의 이름 약자(이니셜)를 소문자로 포함한다. (예: jk, mh, sw)
3. 설명은 간결하게 2~4단어 이내로 작성한다.
4. 이슈 트래커를 사용하는 경우, 이슈 번호를 포함한다. (예: `feat/jk/123-user-avatar`)
5. 작업 완료 후 머지된 브랜치는 삭제한다.

---

## 3. Pull Request Convention

### 3-1. PR 제목 형식

```
[<type>] <subject>
```

**예시:**

```
[feat] 사용자 프로필 이미지 업로드 기능 추가
[fix] 로그인 시 토큰 만료 미처리 오류 수정
[refactor] 결제 모듈 의존성 구조 개선
[chore] GitHub Actions 배포 워크플로우 추가
```

### 3-2. PR 본문 템플릿

```markdown
## 변경 사항 (Changes)
- 변경된 내용을 간단히 설명합니다.

## 변경 이유 (Why)
- 이 변경이 필요한 배경을 설명합니다.

## 관련 이슈 (Related Issues)
- closes #이슈번호

## 테스트 (Test)
- [ ] 단위 테스트 통과
- [ ] 로컬 환경 테스트 완료

## 스크린샷 (Screenshot)
- UI 변경이 있을 경우 첨부합니다.
```

### 3-3. PR 작성 규칙

1. PR 제목은 커밋 타입과 동일한 prefix를 대괄호(`[]`) 안에 표기한다.
2. 하나의 PR은 하나의 목적만 갖도록 한다.
3. 변경 파일이 많을 경우, PR을 분리하여 리뷰 부담을 줄인다.
4. 본문 템플릿의 모든 항목을 빠짐없이 작성한다.
5. 리뷰어를 반드시 지정한다.

---

## Quick Reference

| 구분 | 형식 | 예시 |
|------|------|------|
| Commit | `type: subject` | `feat: 프로필 이미지 업로드 추가` |
| Branch | `type/name/description` | `feat/jk/user-profile-image` |
| PR Title | `[type] subject` | `[feat] 프로필 이미지 업로드 기능 추가` |
