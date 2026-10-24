using Source191.Vantage.Launch;

namespace Source191.Vantage.UnitTests.Launch;

public sealed class RepositoryCatalogueShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-catalogue-" + Guid.NewGuid().ToString("N"));

    public RepositoryCatalogueShould()
    {
        Directory.CreateDirectory(Path.Combine(root, "Northwind.Agents", ".git"));
        Directory.CreateDirectory(Path.Combine(root, "Northwind.Templates", ".git"));
        Directory.CreateDirectory(Path.Combine(root, "Northwind.Billing"));
        File.WriteAllText(Path.Combine(root, "Northwind.Billing", ".git"), "gitdir: ../Northwind.Billing-main/.git/worktrees/x");
        Directory.CreateDirectory(Path.Combine(root, "notes"));
    }

    private RepositoryCatalogue Catalogue => new([root]);

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
        last.Path.ShouldBe(root);
        last.Name.ShouldContain("no repository");
    }

    [Fact]
    public void PutRecentlyUsedRepositoriesFirstInRecentOrder()
    {
        IReadOnlyList<string> recent = [Path.Combine(root, "Northwind.Agents"), Path.Combine(root, "Northwind.Templates"), Path.Combine(root, "gone")];

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
        RepositoryCatalogue catalogue = new([root, Path.Combine(root, "missing")]);

        catalogue.Choices(recent: [], filter: string.Empty).Count(choice => choice.IsRepository).ShouldBe(3);
    }

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
    }
}