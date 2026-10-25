using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class SolutionFinderShould
{
    private const string Orders = @"C:\Users\me\source\repos\Northwind.Orders";

    private static readonly FolderPath Folder = new(Orders);

    [Fact]
    public void FindTheOnlySolutionInTheFolder()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Orders}\Northwind.Orders.slnx").WithFile($@"{Orders}\README.md");

        new SolutionFinder(folders).Find(Folder, "Northwind.Orders").ShouldBe(new SolutionSearch.One($@"{Orders}\Northwind.Orders.slnx"));
    }

    [Fact]
    public void LookInSrcWhenTheFolderHasNone()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Orders}\src\Northwind.Orders.sln");

        new SolutionFinder(folders).Find(Folder, "Northwind.Orders").ShouldBe(new SolutionSearch.One($@"{Orders}\src\Northwind.Orders.sln"));
    }

    [Fact]
    public void PreferTheFolderToSrcWhenBothHoldOne()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Orders}\Top.sln").WithFile($@"{Orders}\src\Deep.sln");

        new SolutionFinder(folders).Find(Folder, "Northwind.Orders").ShouldBe(new SolutionSearch.One($@"{Orders}\Top.sln"));
    }

    [Fact]
    public void PreferTheSolutionNamedAfterTheRepository()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Orders}\Northwind.Orders.slnx").WithFile($@"{Orders}\Northwind.Orders.Tools.slnx");

        new SolutionFinder(folders).Find(Folder, "northwind.orders").ShouldBe(new SolutionSearch.One($@"{Orders}\Northwind.Orders.slnx"));
    }

    [Fact]
    public void OfferSeveralWhenNoneIsNamedAfterTheRepository()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Orders}\Worker.sln").WithFile($@"{Orders}\Api.sln");

        new SolutionFinder(folders).Find(Folder, "Northwind.Orders").ShouldBeOfType<SolutionSearch.Several>().Files.ShouldBe([$@"{Orders}\Api.sln", $@"{Orders}\Worker.sln"]);
    }

    [Fact]
    public void FindNothingWhenNeitherFolderHasOne()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Orders}\README.md").WithFile($@"{Orders}\src\deep\Deep.sln").WithDirectory($@"{Orders}\bin");

        new SolutionFinder(folders).Find(Folder, "Northwind.Orders").ShouldBe(new SolutionSearch.None());
    }

    [Fact]
    public void FindSlnAndSlnx()
    {
        FakeFolders folders = new FakeFolders().WithFile($@"{Orders}\A.SLN").WithFile($@"{Orders}\B.slnx").WithFile($@"{Orders}\C.slnf");

        new SolutionFinder(folders).Find(Folder, "Northwind.Orders").ShouldBeOfType<SolutionSearch.Several>().Files.ShouldBe([$@"{Orders}\A.SLN", $@"{Orders}\B.slnx"]);
    }
}