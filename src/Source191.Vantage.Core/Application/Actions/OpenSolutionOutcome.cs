using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>What the solution key did.</summary>
public abstract record OpenSolutionOutcome : Outcome
{
    /// <summary>The solution was handed to Windows to open with its default application.</summary>
    public sealed record Opened(string FileName) : OpenSolutionOutcome;

    /// <summary>Windows has no application for the solution's type (its extension, such as <c>.slnx</c>), so nothing was opened.</summary>
    public sealed record NoApplication(string Extension) : OpenSolutionOutcome;
}