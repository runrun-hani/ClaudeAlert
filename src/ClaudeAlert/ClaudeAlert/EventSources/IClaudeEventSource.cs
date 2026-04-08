using ClaudeAlert.Core;

namespace ClaudeAlert.EventSources;

public interface IClaudeEventSource : IDisposable
{
    event Action<ClaudeEvent>? OnEvent;
    Task StartAsync(CancellationToken ct);
}
