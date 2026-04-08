# ClaudeAlert 상태 변화 사양서

## 1. 상태 정의

| 상태 | 코드 | 의미 |
|------|------|------|
| 대기 | `Idle` | 초기 상태. 활동 없음 |
| 작업 중 | `Active` | Claude Code가 도구를 사용하고 있음 |
| 완료 | `Done` | Claude Code가 응답을 마침 |
| 입력 대기 | `WaitingForInput` | Claude Code가 사용자 허가를 기다림 |
| 멈춤 | `Stuck` | Active 상태가 설정 시간 이상 지속됨 |
| 오류 | `Error` | 에러 감지됨 |
| 확인됨 | `Acknowledged` | 사용자가 알림을 확인함 |

## 2. 이벤트 소스

### 2-1. JSONL 세션 파일 모니터링 (주 이벤트 소스)
- 경로: `~/.claude/projects/{프로젝트 경로}/*.jsonl`
- 방식: FileSystemWatcher + 3초 주기 폴링
- 앱 시작 시 파일 끝에서부터 읽음 (과거 이벤트 무시)

### 2-2. 이벤트 감지 규칙

| JSONL type | 조건 | 발행 이벤트 |
|------------|------|-------------|
| `assistant` | `stop_reason: "tool_use"` | `tool_use` + 5초 허가 타이머 시작 |
| `assistant` | `stop_reason: "end_turn"` | `stop` + 허가 타이머 취소 |
| `tool_result` | - | 허가 타이머 취소 + 에러 체크 |
| `user` | - | `user_active` |
| `system` | "permission" 포함 | `permission_prompt` |
| `system` | "idle" 포함 | `idle_prompt` |
| (타이머) | 5초간 새 이벤트 없음 | `permission_prompt` |
| (모든 타입) | - | 허가 타이머 취소 |

## 3. 상태 전이 규칙

### 3-1. 포커스 필터 (최우선)
```
Claude Code가 포커스 상태 → 모든 이벤트 무시, 상태 변경 없음
```

### 3-2. Acknowledge 쿨다운
```
Acknowledge 후 5초 이내 → 모든 이벤트 무시
```

### 3-3. 이벤트별 전이

```
tool_use      → Active       (에스컬레이션 중지)
stop          → Done         (에스컬레이션 시작)
permission_prompt → WaitingForInput (에스컬레이션 시작)
idle_prompt   → WaitingForInput (에스컬레이션 시작)
error         → Error        (에스컬레이션 시작)
user_active   → Acknowledged (에스컬레이션 중이면)
```

### 3-4. 자동 전이

```
Active 상태 + StuckThresholdSeconds(기본 120초) 경과 + 이벤트 없음
  → Stuck (에스컬레이션 시작)
```

## 4. 상태 전이 다이어그램

```
                    ┌──────────────────────────────────────┐
                    │                                      │
                    ▼                                      │
  ┌──────┐    tool_use    ┌────────┐                       │
  │ Idle │───────────────▶│ Active │                       │
  └──────┘                └────┬───┘                       │
     ▲                         │                           │
     │                    stop │    120초 무응답            │
     │                         │         │                 │
     │              ┌──────────▼─┐   ┌───▼──┐              │
     │              │    Done    │   │ Stuck │              │
     │              └──────┬─────┘   └───┬───┘              │
     │                     │             │                 │
     │              ┌──────▼─────────────▼───┐             │
     │              │     에스컬레이션 중     │             │
     │              │  (Jump → Roll → Bounce)│             │
     │              └──────────┬─────────────┘             │
     │                         │                           │
     │                   확인  │                           │
     │                         ▼                           │
     │              ┌─────────────────┐      tool_use      │
     │              │  Acknowledged   │────────────────────┘
     │              └────────┬────────┘
     │                       │
     └───────────────────────┘
                   (대기로 복귀)

  ※ WaitingForInput, Error도 에스컬레이션을 시작하며
     동일한 확인 흐름을 따름
```

## 5. 에스컬레이션 시스템

### 5-1. 레벨 전이 (시간 기반)

| 경과 시간 | 레벨 | 쿨다운 | 동작 |
|-----------|------|--------|------|
| 0 ~ JumpSeconds | None | - | 정지 |
| JumpSeconds+ | Jump | 2.5초 | 위로 350-450px 점프 |
| RollSeconds+ | Roll | 3초 | 좌우 이동 + 회전 (400px/s, 540deg/s) |
| BounceSeconds+ | Bounce | 2초 | 전체 화면 튕기기 (800-1200px/s, 1440deg/s) |

기본값: Jump=30초, Roll=60초, Bounce=180초 (설정에서 변경 가능)

### 5-2. Bounce 모드 물리 변경
- 중력: 980 → 300
- 반탄성: 0.5 → 0.92

### 5-3. 에스컬레이션 종료 조건
- `tool_use` 이벤트 수신 (Claude가 다시 작업 시작)
- `Acknowledge()` 호출 (사용자 확인)

## 6. Acknowledge 방법

| 방법 | 조건 |
|------|------|
| 트레이 아이콘 클릭 | 에스컬레이션 중일 때 |
| 트레이 메뉴 → "알림 중지" | 에스컬레이션 중일 때 표시 |
| 캐릭터 클릭 | 에스컬레이션 중일 때 |
| JSONL에 user 메시지 감지 | 에스컬레이션 중 + 사용자가 Claude에 응답 |
| 자동 (포커스 기반) | 에스컬레이션 10초+ 경과 + Claude Code 포커스 |

### 6-1. Acknowledge 시 동작
1. 엔진 즉시 정지
2. 모든 물리 상태 리셋 (속도, 회전, 스케일 = 0)
3. 바닥 위치로 즉시 이동
4. 말풍선 숨김
5. 5초간 모든 이벤트 무시 (쿨다운)

## 7. Claude Code 포커스 감지

### 7-1. 판정 기준

| 조건 | 결과 |
|------|------|
| 포그라운드 창 = ClaudeAlert 자신 | NO |
| 프로세스 이름에 "claude" 포함 | YES |
| 터미널 프로세스 + 타이틀이 "claude"로 시작 | YES |
| 터미널 프로세스 + 타이틀에 " claude" 포함 | YES |
| 터미널 프로세스 + 타이틀에 "Claude Code" 포함 | YES |
| 그 외 (파일 탐색기, 브라우저 등) | NO |

터미널 프로세스: `windowsterminal`, `cmd`, `powershell`, `pwsh`, `conhost`, `wt`

### 7-2. 포커스 상태의 영향
- **YES**: 모든 이벤트 무시 → 상태 변경 없음 (대기 유지)
- **NO**: 이벤트 정상 처리

## 8. 시간 표시

| 상태 | 경과 시간 표시 |
|------|---------------|
| Idle | 숨김 |
| Active | "N초째" / "N분째" |
| Done, WaitingForInput, Stuck, Error | "N초 지남" / "N분 지남" |
| Acknowledged | 숨김 |

## 9. 말풍선

| 상태 | 한국어 | English |
|------|--------|---------|
| Idle | 대기 중 | Standing by |
| Active | 작업 중 | Working |
| Done | 끝났어, 확인해봐 | Done, have a look |
| WaitingForInput | 입력이 필요해 | Need your input |
| Stuck | 좀 막힌 것 같아 | Seems stuck |
| Error | 에러 발생했어 | Got an error |
| Acknowledged | 확인했어 | Got it |

- 상태 변경 시 표시, 5초 후 자동 숨김
- 에스컬레이션 중 15초마다 재표시
- Bounce 레벨에서는 숨김
