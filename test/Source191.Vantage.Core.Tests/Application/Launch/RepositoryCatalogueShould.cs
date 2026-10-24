using Source191.Vantage.Application.Launch;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class RepositoryCatalogueShould
{
    private const string Root = @"C:\Users\me\source\repos";

    private readonly FakeFolders folders = new FakeFolders()
        .WithDirectory(Path.Combine(Root, "Northwind.Agents", ".git"))
        .WithDirectory(Path.Combine(Root, "Northwind.Templates", ".git"))
        .WithFile(Path.Combine(Root, "Northwind.Billing", ".git"), "gitdir: ../Northwind.Billing-main/.git/worktrees/x")
        .WithDirectory(Path.Combine(Root, "notes"));

    private RepositoryCatalogue Catalogue => new(folders, [Root]);

    [Fact]
    public void ListEveryGitRepositoryDirectlyUnderTheRootsAlphabetically()
    {
        IReadOnlyList<RepositoryChoice> choices = Catalogue.Choices(recent: [], filter: string.Empty);

        choices.Where(choice => choice.IsRepository).Select(choice => choice.Name)
            .ShouldBe(["Northwind.Agents", "Northwind.Billing", "Northwind.Templates"]);
    }

    [Fact]
    public void OfferTheRootItselfAsTheNoRepositoryChoiceLast()
    {
        IReadOnlyList<RepositoryChoice> choices = Catalogue.Choices(recent: [], filter: string.Empty);

        RepositoryChoice last = choices[^1];
        last.IsRepository.ShouldBeFalse();
        last.Path.ShouldBe(Root);
        last.Name.ShouldContain("no repository");
    }

    [Fact]
    public void PutRecentlyUsedRepositoriesFirstInRecentOrder()
    {
        IReadOnlyList<string> recent = [Path.Combine(Root, "Northwind.Agents"), Path.Combine(Root, "Northwind.Templates"), Path.Combine(Root, "gone")];

        IReadOnlyList<RepositoryChoice> choices = Catalogue.Choices(recent, filter: string.Empty);

        choices.Take(3).Select(choice => choice.Name).ShouldBe(["Northwind.Agents", "Northwind.Templates", "Northwind.Billing"]);
    }

    [Fact]
    public void FilterByNameCaseInsensitively()
    {
        IReadOnlyList<RepositoryChoice> choices = Catalogue.Choices(recent: [], filter: "BILL");

        choices.Select(choice => choice.Name).ShouldBe(["Northwind.Billing"]);
    }

    [Fact]
    public void IgnoreARootThatDoesNotExist()
    {
        RepositoryCatalogue catalogue = new(folders, [Root, Path.Combine(Root, "missing")]);

        catalogue.Choices(recent: [], filter: string.Empty).Count(choice => choice.IsRepository).ShouldBe(3);
    }
}