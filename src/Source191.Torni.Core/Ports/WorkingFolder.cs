namespace Source191.Torni.Ports;

/// <summary>The folder a child process starts in: Torni's own, which it inherits, or the one named.</summary>
public abstract record WorkingFolder
{
    /// <summary>The one instance callers pass when the child starts in Torni's folder.</summary>
    public static readonly WorkingFolder Inherit = new Inherited();

    public sealed record Inherited : WorkingFolder;

    public sealed record At(string Path) : WorkingFolder;
}