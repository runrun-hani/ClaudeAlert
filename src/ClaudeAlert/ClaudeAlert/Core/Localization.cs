namespace ClaudeAlert.Core;

public static class L10n
{
    public enum Language { Korean, English }

    private static Language _current = Language.Korean;
    public static event Action? LanguageChanged;

    public static Language Current => _current;

    private static readonly Dictionary<string, Dictionary<Language, string>> Strings = new()
    {
        // Status texts
        ["status.idle"] = new() { [Language.Korean] = "대기", [Language.English] = "Idle" },
        ["status.active"] = new() { [Language.Korean] = "작업 중", [Language.English] = "Working" },
        ["status.done"] = new() { [Language.Korean] = "완료", [Language.English] = "Done" },
        ["status.waiting"] = new() { [Language.Korean] = "입력 대기", [Language.English] = "Waiting" },
        ["status.stuck"] = new() { [Language.Korean] = "멈춤", [Language.English] = "Stuck" },
        ["status.error"] = new() { [Language.Korean] = "오류", [Language.English] = "Error" },
        ["status.acknowledged"] = new() { [Language.Korean] = "확인됨", [Language.English] = "Acknowledged" },

        // Elapsed time
        ["elapsed.suffix.active"] = new() { [Language.Korean] = "째", [Language.English] = " elapsed" },
        ["elapsed.suffix.idle"] = new() { [Language.Korean] = " 지남", [Language.English] = " ago" },
        ["elapsed.seconds"] = new() { [Language.Korean] = "{0}초", [Language.English] = "{0}s" },
        ["elapsed.minutes"] = new() { [Language.Korean] = "{0}분", [Language.English] = "{0}m" },
        ["elapsed.hours"] = new() { [Language.Korean] = "{0}시간", [Language.English] = "{0}h" },

        // Speech bubbles
        ["bubble.idle"] = new() { [Language.Korean] = "대기 중", [Language.English] = "Standing by" },
        ["bubble.active"] = new() { [Language.Korean] = "작업 중", [Language.English] = "Working" },
        ["bubble.done"] = new() { [Language.Korean] = "끝났어, 확인해봐", [Language.English] = "Done, have a look" },
        ["bubble.waiting"] = new() { [Language.Korean] = "입력이 필요해", [Language.English] = "Need your input" },
        ["bubble.stuck"] = new() { [Language.Korean] = "좀 막힌 것 같아", [Language.English] = "Seems stuck" },
        ["bubble.error"] = new() { [Language.Korean] = "에러 발생했어", [Language.English] = "Got an error" },
        ["bubble.acknowledged"] = new() { [Language.Korean] = "확인했어", [Language.English] = "Got it" },

        // Settings window
        ["settings.title"] = new() { [Language.Korean] = "설정", [Language.English] = "Settings" },
        ["settings.language"] = new() { [Language.Korean] = "언어", [Language.English] = "Language" },
        ["settings.port"] = new() { [Language.Korean] = "포트", [Language.English] = "Port" },
        ["settings.stuck_threshold"] = new() { [Language.Korean] = "멈춤 감지 (초)", [Language.English] = "Stuck Threshold (sec)" },
        ["settings.escalation_timing"] = new() { [Language.Korean] = "에스컬레이션 타이밍 (초)", [Language.English] = "Escalation Timing (sec)" },
        ["settings.jump"] = new() { [Language.Korean] = "점프", [Language.English] = "Jump" },
        ["settings.roll"] = new() { [Language.Korean] = "굴러가기", [Language.English] = "Roll" },
        ["settings.bounce"] = new() { [Language.Korean] = "튕기기", [Language.English] = "Bounce" },
        ["settings.sound"] = new() { [Language.Korean] = "소리 알림", [Language.English] = "Sound Alerts" },
        ["settings.autostart"] = new() { [Language.Korean] = "Windows 시작 시 자동 실행", [Language.English] = "Start with Windows" },
        ["settings.custom_image"] = new() { [Language.Korean] = "커스텀 이미지", [Language.English] = "Custom Image" },
        ["settings.browse"] = new() { [Language.Korean] = "찾아보기", [Language.English] = "Browse" },
        ["settings.hook_status"] = new() { [Language.Korean] = "Hook 상태", [Language.English] = "Hook Status" },
        ["settings.hook_configured"] = new() { [Language.Korean] = "✅ 구성 완료", [Language.English] = "✅ Configured" },
        ["settings.hook_not_configured"] = new() { [Language.Korean] = "❌ 미구성", [Language.English] = "❌ Not Configured" },
        ["settings.reconfigure"] = new() { [Language.Korean] = "재구성", [Language.English] = "Reconfigure" },
        ["settings.save"] = new() { [Language.Korean] = "저장", [Language.English] = "Save" },
        ["settings.cancel"] = new() { [Language.Korean] = "취소", [Language.English] = "Cancel" },
        ["settings.image_size"] = new() { [Language.Korean] = "이미지 크기", [Language.English] = "Image Size" },
        ["settings.font_size"] = new() { [Language.Korean] = "폰트 크기", [Language.English] = "Font Size" },
        ["settings.image_filter"] = new() { [Language.Korean] = "이미지 파일|*.png;*.gif;*.jpg;*.jpeg", [Language.English] = "Image Files|*.png;*.gif;*.jpg;*.jpeg" },

        // Tray icon
        ["tray.tooltip.idle"] = new() { [Language.Korean] = "ClaudeAlert - 대기", [Language.English] = "ClaudeAlert - Idle" },
        ["tray.tooltip.active"] = new() { [Language.Korean] = "ClaudeAlert - 작업 중", [Language.English] = "ClaudeAlert - Working" },
        ["tray.tooltip.done"] = new() { [Language.Korean] = "ClaudeAlert - 완료", [Language.English] = "ClaudeAlert - Done" },
        ["tray.tooltip.waiting"] = new() { [Language.Korean] = "ClaudeAlert - 입력 대기", [Language.English] = "ClaudeAlert - Waiting" },
        ["tray.tooltip.stuck"] = new() { [Language.Korean] = "ClaudeAlert - 멈춤", [Language.English] = "ClaudeAlert - Stuck" },
        ["tray.tooltip.error"] = new() { [Language.Korean] = "ClaudeAlert - 오류", [Language.English] = "ClaudeAlert - Error" },
        ["tray.show_hide"] = new() { [Language.Korean] = "보이기/숨기기", [Language.English] = "Show/Hide" },
        ["tray.acknowledge"] = new() { [Language.Korean] = "알림 중지", [Language.English] = "Stop Alert" },
        ["tray.exit"] = new() { [Language.Korean] = "종료", [Language.English] = "Exit" },

        // Context menu
        ["menu.settings"] = new() { [Language.Korean] = "설정", [Language.English] = "Settings" },
        ["menu.hide"] = new() { [Language.Korean] = "숨기기", [Language.English] = "Hide" },
        ["menu.exit"] = new() { [Language.Korean] = "종료", [Language.English] = "Exit" },

        // Toast notifications
        ["toast.done.title"] = new() { [Language.Korean] = "작업 완료", [Language.English] = "Task Complete" },
        ["toast.done.body"] = new() { [Language.Korean] = "Claude Code 작업이 완료되었습니다.", [Language.English] = "Claude Code task is complete." },
        ["toast.waiting.title"] = new() { [Language.Korean] = "입력 필요", [Language.English] = "Input Required" },
        ["toast.waiting.body"] = new() { [Language.Korean] = "Claude Code가 입력을 기다리고 있습니다.", [Language.English] = "Claude Code is waiting for your input." },
        ["toast.stuck.title"] = new() { [Language.Korean] = "작업 멈춤", [Language.English] = "Task Stuck" },
        ["toast.stuck.body"] = new() { [Language.Korean] = "Claude Code 작업이 멈춘 것 같습니다.", [Language.English] = "Claude Code task seems stuck." },
        ["toast.error.title"] = new() { [Language.Korean] = "오류 발생", [Language.English] = "Error Occurred" },
        ["toast.error.body"] = new() { [Language.Korean] = "Claude Code에서 오류가 발생했습니다.", [Language.English] = "An error occurred in Claude Code." },

        // App
        ["app.already_running"] = new() { [Language.Korean] = "ClaudeAlert가 이미 실행 중입니다.", [Language.English] = "ClaudeAlert is already running." },
    };

    public static string Get(string key)
    {
        if (Strings.TryGetValue(key, out var map) && map.TryGetValue(_current, out var val))
            return val;
        return key;
    }

    public static string Get(string key, params object[] args)
    {
        var template = Get(key);
        return string.Format(template, args);
    }

    public static void SetLanguage(Language lang)
    {
        if (_current == lang) return;
        _current = lang;
        LanguageChanged?.Invoke();
    }

    public static void SetLanguage(string langName)
    {
        var lang = langName == "English" ? Language.English : Language.Korean;
        SetLanguage(lang);
    }
}
