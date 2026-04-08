namespace ClaudeAlert.Core;

public enum ClaudeState
{
    Idle,
    Active,
    Done,
    WaitingForInput,
    Stuck,
    Error,
    Acknowledged
}
