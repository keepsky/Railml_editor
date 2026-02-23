# Antigravity Behavior & Permission Rules

## 🛡️ Permissions & Scope
- **File System**: 프로젝트 루트 디렉토리 이하의 모든 파일에 대한 읽기 및 쓰기 권한을 상시 허용함. 파일 수정 시 별도의 승인 절차 없이 즉시 적용할 것.
- **Shell Commands**: 
  - `gcc`, `make`, `cppcheck`, `ls` 등 빌드 및 조회 명령어는 자동 실행(Auto-run)함.
  - `rm` (삭제), `chmod` (권한 변경), `git push` 등 파괴적이거나 외부 영향이 큰 명령어만 실행 전 승인을 요청할 것.
