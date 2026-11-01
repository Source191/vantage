namespace Source191.Vantage.Domain;

/// <summary>A process tree looked up by its root: the tree, or the reason there is none to measure.</summary>
public abstract record ProcessTreeLookup
{
    public sealed record Found(ProcessTree Tree) : ProcessTreeLookup;

    /// <summary>The root is not in the process table, or it refused to be measured.</summary>
    public sealed record NotMeasurable : ProcessTreeLookup;
}