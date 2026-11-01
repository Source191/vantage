using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Diagnostics;
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

    /// <summary>
    /// The nullables kept on purpose, each with its reason: absences that arrive from outside Vantage (git, the process
    /// table, config.json, the environment, Microsoft.Extensions.Logging) and are met where they arrive, and the list
    /// selection, which is empty when nothing is shown. Fixing one removes its entry; <c>KeepNoStaleEntry</c> says so.
    /// </summary>
    private static readonly AllowedNullable[] Kept =
    [
        new(typeof(IGit), "LastCommitAtAsync", "git answers nothing when it fails or the worktree has no commits"),
        new(typeof(IGit), "DefaultRemoteBranchAsync", "git answers nothing when it fails or origin has no HEAD"),
        new(typeof(IGit), "CurrentBranchAsync", "git answers nothing when it fails or the head is detached"),
        new(typeof(IGit), "RepositoryRootAsync", "git answers nothing outside a repository"),
        new(typeof(IGit), "MainCheckoutOfAsync", "git answers nothing when it cannot name the main checkout"),
        new(typeof(IGit), "OriginUrlAsync", "git answers nothing when there is no origin"),
        new(typeof(IProcessRegistry), "StartTimeOf", "the process has gone or its start time is refused to Vantage"),
        new(typeof(IProcessRegistry), "ParentOf", "the process has gone or lists no parent"),
        new(typeof(IProcessRegistry), "UsageOf", "the process has gone or cannot be measured"),
        new(typeof(ProcessIdentity), "StillTheSession(processStartedAt)", "takes IProcessRegistry.StartTimeOf's answer as it comes"),
        new(typeof(ProcessTree), "Under(usageOf)", "takes IProcessRegistry.UsageOf as it is"),
        new(typeof(TerminalTab), "ShellOf", "the parent chain from IProcessRegistry.ParentOf may end before a shell"),
        new(typeof(TerminalTab), "TerminalOf", "the parent chain from IProcessRegistry.ParentOf may end before a terminal"),
        new(typeof(FolderPath), "Parent", "a drive's root has no parent, as Path.GetDirectoryName says"),
        new(typeof(TextValues), "Parsed(texts)", "keeps the texts from outside Vantage that parse, nulls among them"),
        new(typeof(VantageConfig), "RepositoryRoots", "config.json may leave the property out"),
        new(typeof(VantageConfig), "DefaultBase", "config.json may leave the property out"),
        new(typeof(VantageConfig), "WorktreeRoot", "config.json may leave the property out"),
        new(typeof(VantageConfig), "Editor", "config.json may leave the property out"),
        new(typeof(VantageConfig), "CheckForUpdates", "config.json may leave the property out"),
        new(typeof(VantageConfig), "DefaultPlace", "config.json may leave the property out"),
        new(typeof(EditorSetting), "Preset", "config.json may leave the property out"),
        new(typeof(EditorSetting), "Executable", "config.json may leave the property out"),
        new(typeof(EditorSetting), "Arguments", "config.json may leave the property out"),
        new(typeof(EditorChoice), "FromFile(preset)", "reads EditorSetting's properties as config.json has them"),
        new(typeof(EditorChoice), "FromFile(executable)", "reads EditorSetting's properties as config.json has them"),
        new(typeof(EditorChoice), "FromFile(arguments)", "reads EditorSetting's properties as config.json has them"),
        new(typeof(Settings), "Resolve(roots)", "resolves a property config.json left out to its default"),
        new(typeof(Settings), "Resolve(worktreeRoot)", "resolves a property config.json left out to its default"),
        new(typeof(Settings), "Resolve(defaultBase)", "resolves a property config.json left out to its default"),
        new(typeof(Settings), "Resolve(checkForUpdates)", "resolves a property config.json left out to its default"),
        new(typeof(Settings), "Resolve(defaultPlace)", "resolves a property config.json left out to its default"),
        new(typeof(ToolVersion), "FromInformational(informational)", "the assembly's informational version attribute may be missing"),
        new(typeof(UpdateCheckOptOut), "Applies(value)", "reads an environment variable that may be unset"),
        new(typeof(DiagnosticLogSetting), "Read(variable)", "reads VANTAGE_LOG, which may be unset"),
        new(typeof(Wording), "ClaudeBellPlan(replacing)", "Claude's settings.json may name no other notification channel"),
        new(typeof(WorktreeCounts), "LastCommitAt", "IGit.LastCommitAtAsync's answer as it comes"),
        new(typeof(WorktreeRow), "LastCommitAt", "IGit.LastCommitAtAsync's answer as it comes"),
        new(typeof(SessionActivity), "LastPromptedAt", "this Vantage has seen no prompt for the session yet"),
        new(typeof(SessionDetail), "LastPromptedAt", "SessionActivity.LastPromptedAt's answer"),
        new(typeof(WorktreeDetail), "Created", "a worktree Vantage did not create has no creation record"),
        new(typeof(ListSelection<>), "SelectedKey", "nothing is selected in an empty list"),
        new(typeof(ListSelection<>), "Selected", "nothing is selected in an empty or filtered-out list"),
        new(typeof(TableTitle), "Selected", "nothing is selected in an empty or filtered-out list"),
        new(typeof(SessionsViewState), "SelectedSessionId", "the list selection's key, held in the state"),
        new(typeof(SessionsViewState), "Selected", "the list selection"),
        new(typeof(WorktreesViewState), "SelectedPath", "the list selection's key, held in the state"),
        new(typeof(WorktreesViewState), "Selected", "the list selection"),
        new(typeof(ConfigViewState), "SelectedKey", "the list selection's key, held in the state"),
        new(typeof(ConfigViewState), "Selected", "the list selection"),
        new(typeof(WizardState), "SelectedIndex", "bound to Terminal.Gui's ListView.SelectedItem, which is null with no row"),
        new(typeof(IListViewState<>), "Banner", "the last load failure's message, none while loading succeeds"),
        new(typeof(SessionsViewState), "Banner", "the last load failure's message, none while loading succeeds"),
        new(typeof(WorktreesViewState), "Banner", "the last load failure's message, none while loading succeeds"),
        new(typeof(ConfigViewState), "Banner", "the last load failure's message, none while loading succeeds"),
        new(typeof(SettingsState), "BrowseFrom", "no folder typed yet, so the folder picker starts where Terminal.Gui chooses"),
        new(typeof(TabKeys), "For", "a key that is not a tab key answers no effect"),
        new(typeof(Started), "ClaudeCode", "Claude Code's version could not be read"),
        new(typeof(Failed), "What", "a failure outside an effect has no code name"),
        new(typeof(TerminalGuiReported), "Failure", "Terminal.Gui reports some events without an exception"),
        new(typeof(Attempt), "RunAsync(what)", "passed on as Failed.What"),
        new(typeof(TerminalGuiBridge), "BeginScope", "ILogger's signature, from Microsoft.Extensions.Logging"),
        new(typeof(TerminalGuiBridge), "Log(exception)", "ILogger's signature, from Microsoft.Extensions.Logging"),
        new(typeof(TerminalGuiBridge), "Log(formatter)", "ILogger's signature, from Microsoft.Extensions.Logging"),
        new(typeof(SessionName), "Value", "deliberately stale: a text value's Value is never null"),
    ];
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

    [Fact]
    public void KeepNoStaleEntry()
    {
        IEnumerable<string> stale = Kept
            .Where(kept => !ModelTypes.Contains(kept.Type) || !NullableMembersOf(kept.Type).Contains(kept.Member))
            .Select(kept => kept.Name);

        stale.ShouldBeEmpty();
        Kept.ShouldAllBe(kept => !string.IsNullOrWhiteSpace(kept.Reason));
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
            .Where(parameter => !IsOutUntilParsed(parameter) && IsNullable(context, parameter))
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