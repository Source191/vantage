using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Source191.Vantage.Domain;

/// <summary>
/// A <see href="https://semver.org/">semantic version</see>, <c>major.minor.patch[-prerelease]</c>, compared by
/// <see href="https://semver.org/#spec-item-11">semantic version precedence</see>; build metadata after <c>+</c> is
/// dropped. Text from outside Vantage (nuget.org's list, the update-check record) goes through
/// <see cref="TryParse"/>, which answers false for anything else.
/// </summary>
public sealed record ReleaseVersion : IComparable<ReleaseVersion>
{
    private ReleaseVersion(int major, int minor, int patch, string prerelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public string Prerelease { get; }

    public bool IsPrerelease => Prerelease.Length > 0;

    /// <summary>For text Vantage already trusts; anything that is not a version is a programming error.</summary>
    public static ReleaseVersion From(string text) => TryParse(text, out ReleaseVersion? version) ? version : throw new ArgumentException("Not a version.", nameof(text));

    public static bool TryParse(string? text, [MaybeNullWhen(false)] out ReleaseVersion version)
    {
        string withoutMetadata = (text ?? string.Empty).Split('+')[0].Trim();
        int dash = withoutMetadata.IndexOf('-', StringComparison.Ordinal);
        string core = dash < 0 ? withoutMetadata : withoutMetadata[..dash];
        string prerelease = dash < 0 ? string.Empty : withoutMetadata[(dash + 1)..];
        if ((dash < 0 || AreIdentifiers(prerelease))
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

    public override string ToString() => Prerelease.Length == 0 ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";

    /// <summary>Digits only, with no leading zero: <see cref="NumberStyles.None"/> refuses a sign, spaces and an empty identifier.</summary>
    private static int? Number(string text) => HasNoLeadingZero(text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : null;

    private static bool HasNoLeadingZero(string text) => text is "0" or not ['0', ..];

    /// <summary>Each dot-separated identifier is non-empty, and a numeric one has no leading zero.</summary>
    private static bool AreIdentifiers(string prerelease) => prerelease.Split('.').All(IsIdentifier);

    private static bool IsIdentifier(string identifier) => identifier.Length > 0 && (!identifier.All(char.IsAsciiDigit) || HasNoLeadingZero(identifier));

    /// <summary>A release outranks any prerelease of the same numbers; prereleases compare identifier by identifier.</summary>
    private static int ComparePrerelease(string mine, string theirs)
    {
        return (mine.Length, theirs.Length) switch
        {
            (0, 0) => 0,
            (0, _) => 1,
            (_, 0) => -1,
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