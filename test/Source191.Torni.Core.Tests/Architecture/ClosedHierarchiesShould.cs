using Source191.Torni.Domain;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Source191.Torni.Core.Tests.Architecture;

/// <summary>
/// A closed hierarchy is an abstract record with its sealed cases nested inside it, so the cases are read as one
/// type's alternatives and no other file can add one. Held for <c>Domain</c> and <c>Ports</c>; <c>Application</c>'s open
/// hierarchies (<c>Outcome</c>, <c>DiagnosticEvent</c>, <c>StatusMessage</c>) have one top-level case per file by design.
/// </summary>
public sealed class ClosedHierarchiesShould
{
    private static readonly string[] Namespaces = ["Source191.Torni.Domain", "Source191.Torni.Ports"];

    [Fact]
    public void FindTheTopLevelCase()
    {
        Type[] probes = [typeof(ProbeShape), typeof(ProbeShape.Circle), typeof(ProbeSquare)];

        TopLevelCasesIn(probes, _ => true).ShouldBe([typeof(ProbeSquare).FullName]);
    }

    [Fact]
    public void NestEveryCaseInsideItsBase()
    {
        Type[] types = typeof(Session).Assembly.GetTypes();

        TopLevelCasesIn(types, type => Namespaces.Contains(type.Namespace)).ShouldBeEmpty();
    }

    private static IEnumerable<string?> TopLevelCasesIn(IEnumerable<Type> types, Func<Type, bool> bases)
    {
        Type[] written = [.. types.Where(type => type.GetCustomAttribute<CompilerGeneratedAttribute>() is null)];
        Type[] hierarchies = [.. written.Where(type => type.IsAbstract && !type.IsSealed && IsRecord(type) && bases(type))];

        return written
            .Where(type => type.BaseType is Type parent && hierarchies.Contains(parent) && type.DeclaringType != parent)
            .Select(type => type.FullName);
    }

    private static bool IsRecord(Type type) => type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is not null;

    private abstract record ProbeShape
    {
        public sealed record Circle : ProbeShape;
    }

    private sealed record ProbeSquare : ProbeShape;
}