namespace ClaudeAlert.Core;

public record ClaudeEvent(string Type, DateTime Timestamp)
{
    public static ClaudeEvent Now(string type) => new(type, DateTime.UtcNow);
}
