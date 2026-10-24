using NetArchTest.Rules;
using Source191.Vantage.Domain;

namespace Source191.Vantage.UnitTests.Architecture;

/// <summary>
/// The dependency direction the project split will later enforce with project references: Domain at the bottom,
/// Ports above it, Application above that, and Adapters and Tui on top, with Program (the global namespace) the only
/// place that may know everything. Rules that the code does not meet yet are skipped and name the task that unskips them.
/// </summary>
public sealed class LayeringShould
{
    private const string Root = "Source191.Vantage";

    private static Types Vantage => Types.InAssembly(typeof(Session).Assembly);

    [Fact]
    public void KeepDomainFreeOfEverythingElse()
    {
        Vantage.That().ResideInNamespace($"{Root}.Domain")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Ports", $"{Root}.Application", $"{Root}.Adapters", $"{Root}.Tui", $"{Root}.Screens", $"{Root}.Actions", $"{Root}.Launch", "Terminal.Gui", "System.IO.File", "System.IO.Directory", "System.Diagnostics.Process")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepTerminalGuiInsideTui()
    {
        Vantage.That().ResideInNamespaceStartingWith(Root).And().DoNotResideInNamespaceStartingWith($"{Root}.Screens.Tui").And().DoNotResideInNamespaceStartingWith($"{Root}.Tui")
            .ShouldNot().HaveDependencyOn("Terminal.Gui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact(Skip = "untangle-layers task 3.7: WorktreeInventory, RepositoryCatalogue and LaunchWorkflow still call System.IO directly")]
    public void KeepFileSystemAndProcessesInsideAdapters()
    {
        Vantage.That().ResideInNamespaceStartingWith(Root).And().DoNotResideInNamespaceStartingWith($"{Root}.Adapters")
            .ShouldNot().HaveDependencyOnAny("System.IO.File", "System.IO.Directory", "System.Diagnostics.Process")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact(Skip = "untangle-layers task 4.4: Application still references Adapters for the ports and the state store")]
    public void KeepApplicationFreeOfAdaptersAndTui()
    {
        Vantage.That().ResideInNamespaceStartingWith($"{Root}.Application")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Adapters", $"{Root}.Tui", $"{Root}.Screens.Tui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact(Skip = "untangle-layers task 3.3: HandoffLog still takes a ViewEffect")]
    public void KeepAdaptersFreeOfApplicationAndTui()
    {
        Vantage.That().ResideInNamespaceStartingWith($"{Root}.Adapters")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Application", $"{Root}.Screens", $"{Root}.Actions", $"{Root}.Launch", $"{Root}.Tui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }
}