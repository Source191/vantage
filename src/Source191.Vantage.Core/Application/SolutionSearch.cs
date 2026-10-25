namespace Source191.Vantage.Application;

/// <summary>What a look for a folder's solution found.</summary>
public abstract record SolutionSearch
{
    /// <summary>Neither the folder nor its src folder holds a solution.</summary>
    public sealed record None : SolutionSearch;

    /// <summary>One solution to open.</summary>
    public sealed record One(string File) : SolutionSearch;

    /// <summary>Several, none named after the repository: the user chooses.</summary>
    public sealed record Several(IReadOnlyList<string> Files) : SolutionSearch;
}
