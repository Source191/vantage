namespace Source191.Vantage.Ports;

/// <summary>The editor as config.json holds it: <c>{ "preset": "VsCode" }</c>, or a custom executable and its argument list.</summary>
public sealed record EditorSetting(string? Preset, string? Executable, IReadOnlyList<string>? Arguments)
{
    public bool Equals(EditorSetting? other)
    {
        return other is not null
            && string.Equals(Preset, other.Preset, StringComparison.Ordinal)
            && string.Equals(Executable, other.Executable, StringComparison.Ordinal)
            && (Arguments ?? []).SequenceEqual(other.Arguments ?? []);
    }

    public override int GetHashCode() => HashCode.Combine(Preset, Executable);
}