using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using System.Reflection;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>
/// The guarantee that no diagnostic record can carry a name, a path or anything a person typed or a program printed:
/// every member of every event is a number, a duration, a version, an enum or one of the vetted values that can only be
/// built from Vantage's own code, and none of those values has a door that takes text.
/// </summary>
public sealed class DiagnosticEventsShould
{
    private static readonly Type[] VettedValues = [typeof(CodeName), typeof(ExceptionSummary), typeof(ChildCommand)];

    private static readonly Type[] AllowedTypes = [typeof(int), typeof(long), typeof(bool), typeof(TimeSpan), typeof(Version), typeof(ToolVersion), .. VettedValues];

    private static IEnumerable<Type> Events => typeof(DiagnosticEvent).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(DiagnosticEvent)));

    public static TheoryData<Type> EventTypes => new(Events);

    public static TheoryData<Type> VettedValueTypes => new(VettedValues);

    [Fact]
    public void FindEveryEvent()
    {
        Events.Count().ShouldBeGreaterThanOrEqualTo(10);
    }

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void HoldOnlyValuesVantageDefines(Type type)
    {
        IEnumerable<Type> members = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.Name != "EqualityContract")
            .Select(property => Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);

        members.Where(member => !member.IsEnum && !AllowedTypes.Contains(member)).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void HaveNoTextField(Type type)
    {
        type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(string) || field.FieldType == typeof(object))
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(VettedValueTypes))]
    public void ExposeNoDoorThatTakesText(Type type)
    {
        IEnumerable<MethodBase> doors = type.GetConstructors().Cast<MethodBase>().Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Static));

        doors.Where(door => door.GetParameters().Any(parameter => parameter.ParameterType == typeof(string))).ShouldBeEmpty();
    }
}