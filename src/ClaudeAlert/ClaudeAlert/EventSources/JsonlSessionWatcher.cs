using System.IO;
using System.Text.Json;
using ClaudeAlert.Core;

namespace ClaudeAlert.EventSources;

public class JsonlSessionWatcher : IClaudeEventSource
{
    private readonly string _projectsDir;
    private FileSystemWatcher? _dirWatcher;
    private FileSystemWatcher? _fileWatcher;
    private string? _currentFile;
    private long _lastPosition;
    private readonly System.Timers.Timer _scanTimer;

    private System.Timers.Timer? _permissionTimer;

    public event Action<ClaudeEvent>? OnEvent;

    public JsonlSessionWatcher()
    {
        _projectsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "projects");

        // Periodically scan for new/changed JSONL files
        _scanTimer = new System.Timers.Timer(3000);
        _scanTimer.Elapsed += (_, _) => ScanForLatestFile();
    }

    public Task StartAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_projectsDir))
            return Task.CompletedTask;

        // Watch the projects directory for new subdirectories/files
        _dirWatcher = new FileSystemWatcher(_projectsDir)
        {
            IncludeSubdirectories = true,
            Filter = "*.jsonl",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName
        };
        _dirWatcher.Changed += (_, _) => ScanForLatestFile();
        _dirWatcher.Created += (_, _) => ScanForLatestFile();
        _dirWatcher.EnableRaisingEvents = true;

        // Initial scan
        ScanForLatestFile();
        _scanTimer.Start();

        ct.Register(() =>
        {
            _scanTimer.Stop();
            _dirWatcher?.Dispose();
            _fileWatcher?.Dispose();
        });

        return Task.CompletedTask;
    }

    private void ScanForLatestFile()
    {
        try
        {
            // Find the most recently modified .jsonl file across all project dirs
            var latestFile = Directory.EnumerateFiles(_projectsDir, "*.jsonl", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateDirectories(_projectsDir)
                    .SelectMany(d =>
                    {
                        try { return Directory.EnumerateFiles(d, "*.jsonl", SearchOption.TopDirectoryOnly); }
                        catch { return Enumerable.Empty<string>(); }
                    }))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestFile == null) return;

            if (_currentFile != latestFile.FullName)
            {
                // Switch to watching a new file
                _currentFile = latestFile.FullName;
                _lastPosition = latestFile.Length; // Start from end — only process NEW events
                WatchFile(_currentFile);
            }

            ReadNewLines();
        }
        catch { }
    }

    private void WatchFile(string path)
    {
        _fileWatcher?.Dispose();
        var dir = Path.GetDirectoryName(path);
        var name = Path.GetFileName(path);
        if (dir == null) return;

        _fileWatcher = new FileSystemWatcher(dir, name)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _fileWatcher.Changed += (_, _) => ReadNewLines();
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void ReadNewLines()
    {
        if (_currentFile == null) return;

        try
        {
            using var fs = new FileStream(_currentFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length <= _lastPosition) return;

            fs.Seek(_lastPosition, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    ProcessLine(line);
            }

            _lastPosition = fs.Position;
        }
        catch { }
    }

    private void ProcessLine(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
                return;

            var type = typeProp.GetString();

            switch (type)
            {
                case "assistant":
                    ProcessAssistantMessage(root);
                    break;
                case "tool_result":
                    ProcessToolResult(root);
                    break;
                case "user":
                    // User sent a message → session is active
                    OnEvent?.Invoke(ClaudeEvent.Now("tool_use"));
                    break;
                case "system":
                    ProcessSystemMessage(root);
                    break;
            }
        }
        catch { }
    }

    private void ProcessAssistantMessage(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var msg))
            return;

        // Check stop_reason
        if (msg.TryGetProperty("stop_reason", out var stopReason))
        {
            var reason = stopReason.GetString();
            switch (reason)
            {
                case "tool_use":
                    // Claude wants to use a tool — emit tool_use now,
                    // but start a timer: if no tool_result comes within 3s,
                    // it's likely waiting for user permission
                    OnEvent?.Invoke(ClaudeEvent.Now("tool_use"));
                    StartPermissionTimer();
                    return;
                case "end_turn":
                    CancelPermissionTimer();
                    OnEvent?.Invoke(ClaudeEvent.Now("stop"));
                    return;
            }
        }
    }

    private void StartPermissionTimer()
    {
        CancelPermissionTimer();
        _permissionTimer = new System.Timers.Timer(3000) { AutoReset = false };
        _permissionTimer.Elapsed += (_, _) =>
        {
            OnEvent?.Invoke(ClaudeEvent.Now("permission_prompt"));
        };
        _permissionTimer.Start();
    }

    private void CancelPermissionTimer()
    {
        _permissionTimer?.Stop();
        _permissionTimer?.Dispose();
        _permissionTimer = null;
    }

    private void ProcessToolResult(JsonElement root)
    {
        // Tool result arrived → tool was approved and executed, cancel permission timer
        CancelPermissionTimer();
        OnEvent?.Invoke(ClaudeEvent.Now("tool_use"));

        // Also check for errors in the result
        if (root.TryGetProperty("message", out var msg) &&
            msg.TryGetProperty("content", out var content))
        {
            var text = content.ValueKind == JsonValueKind.String
                ? content.GetString() ?? ""
                : content.ToString();

            if (text.Contains("error", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("Error:", StringComparison.OrdinalIgnoreCase))
            {
                OnEvent?.Invoke(ClaudeEvent.Now("error"));
            }
        }
    }

    private void ProcessSystemMessage(JsonElement root)
    {
        var raw = root.ToString();
        if (raw.Contains("permission", StringComparison.OrdinalIgnoreCase))
        {
            OnEvent?.Invoke(ClaudeEvent.Now("permission_prompt"));
        }
        else if (raw.Contains("idle", StringComparison.OrdinalIgnoreCase))
        {
            OnEvent?.Invoke(ClaudeEvent.Now("idle_prompt"));
        }
    }

    public void Dispose()
    {
        _scanTimer.Stop();
        _scanTimer.Dispose();
        _dirWatcher?.Dispose();
        _fileWatcher?.Dispose();
    }
}
