namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// Where a recorded failure happened: a tab's load or detail read, an effect run off the UI thread, the update check, the start's What's new,
/// a background task nothing awaited, or the crash that ended Vantage.
/// </summary>
public enum FailureSite
{
    Load,
    Effect,
    UpdateCheck,
    WhatsNew,
    BackgroundTask,
    Crash,
}