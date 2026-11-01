using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class WhereLabelShould
{
    private const string LongRepository = "Northwind.Integration.CosmosDb.ChangeFeedMonitoring";

    [Fact]
    public void NameTheRepositoryAndTheSlugOfAWorktreeVantageMade()
    {
        WhereLabel.For("Northwind.Orders", "Northwind.Orders-fix-123", inRepository: true).Text.ShouldBe("Northwind.Orders › fix-123");
    }

    [Fact]
    public void NameTheMainCheckoutByItsRepositoryAlone()
    {
        WhereLabel.For("Northwind.Orders", "Northwind.Orders", inRepository: true).Text.ShouldBe("Northwind.Orders");
    }

    [Fact]
    public void NameAFolderInsideTheRepository()
    {
        WhereLabel.For("Northwind.Orders", @"Northwind.Orders\src\Api", inRepository: true).Text.ShouldBe("Northwind.Orders › src\\Api");
    }

    [Fact]
    public void NameAHandMadeFolderByItsLabel()
    {
        WhereLabel.For("Northwind.Orders", "spike", inRepository: true).Text.ShouldBe("Northwind.Orders › spike");
    }

    [Fact]
    public void ShowTheLabelAloneOutsideARepository()
    {
        WhereLabel.For("notes", @"~\scratch\notes", inRepository: false).Text.ShouldBe(@"~\scratch\notes");
    }

    [Fact]
    public void BeInsideARepositoryWithItsPlaceAndOutsideOneWithTheFolderLabel()
    {
        WhereLabel.For("Northwind.Orders", "Northwind.Orders-fix-123", inRepository: true).ShouldBe(new WhereLabel.Inside("Northwind.Orders", "fix-123"));
        WhereLabel.For("notes", @"~\scratch\notes", inRepository: false).ShouldBe(new WhereLabel.Outside(@"~\scratch\notes"));
    }

    [Fact]
    public void CutTheLabelOutsideARepositoryAtItsEnd()
    {
        new WhereLabel.Outside(@"~\scratch\notes").Fit(8).ShouldBe(@"~\scrat…");
    }

    [Fact]
    public void ElideTheRepositoryFirst()
    {
        WhereLabel label = WhereLabel.For(LongRepository, $"{LongRepository}-fix-123", inRepository: true);

        string fitted = label.Fit(57);

        fitted.Length.ShouldBe(57);
        fitted.ShouldEndWith("› fix-123");
        fitted.Count(character => character == '…').ShouldBe(1);
        fitted.ShouldStartWith(LongRepository[..8]);
        fitted[..fitted.IndexOf(" › ", StringComparison.Ordinal)].ShouldEndWith(LongRepository[^8..]);
        label.Fit(64).ShouldBe($"{LongRepository} › fix-123");
        label.Fit(20).ShouldEndWith("› fix-123");
        label.Fit(20).Length.ShouldBe(20);
    }
}