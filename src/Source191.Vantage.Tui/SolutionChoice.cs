namespace Source191.Vantage.Tui;

/// <summary>What the solution chooser answered: the file chosen, or that none was.</summary>
internal abstract record SolutionChoice
{
    public sealed record Chosen(string File) : SolutionChoice;

    /// <summary>Esc or Cancel closed the chooser.</summary>
    public sealed record NoneChosen : SolutionChoice;
}