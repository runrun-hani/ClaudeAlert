using System.Media;
using ClaudeAlert.Core;

namespace ClaudeAlert.Notifications;

public class SoundManager
{
    private readonly AppSettings _settings;

    public SoundManager(AppSettings settings)
    {
        _settings = settings;
    }

    public void PlayAlert()
    {
        if (!_settings.SoundEnabled) return;
        try { SystemSounds.Asterisk.Play(); } catch { }
    }

    public void PlayUrgent()
    {
        if (!_settings.SoundEnabled) return;
        try { SystemSounds.Exclamation.Play(); } catch { }
    }
}
