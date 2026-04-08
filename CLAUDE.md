# ClaudeAlert — 프로젝트 구조 가이드

> 이 파일은 LLM이 프로젝트를 빠르게 파악하기 위한 참조 문서입니다. 코드 변경 시 함께 갱신합니다.

## 프로젝트 개요

Claude Code IDE의 작업 상태를 모니터링하고 물리 기반 애니메이션으로 시각적 피드백을 제공하는 Windows WPF 데스크톱 애플리케이션.

- **플랫폼**: .NET 7.0 for Windows (WPF)
- **언어**: C# (nullable enabled, implicit usings)
- **솔루션**: `ClaudeAlert.sln`
- **프로젝트**: `src/ClaudeAlert/ClaudeAlert/ClaudeAlert.csproj`

## NuGet 패키지

| 패키지 | 버전 | 용도 |
|--------|------|------|
| Hardcodet.NotifyIcon.Wpf.NetCore | 1.1.5 | 시스템 트레이 아이콘 |
| Microsoft.Toolkit.Uwp.Notifications | 7.1.3 | Windows 토스트 알림 |
| WpfAnimatedGif | 2.0.2 | GIF 애니메이션 지원 |

## 디렉토리 구조

```
src/ClaudeAlert/ClaudeAlert/
├── App.xaml / App.xaml.cs          # 진입점, 컴포넌트 초기화 및 조율
├── Core/
│   ├── AppSettings.cs              # JSON 기반 설정 (Port, Thresholds, ImageSize, FontSize, Language)
│   ├── ClaudeEvent.cs              # ClaudeEvent 레코드 (Type, Timestamp)
│   ├── ClaudeState.cs              # ClaudeState enum (Idle, Active, Done, WaitingForInput, Stuck, Error, Acknowledged)
│   ├── ClaudeStatusManager.cs      # 상태 머신, 이벤트 처리, 에스컬레이션 타이머
│   └── Localization.cs             # 딕셔너리 기반 다국어 시스템 (Korean/English)
├── EventSources/
│   ├── IClaudeEventSource.cs       # 이벤트 소스 인터페이스
│   ├── HookHttpListener.cs         # HTTP 리스너 (localhost:19542/event)
│   ├── LogFileWatcher.cs           # 로그 파일 [error]/[fatal] 감지
│   └── SessionFileMonitor.cs       # 세션 파일 모니터링
├── Notifications/
│   ├── ToastNotifier.cs            # Windows 토스트 알림
│   └── SoundManager.cs             # 시스템 사운드 (Asterisk, Exclamation)
├── Physics/
│   ├── PhysicsBody.cs              # 물리 속성 (Position, Velocity, Rotation, Scale, Gravity=980, Bounce=0.5)
│   ├── PhysicsEngine.cs            # CompositionTarget.Rendering 기반 2D 물리 시뮬레이션
│   └── EscalationController.cs     # 시간 기반 에스컬레이션 (None→Jump→Roll→Bounce)
├── Views/
│   ├── OverlayWindow.xaml/.cs      # 캐릭터 이미지 + 말풍선 (물리 애니메이션 적용)
│   ├── StatusBarWindow.xaml/.cs    # 고정 위치 상태 텍스트
│   └── SettingsWindow.xaml/.cs     # 설정 창 (ScrollViewer 포함)
├── TrayIcon/
│   └── TrayIconManager.cs          # 시스템 트레이 아이콘 및 컨텍스트 메뉴
├── Setup/
│   ├── HookConfigurator.cs         # ~/.claude/settings.json에 curl 훅 자동 추가
│   ├── FocusHelper.cs              # Claude Code 창 포커스 전환 + 포커스 감지 (Win32 P/Invoke)
│   └── AutoStartManager.cs         # Windows 시작 프로그램 등록
├── Resources/
│   └── Icons/pet.png               # 기본 캐릭터 이미지
├── GlobalUsings.cs
└── AssemblyInfo.cs
```

## 핵심 동작 흐름

### 앱 시작 (App.xaml.cs → OnStartup)
1. 싱글 인스턴스 뮤텍스 확인
2. AppSettings 로드
3. Localization 초기화 (settings.Language)
4. Claude Code 훅 자동 구성 (HookConfigurator)
5. ClaudeStatusManager 생성
6. HTTP 리스너 시작 (localhost:{Port})
7. OverlayWindow (캐릭터) 표시
8. StatusBarWindow (상태 텍스트) 표시
9. ToastNotifier / SoundManager 초기화
10. LogFileWatcher 시작
11. 시스템 트레이 아이콘 등록

### 상태 전이 (ClaudeStatusManager.ProcessEvent)
| 이벤트 | 상태 변경 | 에스컬레이션 |
|--------|-----------|-------------|
| `tool_use` | → Active | 중지 |
| `stop` | → Done | 시작 |
| `permission_prompt` | → WaitingForInput | 시작 |
| `idle_prompt` | → WaitingForInput | 시작 |
| `error` | → Error | 시작 |
| Active 상태 120초+ | → Stuck | 시작 |
| 클릭 또는 Claude Code 포커스 | → Acknowledged | 중지 |

### 에스컬레이션 타임라인
| 경과 시간 | 레벨 | 동작 |
|-----------|------|------|
| 0-30s | None | 정지 |
| 30s+ | Jump | 2.5초마다 위로 350-450px 점프 |
| 60s+ | Roll | 3초마다 좌우 굴러가기 + 회전 |
| 180s+ | Bounce | 4초마다 전체 화면 튕기기 (Gravity=400, Bounce=0.85) |

### 두 윈도우 아키텍처
- **OverlayWindow**: 이미지만 포함, 물리 엔진으로 위치/회전/스케일 애니메이션
- **StatusBarWindow**: 상태 텍스트만 포함, 화면 하단 중앙 고정 (드래그 이동 가능)
- 트레이 아이콘/우클릭 메뉴에서 두 윈도우 함께 토글

## 설정 파일 경로

| 파일 | 경로 |
|------|------|
| 앱 설정 | `%DOCUMENTS%/ClaudeAlert/config.json` |
| 커스텀 이미지 | `%DOCUMENTS%/ClaudeAlert/images/` |
| Claude Code 훅 | `%USERPROFILE%/.claude/settings.json` |

## 빌드 & 실행

```bash
# 빌드
dotnet build -c Release

# 실행
dotnet run --project src/ClaudeAlert/ClaudeAlert
```

## 최근 변경 이력

- **v1.0.0**: 초기 릴리스
