# RailML Editor 개선 계획

## 목표
- 기능 안정성 확보(런타임 예외/경고 축소)
- 구조 개선으로 유지보수성 향상
- 빌드/배포 경로 명확화

## 1. 안정성 우선(단기)
- `Utils` 컨버터 `ConvertBack` 정리
  - TwoWay 바인딩 여부 확인 후, 필요 없는 경우 `BindingMode=OneWay`로 고정
  - 필요한 경우 `ConvertBack` 구현 또는 `Binding.DoNothing` 반환
- 널 안정성 개선
  - `#pragma warning disable` 제거 목표
  - null 허용/비허용 설계 재정의(`?`/`required`)
- 경고 정리
  - CS8618/CS8600/CS8601/CS8603 등 대응

## 2. 구조 개선(중기)
- `MainViewModel.cs` 내 다중 ViewModel 분리
  - `TrackViewModel`, `SignalViewModel`, `RouteViewModel` 등 파일 분리
- `MainWindow.xaml.cs` UI 로직 분리
  - 커맨드/행동을 ViewModel 또는 Behavior로 이동
- `RailmlService.cs` 책임 분리
  - 직렬화/역직렬화 매퍼 분리
  - 토폴로지/연결 생성 로직 분리

## 3. 기능 품질(중기)
- 그래프 빌드 정확도 개선
  - 곡선 트랙 위치 계산 보정
  - 노드 매핑 오차 허용치 구성화
- 템플릿/설정 관리 안정화
  - 템플릿 파일 경로 및 기본값 명확화
  - 설정 저장/로드 실패 로깅

## 4. 빌드/배포(단기~중기)
- Windows/WSL 빌드 정책 문서화
  - `EnableWindowsTargeting` 주석 및 조건부 설정 고려
- 빌드 산출물/임시 폴더 정리
  - `.gitignore` 재점검(특히 `bin/`, `obj/`, 임시 프로젝트 폴더)

## 5. 테스트/회귀(장기)
- 핵심 로직 단위 테스트 추가
  - RailML 저장/불러오기 정합성 테스트
  - 연결/토폴로지 생성 로직 테스트
- 간단한 스모크 테스트 스크립트 작성

## 우선순위 제안
1. 컨버터/널 경고 정리(런타임 리스크 최소화)
2. 구조 분리(유지보수성 개선)
3. 그래프/토폴로지 정확도 개선
4. 테스트 체계 도입
