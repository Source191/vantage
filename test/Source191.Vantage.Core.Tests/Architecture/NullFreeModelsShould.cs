using System.Reflection;
using System.Runtime.CompilerServices;

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

    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static IEnumerable<string> NullableMembersOf(Type type)
    {
        NullabilityInfoContext context = new();
        string[] properties = [.. type.GetProperties(Declared)
            .Where(property => !IsGenerated(property) && IsNullable(context.Create(property)))
            .Select(property => property.Name)];
        IEnumerable<string> methods = type.GetMethods(Declared)
            .Where(method => !method.IsSpecialName && !IsGenerated(method))
            .SelectMany(method => NullableReturnOf(context, method).Concat(NullableParametersOf(context, method, method.Name)));
        IEnumerable<string> constructors = type.GetConstructors(Declared)
            .SelectMany(constructor => NullableParametersOf(context, constructor, "new"))
            .Where(parameter => !properties.Contains(PropertyNameIn(parameter)));

        return properties.Concat(methods).Concat(constructors).Distinct();
    }

    private static IEnumerable<string> NullableReturnOf(NullabilityInfoContext context, MethodInfo method)
    {
        return IsNullable(context.Create(method.ReturnParameter)) ? [method.Name] : [];
    }

    private static IEnumerable<string> NullableParametersOf(NullabilityInfoContext context, MethodBase method, string name)
    {
        return method.GetParameters()
            .Where(parameter => IsNullable(context.Create(parameter)))
            .Select(parameter => $"{name}({parameter.Name})");
    }

    private static string PropertyNameIn(string constructorParameter)
    {
        return constructorParameter["new(".Length..^1];
    }

    private static bool IsNullable(NullabilityInfo info)
    {
        return info.ReadState == NullabilityState.Nullable
            || info.WriteState == NullabilityState.Nullable
            || info.GenericTypeArguments.Any(IsNullable)
            || (info.ElementType is { } element && IsNullable(element));
    }

    private static bool IsGenerated(MemberInfo member)
    {
        return member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
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