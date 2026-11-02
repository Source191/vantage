using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>Every effect handler has a test class of its own, named after it with <c>Should</c> on the end.</summary>
public sealed class HandlersShould
{
    [Fact]
    public void HaveATestClassPerHandler()
    {
        HashSet<string> testClasses = typeof(HandlersShould).Assembly.GetTypes().Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        IEnumerable<string> untested = FindEveryHandler()
            .Select(handler => handler.Name)
            .Where(name => !testClasses.Contains($"{name}Should"))
            .Order(StringComparer.Ordinal);

        untested.ShouldBeEmpty();
    }

    [Fact]
    public void FindTheKnownHandlers()
    {
        FindEveryHandler().Count().ShouldBeGreaterThanOrEqualTo(20);
    }

    private static IEnumerable<Type> FindEveryHandler()
    {
        return typeof(IEffectHandler<>).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.GetInterfaces().Any(IsHandlerInterface));
    }

    private static bool IsHandlerInterface(Type implemented)
    {
        return implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IEffectHandler<>);
    }
}