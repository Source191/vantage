namespace Source191.Vantage.Ports;

/// <summary>The folder a child process starts in: Vantage's own, which it inherits, or the one named.</summary>
public abstract record WorkingFolder
{
    /// <summary>The one instance callers pass when the child starts in Vantage's folder.</summary>
    public static readonly WorkingFolder Inherit = new Inherited();

    public sealed record Inherited : WorkingFolder;

    public sealed record At(string Path) : WorkingFolder;
}