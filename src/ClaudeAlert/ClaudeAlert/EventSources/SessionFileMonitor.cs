using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using ClaudeAlert.Core;

namespace ClaudeAlert.EventSources;

public class SessionFileMonitor : IClaudeEventSource
{
    private readonly string _sessionsDir;
    private DispatcherTimer? _timer;
    private readonly HashSet<int> _knownPids = new();
    private bool _hadActiveSession;

    public event Action<ClaudeEvent>? OnEvent;

    public bool HasActiveSession { get; private set; }
    public int ActiveSessionCount { get; private set; }

    public SessionFileMonitor(string? sessionsDir = null)
    {
        _sessionsDir = sessionsDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "sessions");
    }

    public Task StartAsync(CancellationToken ct)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += (_, _) => PollSessions();
        _timer.Start();

        ct.Register(() => _timer.Stop());

        // Initial poll
        PollSessions();
        return Task.CompletedTask;
    }

    private void PollSessions()
    {
        try
        {
            if (!Directory.Exists(_sessionsDir))
            {
                HasActiveSession = false;
                ActiveSessionCount = 0;
                return;
            }

            var activePids = new HashSet<int>();
            var files = Directory.GetFiles(_sessionsDir, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("pid", out var pidProp))
                    {
                        var pid = pidProp.GetInt32();
                        try
                        {
                            Process.GetProcessById(pid);
                            activePids.Add(pid);
                        }
                        catch (ArgumentException)
                        {
                            // Process not running - stale session file
                        }
                    }
                }
                catch { }
            }

            ActiveSessionCount = activePids.Count;
            HasActiveSession = activePids.Count > 0;

            // Detect new sessions
            foreach (var pid in activePids)
            {
                if (_knownPids.Add(pid))
                {
                    // New session detected
                }
            }

            // Detect ended sessions
            _knownPids.IntersectWith(activePids);

            // If we had sessions and now don't, that's notable
            if (_hadActiveSession && !HasActiveSession)
            {
                // All sessions ended - could indicate Claude Code was closed
            }
            _hadActiveSession = HasActiveSession;
        }
        catch { }
    }

    public void Dispose()
    {
        _timer?.Stop();
    }
}
