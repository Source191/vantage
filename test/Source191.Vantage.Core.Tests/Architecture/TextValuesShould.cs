using Source191.Vantage.Domain;
using System.Reflection;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>
/// Every Domain record that wraps text has the same three doors and no others: a static <c>From</c> that builds the
/// value or throws, <see cref="ITextValue{TSelf}.TryParse"/> that answers false, and (for typed text) a <c>Validate</c>
/// that returns the reason. No static member hands out a null instead of a value.
/// </summary>
public sealed class TextValuesShould
{
    private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly string[] OtherFactoryNames = ["Parse", "Of"];

    private static IEnumerable<Type> TextValueTypes => typeof(ITextValue<>).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(ITextValue<>).Namespace)
        .Where(type => type.GetMethod("<Clone>$") is not null)
        .Where(type => type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)?.PropertyType == typeof(string));

    public static TheoryData<Type> TextValues => new(TextValueTypes);

    [Fact]
    public void FindEveryTextValue()
    {
        TextValueTypes.Select(type => type.Name).ShouldBe(["BranchName", "FolderPath", "JobId", "SessionId", "SessionName", "WorktreeSlug"], ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(TextValues))]
    public void ImplementTheInterface(Type type)
    {
        type.GetInterfaces().ShouldContain(typeof(ITextValue<>).MakeGenericType(type));
    }

    [Theory]
    [MemberData(nameof(TextValues))]
    public void HaveNoPublicConstructor(Type type)
    {
        type.GetConstructors().ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(TextValues))]
    public void DeclareFromOnTheInterface(Type type)
    {
        MethodInfo from = typeof(ITextValue<>).MakeGenericType(type).GetMethod("From").ShouldNotBeNull();

        from.IsAbstract.ShouldBeTrue();
        from.GetParameters().Select(parameter => parameter.ParameterType).ShouldBe([typeof(string)]);
    }

    [Theory]
    [MemberData(nameof(TextValues))]
    public void HaveNoOtherFactory(Type type)
    {
        type.GetMembers(AnyStatic).Select(member => member.Name).Where(OtherFactoryNames.Contains).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(TextValues))]
    public void HaveNoStaticMemberThatAnswersNull(Type type)
    {
        NullabilityInfoContext context = new();
        IEnumerable<string> methods = type.GetMethods(AnyStatic).Where(method => method.ReturnType == type && context.Create(method.ReturnParameter).ReadState == NullabilityState.Nullable).Select(method => method.Name);
        IEnumerable<string> properties = type.GetProperties(AnyStatic).Where(property => property.PropertyType == type && context.Create(property).ReadState == NullabilityState.Nullable).Select(property => property.Name);
        IEnumerable<string> fields = type.GetFields(AnyStatic).Where(field => field.FieldType == type && context.Create(field).ReadState == NullabilityState.Nullable).Select(field => field.Name);

        methods.Concat(properties).Concat(fields).ShouldBeEmpty();
    }

    [Fact]
    public void ParseEveryGoodTextAndSkipTheRest()
    {
        string?[] texts = [@"C:\Users\me\source\repos\Northwind.Orders", null, @"repos\Northwind.Billing", "C:/Users/me/source/repos/Northwind.Billing"];

        texts.Parsed<FolderPath>().ShouldBe([FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders"), FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing")]);
    }
}