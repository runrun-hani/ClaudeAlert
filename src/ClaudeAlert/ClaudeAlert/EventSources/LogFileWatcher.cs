using System.IO;
using System.Text.RegularExpressions;
using ClaudeAlert.Core;

namespace ClaudeAlert.EventSources;

public class LogFileWatcher : IClaudeEventSource
{
    private readonly string _logPath;
    private FileSystemWatcher? _watcher;
    private long _lastPosition;
    private static readonly Regex ErrorPattern = new(
        @"\[(error|fatal)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ActivityPattern = new(
        @"sendMessage|respondToToolPermission|toolUse", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private DateTime _lastEventTime = DateTime.MinValue;
    private readonly TimeSpan _dedupeWindow = TimeSpan.FromSeconds(2);

    public event Action<ClaudeEvent>? OnEvent;

    public LogFileWatcher(string? logPath = null)
    {
        _logPath = logPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Claude", "Logs", "main.log");
    }

    public Task StartAsync(CancellationToken ct)
    {
        if (!File.Exists(_logPath))
            return Task.CompletedTask;

        // Start reading from end of file
        _lastPosition = new FileInfo(_logPath).Length;

        var dir = Path.GetDirectoryName(_logPath)!;
        var fileName = Path.GetFileName(_logPath);
        _watcher = new FileSystemWatcher(dir, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFileChanged;

        ct.Register(() =>
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        });

        return Task.CompletedTask;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            using var fs = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < _lastPosition)
            {
                // File was truncated/rotated
                _lastPosition = 0;
            }

            fs.Seek(_lastPosition, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                ProcessLine(line);
            }
            _lastPosition = fs.Position;
        }
        catch { }
    }

    private void ProcessLine(string line)
    {
        var now = DateTime.UtcNow;
        if (now - _lastEventTime < _dedupeWindow) return;

        if (ErrorPattern.IsMatch(line))
        {
            _lastEventTime = now;
            OnEvent?.Invoke(ClaudeEvent.Now("error"));
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
