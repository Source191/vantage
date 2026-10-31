using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Source191.Vantage.Domain;

/// <summary>
/// A semantic version, <c>major.minor.patch[-prerelease]</c>, compared by semantic version precedence; build metadata
/// after <c>+</c> is dropped. Text from outside Vantage (nuget.org's list, the update-check record) goes through
/// <see cref="TryParse"/>, which answers false for anything else.
/// </summary>
public sealed record ReleaseVersion(int Major, int Minor, int Patch, string? Prerelease) : IComparable<ReleaseVersion>
{
    public bool IsPrerelease => Prerelease is not null;

    public static bool TryParse(string? text, [MaybeNullWhen(false)] out ReleaseVersion version)
    {
        string withoutMetadata = (text ?? string.Empty).Split('+')[0].Trim();
        int dash = withoutMetadata.IndexOf('-', StringComparison.Ordinal);
        string core = dash < 0 ? withoutMetadata : withoutMetadata[..dash];
        string? prerelease = dash < 0 ? null : withoutMetadata[(dash + 1)..];
        if (prerelease is not { Length: 0 }
            && core.Split('.') is [string major, string minor, string patch]
            && Number(major) is int majorNumber
            && Number(minor) is int minorNumber
            && Number(patch) is int patchNumber)
        {
            version = new ReleaseVersion(majorNumber, minorNumber, patchNumber, prerelease);
            return true;
        }

        version = null;
        return false;
    }

    /// <summary>The texts that read as versions, in order; the rest are skipped.</summary>
    public static IEnumerable<ReleaseVersion> Parsed(IEnumerable<string> texts)
    {
        foreach (string text in texts)
        {
            if (TryParse(text, out ReleaseVersion? version))
            {
                yield return version;
            }
        }
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

    /// <summary>Digits only: <see cref="NumberStyles.None"/> refuses a sign, spaces and an empty identifier.</summary>
    private static int? Number(string text) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : null;

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