using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>
/// Every class or record that can be sealed is sealed, as CA1852 (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1852)
/// recommends: a concrete class with no subtype is <c>sealed</c>, otherwise it is <c>abstract</c>, <c>static</c> or a
/// base that another of Vantage's types derives from. CA1852 itself fails the build for internal types; this fact covers
/// the public ones, which are most of Vantage because its projects reference one another.
/// </summary>
public sealed class SealedTypesShould
{
    private static readonly Assembly[] Vantage = [typeof(Session).Assembly, typeof(Source191.Vantage.Adapters.Files.Folders).Assembly, typeof(Source191.Vantage.Tui.ConsoleScreen).Assembly, typeof(InMemoryState).Assembly];

    [Fact]
    public void FindOnlyTheOpenLeaf()
    {
        Type[] probes = [typeof(ProbeOpen), typeof(ProbeSealed), typeof(ProbeStatic), typeof(ProbeShape), typeof(ProbeCircle), typeof(ProbeBase), typeof(ProbeDerived)];

        OpenClassesIn(probes).ShouldBe([typeof(ProbeOpen).FullName]);
    }

    [Fact]
    public void LeaveNoClassOpen()
    {
        OpenClassesIn(Vantage.SelectMany(assembly => assembly.GetTypes())).ShouldBeEmpty();
    }

    private static IEnumerable<string?> OpenClassesIn(IEnumerable<Type> types)
    {
        Type[] written = [.. types.Where(type => type.GetCustomAttribute<CompilerGeneratedAttribute>() is null)];
        HashSet<Type> bases = [.. written.Select(type => type.BaseType).OfType<Type>()];

        return written.Where(type => type.IsClass && !type.IsAbstract && !type.IsSealed && !bases.Contains(type)).Select(type => type.FullName);
    }

#pragma warning disable CA1852 // The open leaf the finder must report.
    private class ProbeOpen;
#pragma warning restore CA1852

    private sealed class ProbeSealed;

    private static class ProbeStatic;

    private abstract record ProbeShape;

    private sealed record ProbeCircle : ProbeShape;

    private class ProbeBase;

    private sealed class ProbeDerived : ProbeBase;
}