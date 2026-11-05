namespace Source191.Torni.Application;

/// <summary>When this console last saw a session prompted, or that it has seen no prompt since it started.</summary>
public abstract record LastPrompt
{
    public sealed record Seen(DateTimeOffset At) : LastPrompt;

    public sealed record NotSeen : LastPrompt;
}