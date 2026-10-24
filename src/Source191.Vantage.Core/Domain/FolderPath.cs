using IOPath = System.IO.Path;

namespace Source191.Vantage.Domain;

/// <summary>
/// A folder on disc, as a full path: normalised once (forward slashes as git prints them become backslashes, trailing
/// separators go, a drive root keeps its own) and compared ignoring case, as Windows does. The one place Vantage
/// decides whether two paths are the same folder.
/// </summary>
public sealed record FolderPath
{
    public FolderPath(string text)
    {
        if (!IOPath.IsPathFullyQualified(text))
        {
            throw new ArgumentException($"{text} is not a full path.", nameof(text));
        }

        string full = IOPath.GetFullPath(text.Replace(IOPath.AltDirectorySeparatorChar, IOPath.DirectorySeparatorChar));
        string root = IOPath.GetPathRoot(full) ?? string.Empty;
        Value = full.Length > root.Length ? full.TrimEnd(IOPath.DirectorySeparatorChar) : full;
    }

    public string Value { get; }

    /// <summary>The last segment, such as the repository's folder name.</summary>
    public string Name => IOPath.GetFileName(Value);

    /// <summary>The containing folder; null for a drive root.</summary>
    public FolderPath? Parent => IOPath.GetDirectoryName(Value) is string parent ? new FolderPath(parent) : null;

    public FolderPath Combine(string child)
    {
        return new FolderPath(IOPath.Combine(Value, child));
    }

    /// <summary>True for this folder itself or anything inside it; a sibling that shares a name prefix is not inside.</summary>
    public bool IsUnder(FolderPath other)
    {
        return Equals(other) || Value.StartsWith(Inside(other.Value), StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(FolderPath? other)
    {
        return other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }

    private static string Inside(string folder)
    {
        return folder.EndsWith(IOPath.DirectorySeparatorChar) ? folder : folder + IOPath.DirectorySeparatorChar;
    }
}