namespace Source191.Vantage.Application;

/// <summary>How a status line message starts: something done, a refusal or part of what was asked, a failure, an action still running, or a hint that is neither.</summary>
public enum StatusTone
{
    Success,
    Notice,
    Failure,
    Working,
    Hint,
}