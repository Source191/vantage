using Source191.Vantage.Application.Views;
using System.Reflection;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>
/// A reducer is a pure function of state and input: a static class whose only fields are <c>static readonly</c>, so nothing
/// it remembers between calls can change what it answers.
/// </summary>
public sealed class ReducersShould
{
    private const string ViewsNamespace = "Source191.Vantage.Application.Views";

    [Fact]
    public void BeStaticClassesWithOnlyReadonlyFields()
    {
        IEnumerable<string> offenders = FindEveryReducer()
            .Where(reducer => !IsPure(reducer))
            .Select(reducer => reducer.FullName ?? reducer.Name);

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void FindAtLeastSevenReducers()
    {
        FindEveryReducer().Count().ShouldBeGreaterThanOrEqualTo(7);
    }

    private static IEnumerable<Type> FindEveryReducer()
    {
        return typeof(SessionsView).Assembly.GetTypes()
            .Where(type => type.Namespace == ViewsNamespace && type.Name.EndsWith("View", StringComparison.Ordinal) && !type.IsNested);
    }

    private static bool IsPure(Type reducer)
    {
        BindingFlags every = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        bool isStaticClass = reducer.IsAbstract && reducer.IsSealed;
        return isStaticClass && reducer.GetFields(every).All(field => field.IsStatic && field.IsInitOnly);
    }
}