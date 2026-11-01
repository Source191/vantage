namespace Source191.Vantage.Adapters.Processes;

/// <summary>Whether Windows has an application for a file type, asked without opening anything.</summary>
public static partial class FileAssociations
{
    /// <summary>True only when Windows answers that nothing is associated with the extension (such as <c>.slnx</c>); any other answer, an error included, is false.</summary>
    public static bool NoneFor(string extension) => false;
}
