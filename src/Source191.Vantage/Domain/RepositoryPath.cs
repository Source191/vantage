using IOPath = System.IO.Path;

namespace Source191.Vantage.Domain;

public sealed record RepositoryPath
{
    private readonly string trimmed;

    public RepositoryPath(string value)
    {
        Value = value;
        trimmed = value.TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
    }

    public string Value { get; }

    public string Name => IOPath.GetFileName(trimmed);

    public string Parent => IOPath.GetDirectoryName(trimmed) ?? string.Empty;
}