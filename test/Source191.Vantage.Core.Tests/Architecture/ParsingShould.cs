using Source191.Vantage.Domain;
using System.Reflection;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>
/// <c>TryParse</c> means one thing in Domain: it answers <see langword="bool"/> and hands the value out through
/// <see langword="out"/>, as <see cref="ITextValue{TSelf}.TryParse"/> does. No Domain type declares a <c>Parse</c>,
/// so no parser answers null in place of false.
/// </summary>
public sealed class ParsingShould
{
    private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static IEnumerable<Type> DomainTypes => typeof(ITextValue<>).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(ITextValue<>).Namespace)
        .Where(type => !type.IsInterface);

    private static IEnumerable<(string Type, MethodInfo Method)> StaticMethodsNamed(string name) => DomainTypes
        .SelectMany(type => type.GetMethods(AnyStatic).Where(method => method.Name == name).Select(method => (type.Name, method)));

    [Fact]
    public void FindEveryParser()
    {
        StaticMethodsNamed("TryParse").Concat(StaticMethodsNamed("Parse"))
            .Select(parser => parser.Type)
            .Distinct()
            .ShouldBe(["BranchName", "FolderPath", "JobId", "ReleaseNotes", "ReleaseVersion", "RemoteRepository", "ReportTitle", "SessionId", "SessionName", "WorktreeSlug"], ignoreOrder: true);
    }

    [Fact]
    public void ReturnBoolFromEveryTryParse()
    {
        StaticMethodsNamed("TryParse")
            .Where(parser => parser.Method.ReturnType != typeof(bool))
            .Select(parser => parser.Type)
            .ShouldBeEmpty();
    }

    [Fact]
    public void DeclareNoParse()
    {
        StaticMethodsNamed("Parse")
            .Select(parser => parser.Type)
            .ShouldBeEmpty();
    }
}