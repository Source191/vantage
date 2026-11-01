using System.Reflection;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>
/// Vantage's own models and results never use null for meaning: no public property, parameter or return of a type in
/// Domain, Ports or Application is nullable, unless it is one of the language's or the house's own shapes or is kept,
/// with its reason, on the allow-list.
/// </summary>
public sealed class NullFreeModelsShould
{
    [Fact]
    public void SeeEveryKindOfNullable()
    {
        NullableMembersOf(typeof(IProbe)).ShouldBe(["Maybe", "MaybeAnswer", "Take(maybe)", "Names", "Count"], ignoreOrder: true);
        NullableMembersOf(typeof(ProbeRecord)).ShouldBe(["Maybe"]);
    }

    private static IEnumerable<string> NullableMembersOf(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name);
    }

    private interface IProbe
    {
        string Sure { get; }

        string? Maybe { get; }

        List<string?> Names { get; }

        List<string> SureNames { get; }

        int? Count { get; }

        int Number { get; }

        string Answer();

        string? MaybeAnswer();

        void Take(string sure, string? maybe);
    }

    private sealed record ProbeRecord(string Sure, string? Maybe);
}