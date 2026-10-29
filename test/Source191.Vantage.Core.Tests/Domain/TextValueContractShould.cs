using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

/// <summary>The contract every text value keeps, written once: <c>From</c> throws what <c>TryParse</c> refuses.</summary>
public sealed class TextValueContractShould
{
    public static TheoryData<ContractCase> Cases => new(
        ContractCase.For<FolderPath>(@"repos\Northwind.Orders", @"C:\Users\me\repos\Northwind.Orders"),
        ContractCase.For<SessionId>(" ", "0f3a9c1e-1111-2222-3333-444455556666"),
        ContractCase.For<JobId>("0f3a9c1e-1111", "0f3a9c1e"),
        ContractCase.For<BranchName>("not a branch", "feature/northwind-orders"),
        ContractCase.For<SessionName>("   ", "northwind orders"),
        ContractCase.For<WorktreeSlug>("---", "northwind"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void RefuseWithArgumentExceptionTheTextTryParseRefuses(ContractCase contract) => Should.NotThrow(contract.RefusedTextThrows);

    [Theory]
    [MemberData(nameof(Cases))]
    public void GiveTheValueTryParseGives(ContractCase contract) => Should.NotThrow(contract.AcceptedTextEqualsParsed);

    [Theory]
    [MemberData(nameof(Cases))]
    public void KeepTheTextGiven(ContractCase contract) => Should.NotThrow(contract.ValueIsTheTextGiven);

    public sealed record ContractCase(string Name, Action RefusedTextThrows, Action AcceptedTextEqualsParsed, Action ValueIsTheTextGiven)
    {
        public static ContractCase For<T>(string refused, string accepted)
            where T : class, ITextValue<T>
        {
            return new ContractCase(
                typeof(T).Name,
                () =>
                {
                    T.TryParse(refused, out _).ShouldBeFalse();
                    Should.Throw<ArgumentException>(() => T.From(refused));
                },
                () =>
                {
                    T.TryParse(accepted, out T? parsed).ShouldBeTrue();
                    T.From(accepted).ShouldBe(parsed);
                },
                () => T.From(accepted).Value.ShouldBe(accepted));
        }

        public override string ToString() => Name;
    }
}