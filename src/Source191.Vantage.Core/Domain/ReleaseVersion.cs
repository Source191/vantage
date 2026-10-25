using System.Globalization;

namespace Source191.Vantage.Domain;

/// <summary>
/// A semantic version, <c>major.minor.patch[-prerelease]</c>, compared by semantic version precedence; build metadata
/// after <c>+</c> is dropped. Text from outside Vantage (nuget.org's list, the update-check record) goes through
/// <see cref="TryParse"/>, which answers null for anything else.
/// </summary>
public sealed record ReleaseVersion(int Major, int Minor, int Patch, string? Prerelease) : IComparable<ReleaseVersion>
{
    public bool IsPrerelease => Prerelease is not null;

    public static ReleaseVersion? TryParse(string? text)
    {
        string withoutMetadata = (text ?? string.Empty).Split('+')[0].Trim();
        int dash = withoutMetadata.IndexOf('-', StringComparison.Ordinal);
        string core = dash < 0 ? withoutMetadata : withoutMetadata[..dash];
        string? prerelease = dash < 0 ? null : withoutMetadata[(dash + 1)..];
        string[] parts = core.Split('.');
        if (parts.Length != 3 || prerelease is { Length: 0 })
        {
            return null;
        }

        int?[] numbers = [.. parts.Select(Number)];

        return numbers.All(number => number is not null)
            ? new ReleaseVersion(numbers[0]!.Value, numbers[1]!.Value, numbers[2]!.Value, prerelease)
            : null;
    }

    public static bool operator <(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) < 0;

    public static bool operator <=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) > 0;

    public static bool operator >=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) >= 0;

    public bool IsHigherThan(ReleaseVersion other) => this > other;

    public int CompareTo(ReleaseVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        int byNumbers = (Major, Minor, Patch).CompareTo((other.Major, other.Minor, other.Patch));

        return byNumbers != 0 ? byNumbers : ComparePrerelease(Prerelease, other.Prerelease);
    }

    public override string ToString() => Prerelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";

    private static int? Number(string text)
    {
        bool digitsOnly = text.Length > 0 && text.All(char.IsAsciiDigit);

        return digitsOnly && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : null;
    }

    /// <summary>A release outranks any prerelease of the same numbers; prereleases compare identifier by identifier.</summary>
    private static int ComparePrerelease(string? mine, string? theirs)
    {
        return (mine, theirs) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            _ => CompareIdentifiers(mine.Split('.'), theirs.Split('.')),
        };
    }

    private static int CompareIdentifiers(string[] mine, string[] theirs)
    {
        foreach ((string left, string right) in mine.Zip(theirs))
        {
            int compared = CompareIdentifier(left, right);
            if (compared != 0)
            {
                return compared;
            }
        }

        return mine.Length.CompareTo(theirs.Length);
    }

    /// <summary>Numeric identifiers compare as numbers and rank below alphanumeric ones, which compare in ASCII order.</summary>
    private static int CompareIdentifier(string left, string right)
    {
        return (Number(left), Number(right)) switch
        {
            (int l, int r) => l.CompareTo(r),
            (int, null) => -1,
            (null, int) => 1,
            _ => string.CompareOrdinal(left, right),
        };
    }
}
