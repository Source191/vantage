using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics.CodeAnalysis;
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
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly AllowedNullable[] Kept = [];

    private static readonly string[] ModelNamespaces = ["Source191.Vantage.Domain", "Source191.Vantage.Ports", "Source191.Vantage.Application"];

    private static readonly string[] EqualityNames = ["Equals", "CompareTo", "op_Equality", "op_Inequality"];

    [Fact]
    public void SeeEveryKindOfNullable()
    {
        NullableMembersOf(typeof(IProbe)).ShouldBe(["Maybe", "MaybeAnswer", "Take(maybe)", "Names", "Count"], ignoreOrder: true);
        NullableMembersOf(typeof(ProbeRecord)).ShouldBe(["Maybe"]);
        NullableMembersOf(typeof(IGenericProbe<>)).ShouldBe(["Maybe", "MaybeLater", "Find"], ignoreOrder: true);
    }

    [Fact]
    public void ExemptTheHouseShapes()
    {
        NullableMembersOf(typeof(IHouseShapes)).ShouldBeEmpty();
        NullableMembersOf(typeof(IBareTryParse)).ShouldBe(["TryParse(text)"]);
    }

    [Fact]
    public void FindTheModelTypes()
    {
        Type[] types = [.. ModelTypes];

        types.ShouldContain(typeof(SessionName));
        types.ShouldContain(typeof(IGit));
        types.ShouldContain(typeof(CurrentSettings));
        types.ShouldContain(typeof(SessionsView));
        types.ShouldContain(typeof(Head.OnBranch));
        types.Select(type => type.FullName).ShouldNotContain("Source191.Vantage.Application.Views.RowDecision");
        types.ShouldAllBe(type => type.IsVisible);
    }

    [Fact]
    public void HoldNoNullableMember()
    {
        IEnumerable<string> unexplained = ModelTypes
            .SelectMany(type => NullableMembersOf(type).Select(member => new AllowedNullable(type, member, string.Empty)))
            .Where(found => !Kept.Any(kept => kept.Type == found.Type && kept.Member == found.Member))
            .Select(found => found.Name);

        unexplained.ShouldBeEmpty();
    }

    private static IEnumerable<Type> ModelTypes => typeof(SessionName).Assembly.GetTypes()
        .Where(type => type.IsVisible && ModelNamespaces.Any(model => type.Namespace == model || type.Namespace?.StartsWith(model + ".", StringComparison.Ordinal) == true));

    private static IEnumerable<string> NullableMembersOf(Type type)
    {
        NullabilityInfoContext context = new();
        string[] properties = [.. type.GetProperties(Declared)
            .Where(property => !IsGenerated(property) && IsNullable(context.Create(property), AnnotationsOf(property)))
            .Select(property => property.Name)];
        IEnumerable<string> methods = type.GetMethods(Declared)
            .Where(method => !IsAccessor(method) && !IsGenerated(method) && !IsHouseShape(context, method))
            .SelectMany(method => NullableReturnOf(context, method).Concat(NullableParametersOf(context, method, method.Name)));
        IEnumerable<string> constructors = type.GetConstructors(Declared)
            .SelectMany(constructor => NullableParametersOf(context, constructor, "new"))
            .Where(parameter => !properties.Contains(PropertyNameIn(parameter)));

        return properties.Concat(methods).Concat(constructors).Distinct();
    }

    private static IEnumerable<string> NullableReturnOf(NullabilityInfoContext context, MethodInfo method)
    {
        return IsNullable(context, method.ReturnParameter) ? [method.Name] : [];
    }

    private static IEnumerable<string> NullableParametersOf(NullabilityInfoContext context, MethodBase method, string name)
    {
        return method.GetParameters()
            .Where(parameter => IsNullable(context, parameter))
            .Select(parameter => $"{name}({parameter.Name})");
    }

    private static string PropertyNameIn(string constructorParameter)
    {
        return constructorParameter["new(".Length..^1];
    }

    private static bool IsNullable(NullabilityInfoContext context, ParameterInfo parameter)
    {
        return IsNullable(context.Create(parameter), AnnotationsOf(parameter));
    }

    private static bool IsNullable(NullabilityInfo info, Annotations annotations)
    {
        int index = 0;

        return IsNullableAt(info, annotations, ref index);
    }

    /// <summary>
    /// Walks the type in the order the compiler writes its nullable flags. <see cref="NullabilityInfoContext"/> reads an
    /// unconstrained type parameter as nullable whether or not it carries <c>?</c>, because it could be instantiated
    /// with a nullable type, so at a type parameter the member's own flag decides.
    /// </summary>
    private static bool IsNullableAt(NullabilityInfo info, Annotations annotations, ref int index)
    {
        Type type = info.Type.IsByRef ? info.Type.GetElementType() ?? info.Type : info.Type;
        bool nullable = type.IsValueType ? IsNullableValue(type, ref index) : IsNullableReference(info, type, annotations, ref index);

        foreach (NullabilityInfo argument in info.GenericTypeArguments)
        {
            nullable |= IsNullableAt(argument, annotations, ref index);
        }

        return nullable;
    }

    private static bool IsNullableValue(Type type, ref int index)
    {
        Type? underlying = Nullable.GetUnderlyingType(type);

        if ((underlying ?? type).IsGenericType)
        {
            index++;
        }

        return underlying is not null;
    }

    private static bool IsNullableReference(NullabilityInfo info, Type type, Annotations annotations, ref int index)
    {
        bool nullable = type.IsGenericParameter
            ? annotations.IsAnnotated(index)
            : info.ReadState == NullabilityState.Nullable || info.WriteState == NullabilityState.Nullable;

        index++;

        if (info.ElementType is { } element)
        {
            nullable |= IsNullableAt(element, annotations, ref index);
        }

        return nullable;
    }

    private static Annotations AnnotationsOf(PropertyInfo property)
    {
        return new(FlagsIn(property.GetCustomAttributesData()), ContextOf(property));
    }

    private static Annotations AnnotationsOf(ParameterInfo parameter)
    {
        return new(FlagsIn(parameter.GetCustomAttributesData()), ContextOf(parameter.Member));
    }

    private static byte[] FlagsIn(IEnumerable<CustomAttributeData> attributes)
    {
        return attributes.FirstOrDefault(IsNamed("NullableAttribute"))?.ConstructorArguments[0].Value switch
        {
            byte all => [all],
            IEnumerable<CustomAttributeTypedArgument> each => [.. each.Select(flag => flag.Value).OfType<byte>()],
            _ => [],
        };
    }

    private static byte ContextOf(MemberInfo member)
    {
        for (MemberInfo? scope = member; scope is not null; scope = scope.DeclaringType)
        {
            if (scope.GetCustomAttributesData().FirstOrDefault(IsNamed("NullableContextAttribute"))?.ConstructorArguments[0].Value is byte context)
            {
                return context;
            }
        }

        return 0;
    }

    private static Func<CustomAttributeData, bool> IsNamed(string name)
    {
        return attribute => attribute.AttributeType.FullName == $"System.Runtime.CompilerServices.{name}";
    }

    private static bool IsAccessor(MethodInfo method)
    {
        return method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal);
    }

    private static bool IsHouseShape(NullabilityInfoContext context, MethodInfo method)
    {
        return EqualityNames.Contains(method.Name) || IsTryParse(context, method);
    }

    private static bool IsTryParse(NullabilityInfoContext context, MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();

        return method.IsStatic
            && method.Name == "TryParse"
            && parameters is [{ ParameterType: Type text } first, ..]
            && text == typeof(string)
            && IsNullable(context, first)
            && parameters.Any(IsOutUntilParsed);
    }

    private static bool IsOutUntilParsed(ParameterInfo parameter)
    {
        return parameter.IsOut
            && (parameter.GetCustomAttribute<MaybeNullWhenAttribute>() is { ReturnValue: false }
                || parameter.GetCustomAttribute<NotNullWhenAttribute>() is { ReturnValue: true });
    }

    private static bool IsGenerated(MemberInfo member)
    {
        return member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
    }

    /// <summary>A member's nullable flags (one for every position, or one for all) and the context that applies without them.</summary>
    private sealed record Annotations(byte[] Flags, byte Context)
    {
        private const byte Annotated = 2;

        public bool IsAnnotated(int index)
        {
            byte flag = Flags switch
            {
                [byte all] => all,
                _ when index < Flags.Length => Flags[index],
                _ => Context,
            };

            return flag == Annotated;
        }
    }

    private sealed record AllowedNullable(Type Type, string Member, string Reason)
    {
        public string Name => $"{NameOf(Type)}.{Member}";

        private static string NameOf(Type type)
        {
            return type.DeclaringType is { } outer ? $"{NameOf(outer)}.{type.Name}" : type.Name;
        }
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

    private interface IGenericProbe<T>
    {
        T Sure { get; }

        T? Maybe { get; }

        IReadOnlyList<T> Items { get; }

        Task<T?> MaybeLater();

        TOut Map<TOut>(Func<T, TOut> map);

        TOut? Find<TOut>(TOut value);
    }

    private interface IHouseShapes
    {
        static abstract bool TryParse(string? text, [MaybeNullWhen(false)] out IHouseShapes value);

        bool Equals(IHouseShapes? other);

        int CompareTo(IHouseShapes? other);

        bool TryFind(int key, [NotNullWhen(true)] out string? found);

        bool TryTake(IHouseShapes from, [MaybeNullWhen(false)] out IHouseShapes taken);
    }

    private interface IBareTryParse
    {
        static abstract bool TryParse(string? text, out IBareTryParse value);
    }
}