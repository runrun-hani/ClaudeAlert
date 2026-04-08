# ClaudeAlert

Claude Code의 작업 상태를 실시간으로 모니터링하고, 물리 기반 캐릭터 애니메이션으로 알려주는 Windows 데스크톱 애플리케이션.

## 주요 기능

- **실시간 상태 모니터링** — Claude Code의 작업 상태(작업 중, 완료, 입력 대기, 멈춤, 오류)를 JSONL 세션 파일을 통해 감지
- **물리 기반 애니메이션** — 캐릭터가 상태에 따라 점프, 굴러가기, 튕기기 등 물리 애니메이션으로 반응
- **에스컬레이션 시스템** — 응답 대기 시간이 길어질수록 애니메이션이 격렬해짐 (점프 → 굴러가기 → 튕기기)
- **말풍선** — 상태 변경 시 캐릭터가 짧은 메시지로 현재 상태를 알려줌
- **자동 Acknowledge** — Claude Code 창을 확인하면 자동으로 알림 중지
- **다국어 지원** — 한국어/영어 실시간 전환
- **다중 모니터 지원** — 모니터 간 자유롭게 이동, 모니터별 경계 충돌 처리
- **커스텀 이미지** — 기본 캐릭터 대신 PNG/GIF 이미지 사용 가능
- **크기 조정** — 이미지 크기(32~256px), 폰트 크기(8~24pt) 설정

## 기술 스택

| 구성 | 기술 |
|------|------|
| **플랫폼** | .NET 7.0 for Windows |
| **UI** | WPF (XAML) + Windows Forms |
| **물리 엔진** | 자체 2D 물리 시뮬레이션 (중력, 마찰, 반탄성) |
| **알림** | Windows 토스트 알림 API |
| **이벤트 감지** | Claude Code JSONL 세션 파일 모니터링 |

### 의존성

- **Hardcodet.NotifyIcon.Wpf.NetCore** (1.1.5) — 시스템 트레이 아이콘
- **Microsoft.Toolkit.Uwp.Notifications** (7.1.3) — 토스트 알림
- **WpfAnimatedGif** (2.0.2) — GIF 애니메이션

## 요구 사항

- Windows 10/11 이상
- .NET 7.0 런타임 (또는 소스 빌드 시 .NET 7.0 SDK)
- Claude Code 설치

## 설치

### Release 다운로드

1. [Releases](https://github.com/runrun-hani/ClaudeAlert/releases)에서 최신 버전 다운로드
2. 원하는 위치에 압축 해제
3. `ClaudeAlert.exe` 실행

### 소스에서 빌드

```bash
git clone https://github.com/runrun-hani/ClaudeAlert.git
cd ClaudeAlert
dotnet build -c Release
dotnet run --project src/ClaudeAlert/ClaudeAlert
```

## 사용 방법

### 동작 원리

ClaudeAlert는 Claude Code의 JSONL 세션 파일(`~/.claude/projects/`)을 모니터링하여 상태를 감지합니다. Hook이나 설정 파일 수정이 필요 없습니다.

```
Claude Code 작업 시작
    ↓
JSONL 파일에서 "tool_use" 감지 → 상태: 작업 중
    ↓
"end_turn" 감지 → 상태: 완료 (에스컬레이션 시작)
    ↓
30초 경과 → 캐릭터 점프
60초 경과 → 캐릭터 굴러가기
180초 경과 → 캐릭터 격렬하게 튕기기
    ↓
사용자가 Claude Code 창 확인 → 자동 Acknowledge (알림 중지)
```

### 조작

- **클릭** — Acknowledge (에스컬레이션 중지) 또는 미니 점프
- **드래그** — 캐릭터 위치 이동 (놓으면 중력 적용)
- **우클릭** — 설정, 숨기기, 종료 메뉴
- **트레이 아이콘** — 더블클릭으로 표시, 우클릭으로 메뉴

### 설정

설정 파일 위치: `%DOCUMENTS%/ClaudeAlert/config.json`

| 항목 | 설명 | 기본값 |
|------|------|--------|
| StuckThresholdSeconds | 멈춤 감지 시간 (초) | 120 |
| EscalationJumpSeconds | 점프 시작 시간 | 30 |
| EscalationRollSeconds | 굴러가기 시작 시간 | 60 |
| EscalationBounceSeconds | 튕기기 시작 시간 | 180 |
| SoundEnabled | 소리 알림 | true |
| ImageSize | 캐릭터 이미지 크기 (px) | 64 |
| FontSize | 상태 텍스트 폰트 크기 (pt) | 10 |
| Language | 언어 (Korean/English) | Korean |
| CustomImagePath | 커스텀 이미지 경로 | null |

### 상태

| 상태 | 의미 | 트리거 |
|------|------|--------|
| 대기 | Claude Code 대기 중 | 초기 상태 |
| 작업 중 | 도구 사용 중 | JSONL에서 tool_use 감지 |
| 완료 | 작업 완료 | JSONL에서 end_turn 감지 |
| 입력 대기 | 사용자 입력 필요 | permission_prompt 감지 |
| 멈춤 | 오래 응답 없음 | Active 상태 120초+ |
| 오류 | 에러 발생 | error 패턴 감지 |
| 확인됨 | 사용자가 확인 | 클릭 또는 Claude Code 포커스 |

## 아키텍처

```
src/ClaudeAlert/ClaudeAlert/
├── Core/                    # 핵심 로직
│   ├── ClaudeStatusManager  # 상태 머신
│   ├── AppSettings          # JSON 설정 관리
│   └── L10n                 # 다국어 시스템
├── EventSources/            # 이벤트 감지
│   ├── JsonlSessionWatcher  # JSONL 세션 파일 모니터링 (주 이벤트 소스)
│   ├── LogFileWatcher       # 로그 파일 에러 감지 (보조)
│   └── SessionFileMonitor   # 세션 상태 추적
├── Physics/                 # 물리 엔진
│   ├── PhysicsEngine        # 2D 물리 시뮬레이션
│   ├── PhysicsBody          # 물리 속성
│   └── EscalationController # 에스컬레이션 단계 관리
├── Views/                   # UI
│   ├── OverlayWindow        # 캐릭터 + 말풍선 (물리 애니메이션)
│   ├── StatusBarWindow      # 상태 텍스트 (고정 위치)
│   └── SettingsWindow       # 설정 창
├── Notifications/           # 알림
│   ├── ToastNotifier        # Windows 토스트
│   └── SoundManager         # 사운드
├── TrayIcon/                # 시스템 트레이
│   └── TrayIconManager
└── Setup/                   # 초기화
    ├── FocusHelper          # Claude Code 포커스 감지
    └── AutoStartManager     # 자동 시작
```

## 기여

1. 이슈 등록: 버그 리포트, 기능 제안
2. Pull Request:
   - Fork → 브랜치 생성 → 커밋 → PR
   - [CONTRIBUTING.md](CONTRIBUTING.md) 참고

## 라이선스

MIT License — [LICENSE](LICENSE) 참고

## 로드맵

- [ ] 커스텀 애니메이션 프리셋
- [ ] Discord/Slack 알림 연동
- [ ] 커스텀 사운드 지원
- [ ] 작업 통계 대시보드
- [ ] macOS/Linux 지원 (연구 중)
