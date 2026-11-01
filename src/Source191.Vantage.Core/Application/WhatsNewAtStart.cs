using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>What the first start of a version shows before the console: the changes since the version that last ran, or nothing.</summary>
public abstract record WhatsNewAtStart
{
    public static readonly WhatsNewAtStart Nothing = new Silent();

    /// <summary>Show what changed, newest release first, up to the running version.</summary>
    public sealed record Show(ReleaseVersion Running, IReadOnlyList<ReleaseSection> Sections) : WhatsNewAtStart;

    /// <summary>Show nothing: the same version ran last, nothing ran before, a development build, or no notes to show.</summary>
    public sealed record Silent : WhatsNewAtStart;
}